using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using System.Configuration;

namespace BelotWebApp
{
    [HubName("room")] // Attribute -> client-side name for the class may differ from server-side name
    public class ChatRoom : Hub
    {
        public static List<BelotGame> games = new List<BelotGame>();

        public static Dictionary<string, string> allConnections = new Dictionary<string, string>();

        public static int scoreTarget = 1501;
        public static string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";

        public static AgentBasic basic = new AgentBasic();

        public static Serilog.Core.Logger log = new LoggerConfiguration().WriteTo.File(ConfigurationManager.AppSettings["logfilepath"] + "BelotServerLog-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        public ChatRoom()
        {
            //log.Information("Creating new Chat Room");
        }

        // -------------------- Main --------------------

        public async Task GameController()
        {
            log.Information("Entering GameController.");
            BelotGame game = GetGame();
            if (game.IsNewGame)
            {
                game.IsNewGame = false;
                Clients.Group(GetRoomId()).HideDeck(true);
                Clients.Group(GetRoomId()).DisableNewGame();
                Clients.Group(GetRoomId()).CloseModalsAndButtons();
                Clients.Group(GetRoomId()).DisableRadios();
                game.NewGame();
                Clients.Group(GetRoomId()).NewGame(game.GameId); // reset score table (offcanvas), reset score totals (card table), hide winner markers, set game id
            }
            while (((game.EWTotal < scoreTarget && game.NSTotal < scoreTarget) || game.EWTotal == game.NSTotal || game.Capot) && !game.WaitDeal && !game.WaitCall & !game.WaitCard)
            {
                await Task.Run(() => RoundController());
            }

            if (!game.WaitDeal && !game.WaitCall & !game.WaitCard) EndGame();
            log.Information("Leaving GameController.");
        }

        public void RoundController()
        {
            log.Information("Entering RoundController.");
            BelotGame game = GetGame();
            if (game.IsNewRound)
            {
                game.IsNewRound = false;
                game.NewRound();
                Clients.Group(GetRoomId()).SetTurnIndicator(game.Turn); // show dealer
                Clients.Group(GetRoomId()).SetDealerMarker(game.Turn);
                Clients.Group(GetRoomId()).NewRound(); // reset table, reset board, disable cards, reset suit selection 
                if (game.Players[game.Turn].IsHuman)
                {
                    game.WaitDeal = true;
                    Clients.Client(game.Players[game.Turn].ConnectionId).EnableDealBtn();
                    return;
                }
                else
                {
                    Thread.Sleep(game.BotDelay);
                }
            }

            if (!game.WaitDeal && game.CardsDealt == 0)
            {
                game.Shuffle();
                game.Deal(5);
                for (int i = 0; i < 4; i++)
                {
                    if (game.Players[i].IsHuman)
                    {
                        Clients.Client(game.Players[i].ConnectionId).Deal(new JavaScriptSerializer().Serialize(game.Hand[i]));
                        Clients.Client(game.Players[i].ConnectionId).RotateCards();
                    }
                }
            }

            if (game.NumCardsPlayed == 0)
            {
                while (!game.SuitDecided() && !game.WaitCall)
                {
                    Clients.Group(GetRoomId()).SetTurnIndicator(game.Turn);
                    CallController();
                }
            }

            if (game.RoundSuit == 0 && !game.WaitCall)
            {
                SysAnnounce("No suit chosen.");
                game.IsNewRound = true;
            }
            else if (game.RoundSuit == 9) game.IsNewRound = true;
            else if (game.RoundSuit != 0 && !game.WaitCall)
            {
                if (game.NumCardsPlayed == 0)
                {
                    SysAnnounce("The round will be played in " + BelotHelpers.GetSuitNameFromNumber(game.RoundSuit) + ".");
                    game.Turn = game.FirstPlayer;
                    Clients.Group(GetRoomId()).SetTurnIndicator(game.Turn);
                    game.Deal(3);
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.Players[i].IsHuman)
                        {
                            Clients.Client(game.Players[i].ConnectionId).Deal(new JavaScriptSerializer().Serialize(game.Hand[i]));
                            Clients.Client(game.Players[i].ConnectionId).RotateCards();
                        }
                    }
                    if (game.RoundSuit != 5)
                    {
                        game.FindRuns();
                        game.FindCarres();
                        game.TruncateRuns();
                        game.FindBelots();
                    }
                }
                while (game.NumCardsPlayed < 32 && !game.WaitCard)
                {
                    Clients.Group(GetRoomId()).SetTurnIndicator(game.Turn);
                    TrickController();
                }
                if (game.NumCardsPlayed == 32)
                {
                    Clients.Group(GetRoomId()).NewRound();
                    SysAnnounce(game.FinalisePoints());
                    //HubFinalisePoints();
                    game.ScoreHistory.Add(new int[] { game.EWRoundPoints, game.NSRoundPoints });
                    Clients.Group(GetRoomId()).AppendScoreHistory(game.EWRoundPoints, game.NSRoundPoints);
                    Clients.Group(GetRoomId()).UpdateScoreTotals(game.EWTotal, game.NSTotal);
                    Clients.Group(GetRoomId()).ShowRoundSummary(game.TrickPoints, game.DeclarationPoints, game.BelotPoints, game.Result, game.EWRoundPoints, game.NSRoundPoints);
                    Thread.Sleep(game.RoundSummaryDelay);
                    Clients.Group(GetRoomId()).HideRoundSummary();
                    game.IsNewRound = true;
                }
            }
            log.Information("Leaving RoundController.");
        }

