using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.Identity;
using ChatWebApp.Models;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using Serilog;

namespace ChatWebApp
{
    [HubName("room")] // Attribute -> client-side name for the class may differ from server-side name
    public class ChatRoom : Hub
    {
        public static BelotGame game;

        public static List<Spectator> spectators = new List<Spectator>();
        public static Player[] players = { new Player(), new Player(), new Player(), new Player() };

        public static bool newRound = true;
        public static bool newGame = true;
        public static int scoreTarget = 1501;
        public static string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";
        public static int botDelay = 500;
        public static bool waitDeal, waitCall, waitCard;

        public static BelotHelpers helpers = new BelotHelpers();

        public static AgentBasic basic = new AgentBasic();

        public static string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/BelotServerLog-.txt");
        public static Serilog.Core.Logger log = new LoggerConfiguration().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger();

        //public static logPath

        public ChatRoom()
        {
            //log.Information("Creating new Chat Room");
        }

        // -------------------- Main --------------------

        public async Task GameController()
        {
            log.Debug("Entering GameController.");
            if (newGame)
            {
                newGame = false;
                Clients.All.DisableNewGame();
                Clients.All.CloseModalsAndButtons();
                Clients.All.DisableRadios();
                game = new BelotGame(players, Guid.NewGuid().ToString(), true);
                waitDeal = false;
                waitCall = false;
                waitCard = false;
                newRound = true;
                game.NewGame();
                Clients.All.NewGame(); // reset score table (offcanvas), reset score totals (card table), hide winner markers
            }
            while (((game.EWTotal < scoreTarget && game.NSTotal < scoreTarget) || game.EWTotal == game.NSTotal || game.Capot) && !waitDeal && !waitCall & !waitCard)
            {
                await Task.Run(() => RoundController());
            }

            if (!waitDeal && !waitCall & !waitCard) EndGame();
            log.Debug("Leaving GameController.");
        }

        public void RoundController()
        {
            log.Debug("Entering RoundController.");
            if (newRound)
            {
                newRound = false;
                game.NewRound();
                Clients.All.SetTurnIndicator(game.Turn); // show dealer
                Clients.All.SetDealerMarker(game.Turn);
                Clients.All.DisableDealBtn();
                Clients.All.NewRound(); // reset table, reset board, disable cards, reset suit selection 
                if (game.Players[game.Turn].IsHuman)
                {
                    Clients.Client(game.Players[game.Turn].ConnectionId).EnableDealBtn();
                    waitDeal = true;
                    return;
                }
                else
                {
                    Thread.Sleep(botDelay);
                }
            }

            if (!waitDeal && game.CardsDealt == 0)
            {
                game.Shuffle();
                game.Deal(5);
                for (int i = 0; i < 4; i++)
                {
                    if (game.Players[i].IsHuman) Clients.Client(game.Players[i].ConnectionId).Deal(new JavaScriptSerializer().Serialize(game.Hand[i]));
                }
            }

            if (game.NumCardsPlayed == 0)
            {
                while (!game.SuitDecided() && !waitCall)
                {
                    Clients.All.SetTurnIndicator(game.Turn);
                    CallController();
                }
            }

            if (game.RoundSuit == 0 && !waitCall)
            {
                SysAnnounce("No suit chosen.");
                newRound = true;
            }
            else if (game.RoundSuit != 0 && !waitCall)
            {
                if (game.NumCardsPlayed == 0)
                {
                    SysAnnounce("The round will be played in " + helpers.GetSuitNameFromNumber(game.RoundSuit) + ".");
                    game.Turn = game.FirstPlayer;
                    Clients.All.SetTurnIndicator(game.Turn);
                    game.Deal(3);
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.Players[i].IsHuman) Clients.Client(game.Players[i].ConnectionId).Deal(new JavaScriptSerializer().Serialize(game.Hand[i]));
                    }
                    if (game.RoundSuit != 5)
                    {
                        game.FindRuns();
                        game.FindCarres();
                        game.TruncateRuns();
                        game.FindBelots();
                    }
                }
                while (game.NumCardsPlayed < 32 & !waitCard)
                {
                    Clients.All.SetTurnIndicator(game.Turn);
                    TrickController();
                }
                if (game.NumCardsPlayed == 32)
                {
                    Clients.All.NewRound();
                    SysAnnounce(game.FinalisePoints());
                    //HubFinalisePoints();
                    Clients.All.AppendScoreTable(game.EWRoundPoints, game.NSRoundPoints);
                    Clients.All.UpdateScoreTotals(game.EWTotal, game.NSTotal);
                    Clients.All.ShowRoundSummary(game.TrickPoints, game.DeclarationPoints, game.BelotPoints, game.Result, game.EWRoundPoints, game.NSRoundPoints);
                    Thread.Sleep(6000);
                    Clients.All.HideRoundSummary();
                    newRound = true;
                }
            }
            log.Debug("Leaving RoundController.");
        }

        public void CallController()
        {
            log.Debug("Entering CallController.");
            int[] validCalls = game.ValidCalls();
            if (validCalls.Sum() == 0)
            {
                game.NominateSuit(0); // auto-pass
                //Thread.Sleep(botDelay);
                AnnounceSuit();
                if (--game.Turn == -1) game.Turn = 3;
            }
            else if (game.Players[game.Turn].IsHuman)
            {
                Clients.Client(game.Players[game.Turn].ConnectionId).ShowSuitModal(validCalls);
                waitCall = true;
            }
            else // bot
            {
                game.NominateSuit(basic.CallSuit(game.Hand[game.Turn], validCalls));
                //Thread.Sleep(botDelay);
                AnnounceSuit();
                if (--game.Turn == -1) game.Turn = 3;
            }
            log.Debug("Leaving CallController.");
        }

        public void TrickController()
        {
            log.Debug("Entering TrickController.");
            while (game.PlayedCards.Where(c => c != "c0-00").Count() < 4 && !waitCard)
            {
                if (game.Hand[game.Turn].Where(c => c != "c0-00").Count() == 1) // auto-play last card
                {
                    if (game.Players[game.Turn].IsHuman)
                    {
                        Clients.Client(game.Players[game.Turn].ConnectionId).PlayFinalCard();
                        //game.Log.Information("Playing final card for human player");
                    }
                    game.PlayCard(game.Hand[game.Turn].Where(c => c != "c0-00").First()); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                    CardPlayEnd();
                    continue;
                }
                bool belot = false;
                int[] validCards = game.ValidCards();
                if (game.Players[game.Turn].IsHuman)
                {
                    Clients.Client(game.Players[game.Turn].ConnectionId).enableCards(validCards);
                    waitCard = true;
                    // once a card is clicked, declarable extras are calculated in hub method, human selects and declares extras, then the card is played and game loop reinitiates
                }
                else
                {
                    string card = new AgentBasic().SelectCard(game.Hand[game.Turn], validCards, game.PlayedCards, game.Turn, game.DetermineWinner(), game.RoundSuit, game.TrickSuit, game.EWCalled);

                    game.PlayCard(card);

                    List<string> extras = new List<string>();

                    if (game.RoundSuit != 5)
                    {
                        if (game.CheckBelot(card))
                        {
                            extras.Add("Belot");
                            game.DeclareBelot();
                            belot = true;
                        }

                        if (game.NumCardsPlayed < 5)
                        {
                            game.DeclareRuns();
                            game.DeclareCarres();
                        }
                        AnnounceExtras(belot);
                    }

                    CardPlayEnd();
                }
            }
            if (!waitCard) // trick end
            {
                int winner = game.DetermineWinner();
                //int pointsBefore = game.EWRoundPoints + game.NSRoundPoints;
                if (winner == 0 || winner == 2)
                {
                    game.EWRoundPoints += game.CalculateTrickPoints();
                    game.EWWonATrick = true;
                }
                else
                {
                    game.NSRoundPoints += game.CalculateTrickPoints();
                    game.NSWonATrick = true;
                }

                Clients.All.ShowWinner(winner);
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(400);
                    Clients.All.ShowWinner(winner);
                }

                if (game.NumCardsPlayed < 32)
                {
                    Clients.All.ResetTable();
                    game.Turn = winner;
                    game.PlayedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
                }
                game.HighestTrumpInTrick = 0;
                game.TrickSuit = 0;
            }
            log.Debug("Leaving TrickController.");
        }

        // -------------------- Reset --------------------



        public void EndGame()
        {
            log.Debug("Entering EndGame.");
            string winner = "N/S";
            if (game.EWTotal > game.NSTotal) winner = "E/W";
            SysAnnounce(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + ".");

            Clients.All.SetDealerMarker(4);
            Clients.All.NewRound();
            Clients.All.SetTurnIndicator(4);
            // fancy animation and modal to indicate winning team
            if (game.EWTotal > game.NSTotal)
            {
                Clients.All.ShowWinner(0);
                Clients.All.ShowWinner(2);
            }
            else
            {
                Clients.All.ShowWinner(1);
                Clients.All.ShowWinner(3);
            }
            Clients.All.EnableNewGame();
            Clients.All.EnableRadios();
            newGame = true;
            log.Debug("Leaving EndGame.");
        }

        // -------------------- Setup --------------------

        public void HubShuffle()
        {
            log.Debug("Entering HubShuffle.");
            // this method is only called by human interaction with the html elements
            waitDeal = false;
            GameController();
            log.Debug("Leaving HubShuffle.");
        }

        // -------------------- Suit Nomination --------------------

        public void HubNominateSuit(int suit)
        {

            log.Debug("Entering HubNominateSuit.");
            game.NominateSuit(suit);

            // this method is only called by human interaction with the html elements
            waitCall = false;
            AnnounceSuit();
            if (--game.Turn == -1) game.Turn = 3;
            GameController();
            log.Debug("Leaving HubNominateSuit.");
        }

        public void AnnounceSuit()
        {
            log.Debug("Entering AnnounceSuit.");
            string username = GetDisplayName(game.Turn);

            string message = username + " passed.";

            int suit = game.SuitCall[game.SuitCall.Count - 1];

            if (suit > 0)
            {
                Clients.All.SuitNominated(suit);
                Clients.All.setCallerIndicator(game.Turn);
                if (suit < 7)
                {
                    message = username + " called " + helpers.GetSuitNameFromNumber(suit) + ".";
                }
                else if (suit == 7)
                {
                    message = username + " doubled!";
                }
                else
                {
                    message = username + " redoubled!!";
                }
            }

            SysAnnounce(message);
            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.All.EmoteSuit(suit, seatPos[game.Turn]);
            Emote(seatPos[game.Turn], botDelay);
            log.Debug("Leaving AnnounceSuit.");
        }

        // -------------------- Card Validation --------------------

        // -------------------- Gameplay --------------------

        public void HubPlayCard(string card)
        {
            log.Debug("Entering HubPlayCard.");
            game.PlayCard(card);

            List<string> extras = new List<string>();

            if (game.CheckBelot(card)) extras.Add("Belot: " + helpers.GetSuitNameFromNumber(Int32.Parse(card.Substring(1, 1))));

            if (game.RoundSuit != 5 && game.NumCardsPlayed < 5)
            {
                for (int i = 0; i < game.Runs[game.Turn].Count; i++)
                {
                    string extra = "";
                    if (!game.Runs[game.Turn][i].Declarable) extra += "#";
                    extra += helpers.GetRunNameFromLength(game.Runs[game.Turn][i].Length) + ": " + helpers.GetSuitNameFromNumber(game.Runs[game.Turn][i].Suit) + " " + helpers.GetCardRankFromNumber(game.Runs[game.Turn][i].Strength - game.Runs[game.Turn][i].Length + 1) + "→" + helpers.GetCardRankFromNumber(game.Runs[game.Turn][i].Strength);
                    extras.Add(extra);
                }

                for (int i = 0; i < game.Carres[game.Turn].Count; i++)
                {
                    extras.Add("Carre: " + helpers.GetCardRankFromNumber(game.Carres[game.Turn][i].Rank));
                }
            }
            Clients.Caller.DeclareExtras(new JavaScriptSerializer().Serialize(extras));
            log.Debug("Leaving HubPlayCard.");
        }

        public void CardPlayEnd()
        {
            log.Debug("Entering CardPlayEnd.");
            Clients.All.SetTableCard(game.Turn, game.PlayedCards[game.Turn]);
            Thread.Sleep(botDelay);
            if (game.NumCardsPlayed % 4 != 0) if (--game.Turn == -1) game.Turn = 3;
            if (game.NumCardsPlayed < 32) Clients.All.SetTurnIndicator(game.Turn);
            log.Debug("Leaving CardPlayEnd.");
        }

        public void HubExtrasDeclared(bool belot, bool[] runs, bool[] carres)
        {
            log.Debug("Entering HubExtrasDeclared.");
            if (game.RoundSuit != 5)
            {
                if (belot) game.DeclareBelot(belot);
                if (game.NumCardsPlayed < 5)
                {
                    game.DeclareRuns(runs);
                    game.DeclareCarres(carres);
                }
                AnnounceExtras(belot);
            }
            CardPlayEnd();
            waitCard = false;
            GameController();
            log.Debug("Leaving HubExtrasDeclared.");
        }

        public void AnnounceExtras(bool belotDeclared)
        {
            log.Debug("Entering AnnounceExtras.");
            List<string> emotes = new List<string>();
            if (belotDeclared)
            {
                SysAnnounce(game.GetDisplayName(game.Turn) + " called a Belot.");
                emotes.Add("Belot");
            }
            if (game.NumCardsPlayed < 5)
            {
                foreach (Run run in game.Runs[game.Turn])
                {
                    if (run.Declared)
                    {
                        string runName = helpers.GetRunNameFromLength(run.Length);
                        SysAnnounce(game.GetDisplayName(game.Turn) + " called a " + runName + ".");
                        emotes.Add(runName);
                    }
                }
                foreach (Carre carre in game.Carres[game.Turn])
                {
                    if (carre.Declared)
                    {
                        SysAnnounce(game.GetDisplayName(game.Turn) + " called a Carre.");
                        emotes.Add("Carre");
                    }
                }
            }
            if (emotes.Count > 0)
            {
                string[] seatPos = new string[] { "w", "n", "e", "s" };
                Clients.All.SetExtrasEmote(new JavaScriptSerializer().Serialize(emotes), seatPos[game.Turn]);
                Emote(seatPos[game.Turn], botDelay);
            }
            log.Debug("Leaving AnnounceExtras.");
        }

        public void Emote(string seat, int duration)
        {
            log.Debug("Entering Emote.");
            Clients.All.ShowEmote(seat);
            Thread.Sleep(duration);
            Clients.All.HideEmote(seat);
            log.Debug("Leaving Emote.");
        }

        // -------------------- Points --------------------

        public void HubFinalisePoints()
        {
            log.Debug("Entering HubFinalisePoints.");
            string[] Result = new string[] { "", "Success" };
            string[] message = { "N/S", "call", "succeeded" };
            if (game.EWCalled)
            {
                Result = new string[] { "Success", "" };
                message[0] = "E/W";
            }


            if (!game.EWWonATrick) // capot
            {
                Result[1] = "Capot";
                message[3] += ", Capot";
            }
            else if (!game.NSWonATrick)
            {
                Result[0] = "Capot";
                message[3] += ", Capot";
            }

            if (game.EWCalled && game.EWRoundPoints <= game.NSRoundPoints) // inside
            {
                Result[0] = "Inside";
                message[2] = "failed";
                if (game.Capot) message[2] += ", Capot";
                message[2] += ", Inside";
            }
            else if (!game.EWCalled && game.NSRoundPoints <= game.EWRoundPoints)
            {
                Result[1] = "Inside";
                message[2] = "failed";
                if (game.Capot) message[2] += ", Capot";
                message[2] += ", Inside";
            }
            SysAnnounce(String.Join(" ", message) + ".");
            log.Debug("Leaving HubFinalisePoints.");
        }

        // -------------------- Seat Management --------------------

        public void BookSeat(int position) // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot
        {
            log.Debug("Entering BookSeat.");
            string[] seat = { "West", "North", "East", "South" };

            string requestor = GetCallerUsername();

            if (position == 8) // vacate to Spectator
            {
                if (spectators.Where(s => s.Username == requestor).Count() == 0)
                {
                    UnbookSeat();
                    spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    UpdateConnectedUsers();
                    Clients.Caller.SetRadio("x");
                }
                return;
            }

            string occupier;
            if (position > 3)
            {
                occupier = players[position - 4].Username;
            }
            else
            {
                occupier = players[position].Username;
            }

            if ((occupier == "" || occupier == botGUID) && position < 4) // empty seat or bot-occupied requested by human
            {
                UnbookSeat();
                if (spectators.Where(s => s.Username == requestor).Count() == 1) spectators.Remove(spectators.Where(s => s.Username == requestor).First());
                players[position] = new Player(requestor, Context.ConnectionId, true);
                UpdateConnectedUsers();
                Clients.All.SeatBooked(position, requestor);
                Clients.All.SetBotBadge(seat[position], false);
                Clients.Caller.SetRadio(seat[position]);
                //log.Information(requestor + " occupied the " + seat[position] + " seat.");
            }
            else if (occupier == "" && position > 3) // empty seat requested by bot
            {
                position -= 4;
                string botName = GetBotName(position);
                players[position] = new Player(botGUID, "", false);
                UpdateConnectedUsers();
                Clients.All.SeatBooked(position, botName);
                Clients.All.SetBotBadge(seat[position], true);
                //log.Information(botName + " occupied the " + seat[position] + " seat.");
            }
            // if bot occupied seat requested by bot -> do nothing
            else if (occupier == requestor && position > 3) // human assigns bot to his own occupied seat
            {
                position -= 4;
                string botName = GetBotName(position);
                UnbookSeat();
                spectators.Add(new Spectator(requestor, Context.ConnectionId));
                players[position] = new Player(botGUID, "", false);
                UpdateConnectedUsers();
                Clients.All.SeatBooked(position, botName);
                Clients.Caller.SetRadio("x");
                Clients.All.SetBotBadge(seat[position], true);
                //log.Information(botName + " occupied the " + seat[position] + " seat.");
            }
            // if human tries to occupy his own seat, do nothing
            else if (occupier != "" && occupier != botGUID && occupier != requestor) // human-occupied seat is requested by another human or by a bot on behalf of another human
            {
                Clients.Caller.SeatAlreadyBooked(occupier);
            }

            if (players.Where(s => s.Username != "").Count() == 4) Clients.All.EnableNewGame();
            log.Debug("Leaving BookSeat.");
        }

        public void UnbookSeat()
        {
            log.Debug("Entering UnbookSeat.");
            string username = GetCallerUsername();

            if (spectators.Where(s => s.Username == username).Count() == 0)
            {
                Clients.All.DisableNewGame();
                int position = Array.IndexOf(players, players.Where(p => p.Username == username).First());
                players[position] = new Player();
                UpdateConnectedUsers();

                Clients.All.SeatUnbooked(position, username);
                string[] seat = { "West", "North", "East", "South" };
                //log.Information(username + " vacated the " + seat[position] + " seat.");
            }
            log.Debug("Leaving UnbookSeat.");
        }

        // -------------------- Messaging & Alerts --------------------

        public string MsgHead()
        {
            return GetServerDateTime() + ", " + GetCallerUsername();
        }

        [HubMethodName("announce")] //client-side name for the method may differ from server-side name
        public void Announce(string message)
        {
            Clients.All.Announce(MsgHead() + " >> " + message);
            Clients.Others.showChatNotification();
        }

        public void SysAnnounce(string message)
        {
            //log.Information(message);
            Clients.All.Announce(GetServerDateTime() + " >> " + message);
        }

        // -------------------- Get Stuff --------------------

        public string GetServerDateTime()
        {
            //return DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            return DateTime.Now.ToString("HH:mm");
        }

        public string GetCallerUsername()
        {
            if (Context.User.Identity.IsAuthenticated)
            {
                return Context.User.Identity.Name;
            }
            else
            {
                return "Unknown"; // This should never happen
            }
        }

        public string GetBotName(int pos)
        {
            string[] seat = { "West", "North", "East", "South" };
            return "Robot " + seat[pos];
        }

        public string GetDisplayName(int pos)
        {
            if (players[pos].IsHuman)
            {
                return players[pos].Username;
            }
            else
            {
                return GetBotName(pos);
            }
        }

        // -------------------- Connection --------------------

        public void UpdateConnectedUsers()
        {
            string[] playerNames = players.Select(s => s.Username).Where(s => s != "").Where(s => s != botGUID).ToArray();
            Array.Sort(playerNames);
            string[] specNames = spectators.Select(s => s.Username).ToArray();
            Array.Sort(specNames);
            Clients.All.ConnectedUsers(playerNames, specNames);
        }

        public void LoadContext()
        {
            for (int i = 0; i < 4; i++)
            {
                if (players[i].IsHuman)
                {
                    Clients.Caller.SeatBooked(i, players[i].Username);

                }
                else if (players[i].Username == botGUID)
                {
                    string[] seat = { "West", "North", "East", "South" };
                    Clients.Caller.SeatBooked(i, GetBotName(i));
                    Clients.Caller.SetBotBadge(seat[i], true);
                }
            }

            if (players.Where(s => s.Username != "").Count() == 4) Clients.Caller.EnableNewGame();
        }

        public override Task OnConnected()
        {
            log.Debug("Entering OnConnected.");
            string username = GetCallerUsername();
            if (players.Where(p => p.Username == username).Count() == 0) spectators.Add(new Spectator(username, Context.ConnectionId));
            UpdateConnectedUsers();

            SysAnnounce(username + " connected.");
            log.Information(username + " Connected.");

            LoadContext();
            log.Debug("Leaving OnConnected.");
            return base.OnConnected();
        }
        public override Task OnDisconnected(bool stopCalled = true)
        {
            log.Debug("Entering OnDisconnected.");
            string username = GetCallerUsername();
            if (spectators.Where(p => p.Username == username).Count() == 1) spectators.Remove(spectators.Where(s => s.Username == username).First());
            UpdateConnectedUsers();

            UnbookSeat();
            SysAnnounce(username + " disconnected.");
            log.Information(username + " disconnected.");
            //Clients.All.connectedUsers((new JavaScriptSerializer()).Serialize(connectedUsers));

            log.Debug("Leaving OnDisconnected.");
            return base.OnDisconnected(stopCalled);
        }
    }
}