        public void CallController()
        {
            log.Information("Entering CallController.");
            BelotGame game = GetGame();
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
                bool fiveUnderNine = false;
                if (game.SuitCall.Count < 4) fiveUnderNine = BelotHelpers.FiveUnderNine(game.Hand[game.Turn]);
                Clients.Client(game.Players[game.Turn].ConnectionId).ShowSuitModal(validCalls, fiveUnderNine);
                game.WaitCall = true;
            }
            else // bot
            {
                game.NominateSuit(basic.CallSuit(game.Hand[game.Turn], validCalls));
                //Thread.Sleep(botDelay);
                AnnounceSuit();
                if (--game.Turn == -1) game.Turn = 3;
            }
            log.Information("Leaving CallController.");
        }

        public void TrickController()
        {
            log.Information("Entering TrickController.");
            BelotGame game = GetGame();
            while (game.PlayedCards.Where(c => c != "c0-00").Count() < 4 && !game.WaitCard)
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
                    game.WaitCard = true;
                    if (game.PlayedCards.Where(c => c != "c0-00").Count() == 0)
                    {
                        if (game.GetWinners(game.Turn).Where(w => w == 2).Count() == game.Hand[game.Turn].Where(c => c != "c0-00").Count() && game.NumCardsPlayed > 4)
                        {
                            Clients.Client(game.Players[game.Turn].ConnectionId).ShowThrowBtn();
                        }
                    }
                    Clients.Client(game.Players[game.Turn].ConnectionId).enableCards(validCards);
                    // once a card is clicked, declarable extras are calculated in hub method, human selects and declares extras, then the card is played and game loop reinitiates
                }
                else
                {
                    string card = new AgentBasic().SelectCard(game.Hand[game.Turn], validCards, game.GetWinners(game.Turn), game.PlayedCards, game.Turn, game.DetermineWinner(), game.RoundSuit, game.TrickSuit, game.EWCalled);

                    game.PlayCard(card);

                    //List<string> extras = new List<string>();

                    if (game.RoundSuit != 5)
                    {
                        if (game.CheckBelot(card))
                        {
                            //extras.Add("Belot");
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
            if (!game.WaitCard) // trick end
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

                Clients.Group(GetRoomId()).ShowTrickWinner(winner);
                Thread.Sleep(1000);

                if (game.NumCardsPlayed < 32)
                {
                    Clients.Group(GetRoomId()).ResetTable();
                    game.Turn = winner;
                    game.PlayedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
                }
                game.HighestTrumpInTrick = 0;
                game.TrickSuit = 0;
            }
            log.Information("Leaving TrickController.");
        }

        // -------------------- Reset --------------------

        public void EndGame()
        {
            log.Information("Entering EndGame.");
            BelotGame game = GetGame();
            string winner = "N/S";
            if (game.EWTotal > game.NSTotal) winner = "E/W";
            SysAnnounce(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + ".");
            game.Log.Information(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + ".");

            Clients.Group(GetRoomId()).SetDealerMarker(4);
            Clients.Group(GetRoomId()).NewRound();
            Clients.Group(GetRoomId()).SetTurnIndicator(4);
            // fancy animation and modal to indicate winning team
            if (game.EWTotal > game.NSTotal)
            {
                Clients.Group(GetRoomId()).ShowGameWinner(0);
                Clients.Group(GetRoomId()).ShowGameWinner(2);
            }
            else
            {
                Clients.Group(GetRoomId()).ShowGameWinner(1);
                Clients.Group(GetRoomId()).ShowGameWinner(3);
            }
            Clients.Group(GetRoomId()).EnableNewGame();
            Clients.Group(GetRoomId()).EnableRadios();
            game.IsNewGame = true;
            game.CloseLog();
            log.Information("Leaving EndGame.");
        }

        // -------------------- Setup --------------------

        public void HubShuffle()
        {
            log.Information("Entering HubShuffle.");
            // this method is only called by human interaction with the html elements
            GetGame().WaitDeal = false;
            GameController();
            log.Information("Leaving HubShuffle.");
        }

        // -------------------- Suit Nomination --------------------

        public void HubNominateSuit(int suit)
        {

            log.Information("Entering HubNominateSuit.");
            BelotGame game = GetGame();
            game.NominateSuit(suit);

            // this method is only called by human interaction with the html elements
            game.WaitCall = false;
            AnnounceSuit();
            if (--game.Turn == -1) game.Turn = 3;
            GameController();
            log.Information("Leaving HubNominateSuit.");
        }

        public void AnnounceSuit()
        {
            log.Information("Entering AnnounceSuit.");
            BelotGame game = GetGame();
            string username = GetDisplayName(game.Turn);

            string message = username + " passed.";

            int suit = game.SuitCall[game.SuitCall.Count - 1];

            if (suit > 0)
            {
                Clients.Group(GetRoomId()).SuitNominated(suit);
                Clients.Group(GetRoomId()).setCallerIndicator(game.Turn);
                if (suit < 7)
                {
                    message = username + " called " + BelotHelpers.GetSuitNameFromNumber(suit) + ".";
                }
                else if (suit == 7)
                {
                    message = username + " doubled!";
                }
                else if (suit == 8)
                {
                    message = username + " redoubled!!";
                }
                else
                {
                    message = username + " called five-under-nine.";
                }
            }

            SysAnnounce(message);
            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.Group(GetRoomId()).EmoteSuit(suit, seatPos[game.Turn]);
            Emote(seatPos[game.Turn], game.BotDelay);
            log.Information("Leaving AnnounceSuit.");
        }

        // -------------------- Card Validation --------------------

        // -------------------- Gameplay --------------------

        public void HubPlayCard(string card)
        {
            log.Information("Entering HubPlayCard.");
            BelotGame game = GetGame();
            game.PlayCard(card);
            Clients.Caller.SetTableCard(game.Turn, game.PlayedCards[game.Turn]);

            List<string> extras = new List<string>();

            if (game.CheckBelot(card)) extras.Add("Belot: " + BelotHelpers.GetSuitNameFromNumber(Int32.Parse(card.Substring(1, 1))));

            if (game.RoundSuit != 5 && game.NumCardsPlayed < 5)
            {
                for (int i = 0; i < game.Runs[game.Turn].Count; i++)
                {
                    string extra = "";
                    if (!game.Runs[game.Turn][i].Declarable) extra += "#";
                    extra += BelotHelpers.GetRunNameFromLength(game.Runs[game.Turn][i].Length) + ": " + BelotHelpers.GetSuitNameFromNumber(game.Runs[game.Turn][i].Suit) + " " + BelotHelpers.GetCardRankFromNumber(game.Runs[game.Turn][i].Strength - game.Runs[game.Turn][i].Length + 1) + "→" + BelotHelpers.GetCardRankFromNumber(game.Runs[game.Turn][i].Strength);
                    extras.Add(extra);
                }

                for (int i = 0; i < game.Carres[game.Turn].Count; i++)
                {
                    extras.Add("Carre: " + BelotHelpers.GetCardRankFromNumber(game.Carres[game.Turn][i].Rank));
                }
            }
            game.CurrentExtras = extras; // store extras in case client disconnects after playing card, before declaring extras
            Clients.Caller.DeclareExtras(new JavaScriptSerializer().Serialize(extras));
            log.Information("Leaving HubPlayCard.");
        }

        public void CardPlayEnd()
        {
            log.Information("Entering CardPlayEnd.");
            BelotGame game = GetGame();
            Clients.Group(GetRoomId()).SetTableCard(game.Turn, game.PlayedCards[game.Turn]);
            Thread.Sleep(game.BotDelay);
            if (game.NumCardsPlayed % 4 != 0) if (--game.Turn == -1) game.Turn = 3;
            if (game.NumCardsPlayed < 32) Clients.Group(GetRoomId()).SetTurnIndicator(game.Turn);
            log.Information("Leaving CardPlayEnd.");
        }

        public void HubExtrasDeclared(bool belot, bool[] runs, bool[] carres)
        {
            log.Information("Entering HubExtrasDeclared.");
            BelotGame game = GetGame();
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
            game.WaitCard = false;
            GameController();
            log.Information("Leaving HubExtrasDeclared.");
        }

        public void AnnounceExtras(bool belotDeclared)
        {
            log.Information("Entering AnnounceExtras.");
            BelotGame game = GetGame();
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
                        string runName = BelotHelpers.GetRunNameFromLength(run.Length);
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
                Clients.Group(GetRoomId()).SetExtrasEmote(new JavaScriptSerializer().Serialize(emotes), seatPos[game.Turn]);
                Emote(seatPos[game.Turn], game.BotDelay);
            }
            log.Information("Leaving AnnounceExtras.");
        }

        public void Emote(string seat, int duration)
        {
            log.Information("Entering Emote.");
            Clients.Group(GetRoomId()).ShowEmote(seat);
            Thread.Sleep(duration);
            Clients.Group(GetRoomId()).HideEmote(seat);
            log.Information("Leaving Emote.");
        }

        public void ThrowCards()
        {
            log.Information("Entering ThrowCards.");
            BelotGame game = GetGame();

            int points = 10; // stoch

            for (int i = 0; i < 4; i++)
            {
                foreach (string card in game.Hand[i].Where(c => c != "c0-00"))
                {
                    points += game.CalculateCardPoints(card);
                }
            }

            if (game.Turn % 2 == 0) game.EWRoundPoints += points;
            else game.NSRoundPoints += points;

            for (int i = 0; i < 4; i++)
            {
                foreach (Belot belot in game.Belots[i])
                {
                    if (belot.Declarable == true) belot.Declared = true;
                }
            }

            game.NumCardsPlayed = 32;
            game.WaitCard = false;

            Clients.Group(GetRoomId()).throwCards(GetDisplayName(game.Turn), new JavaScriptSerializer().Serialize(game.Hand));
            Thread.Sleep(3500);
            Clients.Group(GetRoomId()).CloseThrowModal();

            GameController();

            //Thread.Sleep(duration);
            log.Information("Leaving ThrowCards.");
        }

        // -------------------- Points --------------------

        public void HubFinalisePoints()
        {
            log.Information("Entering HubFinalisePoints.");
            BelotGame game = GetGame();
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
            log.Information("Leaving HubFinalisePoints.");
        }

        // -------------------- Seat Management --------------------

        public void BookSeat(int position) // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot
        {
            log.Information("Entering BookSeat.");
            BelotGame game = GetGame();
            string[] seat = { "West", "North", "East", "South" };

            string requestor = GetCallerUsername();

            if (position == 8) // vacate to Spectator
            {
                if (game.Spectators.Where(s => s.Username == requestor).Count() == 0)
                {
                    UnbookSeat();
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    UpdateConnectedUsers();
                    //Clients.Caller.SetRadio("x");
                }
                return;
            }

            string occupier;
            if (position > 3)
            {
                occupier = game.Players[position - 4].Username;
            }
            else
            {
                occupier = game.Players[position].Username;
            }

            if ((occupier == "" || occupier == botGUID) && position < 4) // empty seat or bot-occupied requested by human
            {
                UnbookSeat();
                if (game.Spectators.Where(s => s.Username == requestor).Count() == 1) game.Spectators.Remove(game.Spectators.Where(s => s.Username == requestor).First());
                game.Players[position] = new Player(requestor, Context.ConnectionId, true);
                UpdateConnectedUsers();
                Clients.OthersInGroup(GetRoomId()).SeatBooked(position, requestor, false);
                Clients.Caller.SeatBooked(position, requestor, true);
                Clients.Caller.EnableRotation(true);
                string[] scoreSummary = { "Us", "Them" };
                Clients.Caller.SetScoreTitles(scoreSummary[(position + 1) % 2], scoreSummary[position % 2]);

                Clients.OthersInGroup(GetRoomId()).EnableSeatOptions(position, false);
                Clients.Group(GetRoomId()).EnableOccupySeat(position, false);
                Clients.OthersInGroup(GetRoomId()).EnableAssignBotToSeat(position, false);
                Clients.Caller.EnableAssignBotToSeat(position, true);
                Clients.Caller.EnableVacateSeat(position, true);

                Clients.Group(GetRoomId()).SetBotBadge(position, false);
                //Clients.Caller.SetRadio(seat[position]);
                //log.Information(requestor + " occupied the " + seat[position] + " seat.");
            }
            else if (occupier == "" && position > 3) // empty seat requested by bot
            {
                position -= 4;
                string botName = GetBotName(position);
                game.Players[position] = new Player(botGUID, "", false);
                UpdateConnectedUsers();
                Clients.Group(GetRoomId()).SeatBooked(position, botName, false);
                Clients.Group(GetRoomId()).SetBotBadge(position, true);
                Clients.Group(GetRoomId()).EnableAssignBotToSeat(position, false);
                Clients.Caller.EnableVacateSeat(position, false);
                //log.Information(botName + " occupied the " + seat[position] + " seat.");
            }
            // if bot occupied seat requested by bot -> do nothing
            else if (occupier == requestor && position > 3) // human assigns bot to his own occupied seat
            {
                position -= 4;
                string botName = GetBotName(position);
                UnbookSeat();
                game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                game.Players[position] = new Player(botGUID, "", false);
                UpdateConnectedUsers();
                Clients.Group(GetRoomId()).SeatBooked(position, botName, false);
                //Clients.Caller.SetRadio("x");
                Clients.Group(GetRoomId()).SetBotBadge(position, true);
                Clients.Group(GetRoomId()).EnableOccupySeat(position, true);
                Clients.Group(GetRoomId()).EnableAssignBotToSeat(position, false);
                //log.Information(botName + " occupied the " + seat[position] + " seat.");
            }
            // if human tries to occupy his own seat, do nothing
            else if (occupier != "" && occupier != botGUID && occupier != requestor) // human-occupied seat is requested by another human or by a bot on behalf of another human
            {
                Clients.Caller.SeatAlreadyBooked(occupier);
            }

            if (game.Players.Where(s => s.Username != "").Count() == 4) Clients.Group(GetRoomId()).EnableNewGame();
            log.Information("Leaving BookSeat.");
        }

        public void UnbookSeat()
        {
            log.Information("Entering UnbookSeat.");
            BelotGame game = GetGame();
            string username = GetCallerUsername();

            if (game.Players.Where(s => s.Username == username).Count() == 1)
            {
                int position = Array.IndexOf(game.Players, game.Players.Where(p => p.Username == username).First());
                if (game.IsNewGame)
                {
                    Clients.Group(GetRoomId()).DisableNewGame();
                    game.Players[position] = new Player();
                }
                else game.Players[position].IsDisconnected = true;
                Clients.Caller.EnableRotation(false);
                Clients.Caller.SetScoreTitles("N/S", "E/W");
                Clients.Group(GetRoomId()).SeatUnbooked(position);
                Clients.Group(GetRoomId()).EnableSeatOptions(position, true);
                Clients.Group(GetRoomId()).EnableOccupySeat(position, true);
                Clients.Group(GetRoomId()).EnableAssignBotToSeat(position, true);
                Clients.Group(GetRoomId()).EnableVacateSeat(position, false);
                //string[] seat = { "West", "North", "East", "South" };
                //log.Information(username + " vacated the " + seat[position] + " seat.");
            }
            UpdateConnectedUsers();
            log.Information("Leaving UnbookSeat.");
        }

        // -------------------- Messaging & Alerts --------------------

        public string MsgHead()
        {
            return GetServerDateTime() + ", " + GetCallerUsername();
        }

        [HubMethodName("announce")] //client-side name for the method may differ from server-side name
        public void Announce(string message)
        {
            Clients.Group(GetRoomId()).Announce(MsgHead() + " >> " + message);
            Clients.Group(GetRoomId()).showChatNotification();
        }

        public void SysAnnounce(string message)
        {
            //log.Information(message);
            Clients.Group(GetRoomId()).Announce(GetServerDateTime() + " >> " + message);
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
            if (GetGame().Players[pos].IsHuman)
            {
                return GetGame().Players[pos].Username;
            }
            else
            {
                return GetBotName(pos);
            }
        }

        public string GetRoomId()
        {
            allConnections.TryGetValue(Context.ConnectionId, out string roomId);
            return roomId;
        }

        public BelotGame GetGame()
        {
            allConnections.TryGetValue(Context.ConnectionId, out string roomId);
            return games.Where(i => i.RoomId == roomId).First();
        }

        // -------------------- Connection --------------------

        public void UpdateConnectedUsers()
        {
            BelotGame game = GetGame();
            string[] playerNames = game.Players.Where(d => d.IsDisconnected == false).Select(s => s.Username).Where(s => s != "").Where(s => s != botGUID).ToArray();
            Array.Sort(playerNames);
            string[] specNames = game.Spectators.Select(s => s.Username).ToArray();
            Array.Sort(specNames);
            Clients.Group(GetRoomId()).ConnectedUsers(playerNames, specNames);
            //Clients.Caller.ConnectedUsers(playerNames, specNames);
        }

        public void LoadContext()
        {
            BelotGame game = GetGame();

            for (int i = 0; i < 4; i++)
            {
                // Update table seats
                if (game.Players[i].IsHuman)
                {
                    Clients.Caller.EnableOccupySeat(i, false);
                    if (game.Players[i].Username == Context.User.Identity.Name)
                    {
                        Clients.Caller.SeatBooked(i, game.Players[i].Username, true);
                        Clients.Caller.EnableVacateSeat(i, true);
                    }
                    else
                    {
                        Clients.Caller.SeatBooked(i, game.Players[i].Username, false);
                        Clients.Caller.EnableSeatOptions(i, false);
                        Clients.Caller.EnableAssignBotToSeat(i, false);
                    }
                }
                else if (game.Players[i].Username == botGUID)
                {
                    string[] seat = { "West", "North", "East", "South" };
                    Clients.Caller.SeatBooked(i, GetBotName(i), false);
                    Clients.Caller.SetBotBadge(i, true);
                    Clients.Caller.EnableAssignBotToSeat(i, false);
                }

                // Update table cards
                if (!game.IsNewGame) Clients.Caller.SetTableCard(i, game.PlayedCards[i]);
            }

            if (!game.IsNewGame)
            {
                Clients.Caller.HideDeck(true);

                int dealer = game.FirstPlayer + 1;
                if (dealer == 4) dealer = 0;

                Clients.Caller.SetDealerMarker(dealer);

                Clients.Caller.SetTurnIndicator(game.Turn);

                Clients.Caller.DisableRadios();

                Clients.Caller.UpdateScoreTotals(game.EWTotal, game.NSTotal);

                for (int i = 0; i < game.ScoreHistory.Count; i++)
                {
                    Clients.Caller.AppendScoreHistory(game.ScoreHistory[i][0], game.ScoreHistory[i][1]);
                }

                Clients.Caller.SuitNominated(game.RoundSuit);
                if (game.Multiplier == 2) Clients.Caller.SuitNominated(7);
                else if (game.Multiplier == 4) Clients.Caller.SuitNominated(8);

                if (game.RoundSuit > 0) Clients.Caller.setCallerIndicator(game.Caller);

                // if the connecting user is a player
                if (game.Players.Where(u => u.Username == GetCallerUsername()).Count() > 0)
                {
                    int pos = Array.IndexOf(game.Players, game.Players.Where(p => p.Username == GetCallerUsername()).First());

                    Clients.Caller.EnableRotation(true);
                    string[] scoreSummary = { "Us", "Them" };
                    Clients.Caller.SetScoreTitles(scoreSummary[(pos + 1) % 2], scoreSummary[pos % 2]);

                    Clients.Caller.Deal(new JavaScriptSerializer().Serialize(game.Hand[pos]));

                    for (int i = 0; i < 8; i++)
                    {
                        if (i < game.Hand[pos].Count)
                        {
                            if (game.Hand[pos][i] == "c0-00")
                            {
                                Clients.Caller.HideCard("card" + i);
                            }
                        }
                        else
                        {
                            Clients.Caller.HideCard("card" + i);
                        }
                    }
                    Clients.Caller.RotateCards();

                    if (game.Turn == pos)
                    {
                        // deal
                        if (dealer == pos && game.Hand[pos].Count == 0)
                        {
                            Clients.Caller.EnableDealBtn();
                        }
                        // if the game is in the suit-calling phase
                        else if (game.Hand[pos].Count == 5)
                        {
                            int[] validCalls = game.ValidCalls();
                            bool fiveUnderNine = false;
                            if (game.SuitCall.Count < 4) fiveUnderNine = BelotHelpers.FiveUnderNine(game.Hand[game.Turn]);
                            Clients.Caller.ShowSuitModal(validCalls, fiveUnderNine);
                        }
                        // if the connecting user must declare extras
                        else if (game.PlayedCards[game.Turn] != "c0-00")
                        {
                            Clients.Caller.DeclareExtras(new JavaScriptSerializer().Serialize(game.CurrentExtras));
                        }
                        // if the game is in the card-playing phase
                        else if (game.Hand[pos].Count == 8)
                        {
                            Clients.Caller.EnableCards(game.ValidCards());
                        }
                    }
                }
                else
                {
                    //for (int i = 0; i < 8; i++)
                    //{
                    //    Clients.Caller.HideCard("card" + i);
                    //}
                }
            }
            else if (game.Players.Where(s => s.Username != "").Count() == 4) Clients.Caller.EnableNewGame();
        }

        public override async Task OnConnected()
        {
            log.Information("Entering OnConnected.");

            var roomId = Context.Headers["Referer"].Substring(Context.Headers["Referer"].Length - 36, 36);

            Clients.Caller.SetRoomId(roomId);

            allConnections.Add(Context.ConnectionId, roomId);

            await Groups.Add(Context.ConnectionId, roomId);

            BelotGame game = GetGame();

            Clients.Caller.SetGameId(game.GameId);

            string username = GetCallerUsername();
            IEnumerable<Player> players = game.Players.Where(p => p.Username == username);
            if (players.Count() == 0) game.Spectators.Add(new Spectator(username, Context.ConnectionId));
            else
            {
                players.First().ConnectionId = Context.ConnectionId;
                players.First().IsDisconnected = false;
                int pos = Array.IndexOf(game.Players, players.First());
                Clients.OthersInGroup(GetRoomId()).SeatBooked(pos, username, false);
            }
            UpdateConnectedUsers();

            SysAnnounce(username + " connected.");
            log.Information(username + " Connected.");

            LoadContext();
            log.Information("Leaving OnConnected.");
            await base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled = true)
        {
            log.Information("Entering OnDisconnected.");

            BelotGame game = GetGame();

            string username = GetCallerUsername();
            if (game.Spectators.Where(p => p.Username == username).Count() == 1) game.Spectators.Remove(game.Spectators.Where(s => s.Username == username).First());
            UnbookSeat();
            SysAnnounce(username + " disconnected.");
            log.Information(username + " disconnected.");
            //Clients.Group(GetGameId()).connectedUsers((new JavaScriptSerializer()).Serialize(connectedUsers));

            if (game.Spectators.Count() + game.Players.Where(h => h.IsHuman == true).Where(d => d.IsDisconnected == false).Count() == 0)
            {

                int oldwinnerDelay = game.WinnerDelay;
                int oldBotDelay = game.BotDelay;
                int oldRoundSummaryDelay = game.RoundSummaryDelay;
                game.WinnerDelay = 0;
                game.BotDelay = 0;
                game.RoundSummaryDelay = 0;
                Thread.Sleep(1500);
                game.WinnerDelay = oldwinnerDelay;
                game.BotDelay = oldBotDelay;
                game.RoundSummaryDelay = oldRoundSummaryDelay;

                if (game.Spectators.Count() + game.Players.Where(h => h.IsHuman == true).Where(d => d.IsDisconnected == false).Count() == 0)
                {
                    games.Remove(game);
                    game.CloseLog();
                    game = null;

                    //game.Players = new Player[] { new Player(), new Player(), new Player(), new Player() };
                }
            }
            allConnections.Remove(Context.ConnectionId);
            log.Information("Leaving OnDisconnected.");
            return base.OnDisconnected(stopCalled);
        }
    }
}