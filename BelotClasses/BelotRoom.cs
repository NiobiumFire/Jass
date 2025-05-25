using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Players;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System.Text.Json;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoom : Hub
    {
        public static List<BelotGame> games = [];

        public static Dictionary<string, string> allConnections = [];

        public static readonly int scoreTarget = 1501;
        public static readonly string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";

        public static Serilog.Core.Logger log;// = new LoggerConfiguration().WriteTo.File(ConfigurationManager.AppSettings["logfilepath"] + "BelotServerLog-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        public BelotRoom(IConfiguration _config)
        {
            log ??= new LoggerConfiguration().WriteTo.File(_config.GetSection("SerilogPath:Path").Value + "BelotServerLog-.txt", rollingInterval: RollingInterval.Day).CreateLogger();
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
                await Clients.Group(GetRoomId()).SendAsync("HideDeck", true);
                await Clients.Group(GetRoomId()).SendAsync("DisableNewGame");
                await Clients.Group(GetRoomId()).SendAsync("CloseModalsAndButtons");
                await Clients.Group(GetRoomId()).SendAsync("DisableRadios");
                game.NewGame();
                await Clients.Group(GetRoomId()).SendAsync("NewGame", game.GameId); // reset score table (offcanvas), reset score totals (card table), hide winner markers, set game id
            }
            while (((game.EWTotal < scoreTarget && game.NSTotal < scoreTarget) || game.EWTotal == game.NSTotal || game.Capot) && !game.WaitDeal && !game.WaitCall & !game.WaitCard)
            {
                //await Task.Run(() => RoundController());
                await RoundController();
            }

            //if (!game.WaitDeal && !game.WaitCall & !game.WaitCard) await Task.Run(() => EndGame());
            if (!game.WaitDeal && !game.WaitCall & !game.WaitCard) await EndGame();
            log.Information("Leaving GameController.");
        }

        public async Task RoundController()
        {
            log.Information("Entering RoundController.");
            BelotGame game = GetGame();
            if (game.IsNewRound)
            {
                game.IsNewRound = false;
                game.NewRound();
                await Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", game.Turn); // show dealer
                await Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", game.Turn); // show dealer
                await Clients.Group(GetRoomId()).SendAsync("SetDealerMarker", game.Turn);
                await Clients.Group(GetRoomId()).SendAsync("NewRound"); // reset table, reset board, disable cards, reset suit selection 
                if (game.Players[game.Turn].IsHuman)
                {
                    game.WaitDeal = true;
                    await Clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("EnableDealBtn");
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
                        await Clients.Client(game.Players[i].ConnectionId).SendAsync("Deal", game.Hand[i]);
                        await Clients.Client(game.Players[i].ConnectionId).SendAsync("RotateCards");
                    }
                }
            }

            if (game.NumCardsPlayed == 0)
            {
                while (!game.SuitDecided() && !game.WaitCall)
                {
                    await Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", game.Turn);
                    //await Task.Run(() => CallController());
                    await CallController();
                }
            }

            if (game.RoundCall == Call.Pass && !game.WaitCall)
            {
                //await Task.Run(() => SysAnnounce("No suit chosen."));
                await SysAnnounce("No suit chosen.");
                game.IsNewRound = true;
            }
            else if (game.RoundCall == Call.FiveUnderNine)
            {
                game.IsNewRound = true;
            }
            else if (game.RoundCall != 0 && !game.WaitCall)
            {
                if (game.NumCardsPlayed == 0)
                {
                    //await Task.Run(() => SysAnnounce("The round will be played in " + BelotHelpers.GetSuitNameFromNumber(game.RoundSuit) + "."));
                    await SysAnnounce("The round will be played in " + BelotHelpers.GetSuitNameFromNumber(game.RoundCall) + ".");
                    game.Turn = game.FirstPlayer;
                    await Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", game.Turn);
                    game.Deal(3);
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.Players[i].IsHuman)
                        {
                            await Clients.Client(game.Players[i].ConnectionId).SendAsync("Deal", game.Hand[i]);
                            await Clients.Client(game.Players[i].ConnectionId).SendAsync("RotateCards");
                        }
                    }
                    if (game.RoundCall != Call.NoTrumps)
                    {
                        game.FindRuns();
                        game.FindCarres();
                        game.TruncateRuns();
                        game.FindBelots();
                    }
                }
                while (game.NumCardsPlayed < 32 && !game.WaitCard)
                {
                    await Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", game.Turn);
                    //await Task.Run(() => TrickController());
                    await TrickController();
                }
                if (game.NumCardsPlayed == 32)
                {
                    await Clients.Group(GetRoomId()).SendAsync("NewRound");
                    //await Task.Run(() => SysAnnounce(game.FinalisePoints()));
                    await SysAnnounce(game.FinalisePoints());
                    //HubFinalisePoints();
                    game.ScoreHistory.Add(new int[] { game.EWRoundPoints, game.NSRoundPoints });
                    await Clients.Group(GetRoomId()).SendAsync("AppendScoreHistory", game.EWRoundPoints, game.NSRoundPoints);
                    await Clients.Group(GetRoomId()).SendAsync("UpdateScoreTotals", game.EWTotal, game.NSTotal);
                    await Clients.Group(GetRoomId()).SendAsync("ShowRoundSummary", game.TrickPoints, game.DeclarationPoints, game.BelotPoints, game.Result, game.EWRoundPoints, game.NSRoundPoints);
                    Thread.Sleep(game.RoundSummaryDelay);
                    await Clients.Group(GetRoomId()).SendAsync("HideRoundSummary");
                    game.IsNewRound = true;
                }
            }
            log.Information("Leaving RoundController.");
        }

        public async Task CallController()
        {
            log.Information("Entering CallController.");
            BelotGame game = GetGame();
            int[] validCalls = game.ValidCalls();
            if (validCalls.Sum() == 0)
            {
                game.NominateSuit(0); // auto-pass
                //Thread.Sleep(botDelay);
                //await Task.Run(() => AnnounceSuit());
                await AnnounceSuit();
                if (--game.Turn == -1) game.Turn = 3;
            }
            else if (game.Players[game.Turn].IsHuman)
            {
                bool fiveUnderNine = game.Calls.Count < 4 && BelotHelpers.FiveUnderNine(game.Hand[game.Turn]);
                await Clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("ShowSuitModal", validCalls, fiveUnderNine);
                game.WaitCall = true;
            }
            else // bot
            {
                game.NominateSuit(AgentBasic.CallSuit(game.Hand[game.Turn], validCalls));
                //Thread.Sleep(botDelay);
                //await Task.Run(() => AnnounceSuit());
                await AnnounceSuit();
                if (--game.Turn == -1) game.Turn = 3;
            }
            log.Information("Leaving CallController.");
        }

        public async Task TrickController()
        {
            log.Information("Entering TrickController.");
            BelotGame game = GetGame();
            while (game.TableCards.Count(c => !c.IsNull()) < 4 && !game.WaitCard)
            {
                if (game.Hand[game.Turn].Count(c => !c.Played) == 1) // auto-play last card
                {
                    if (game.Players[game.Turn].IsHuman)
                    {
                        await Clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("PlayFinalCard");
                        //game.Log.Information("Playing final card for human player");
                    }
                    game.PlayCard(game.Hand[game.Turn].FirstOrDefault(c => !c.Played)); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                    await CardPlayEnd();
                    continue;
                }
                bool belot = false;
                int[] validCards = game.ValidCards();
                if (game.Players[game.Turn].IsHuman)
                {
                    game.WaitCard = true;
                    if (game.TableCards.All(c => c.IsNull()))
                    {
                        if (game.GetWinners(game.Turn).Count(w => w == 2) == game.Hand[game.Turn].Count(c => !c.Played) && game.NumCardsPlayed > 3)
                        {
                            await Clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("ShowThrowBtn");
                        }
                    }
                    await Clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("EnableCards", validCards);
                    // once a card is clicked, declarable extras are calculated in hub method, human selects and declares extras, then the card is played and game loop reinitiates
                }
                else
                {
                    var card = AgentBasic.SelectCard(game.Hand[game.Turn], validCards, game.GetWinners(game.Turn), game.TableCards, game.Turn, game.DetermineWinner(), game.RoundCall, game.TrickSuit, game.EWCalled);

                    game.PlayCard(card);

                    //List<string> extras = new List<string>();

                    if (game.RoundCall != Call.NoTrumps)
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
                        //await Task.Run(() => AnnounceExtras(belot));
                        await AnnounceExtras(belot);
                    }

                    //await Task.Run(() => CardPlayEnd());
                    await CardPlayEnd();
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

                await Clients.Group(GetRoomId()).SendAsync("ShowTrickWinner", winner);
                Thread.Sleep(1000);

                if (game.NumCardsPlayed < 32)
                {
                    await Clients.Group(GetRoomId()).SendAsync("ResetTable");
                    game.Turn = winner;
                    game.TableCards = [new(), new(), new(), new()];
                }
                game.HighestTrumpInTrick = 0;
                game.TrickSuit = null;
            }
            log.Information("Leaving TrickController.");
        }

        // -------------------- Reset --------------------

        public async Task EndGame()
        {
            log.Information("Entering EndGame.");
            BelotGame game = GetGame();
            string winner = "N/S";
            if (game.EWTotal > game.NSTotal) winner = "E/W";
            //await Task.Run(() => SysAnnounce(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + "."));
            await SysAnnounce(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + ".");
            game.Log.Information(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + ".");

            await Clients.Group(GetRoomId()).SendAsync("SetDealerMarker", 4);
            await Clients.Group(GetRoomId()).SendAsync("NewRound");
            await Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", 4);
            // fancy animation and modal to indicate winning team
            if (game.EWTotal > game.NSTotal)
            {
                await Clients.Group(GetRoomId()).SendAsync("ShowGameWinner", 0);
                await Clients.Group(GetRoomId()).SendAsync("ShowGameWinner", 2);
            }
            else
            {
                await Clients.Group(GetRoomId()).SendAsync("ShowGameWinner", 1);
                await Clients.Group(GetRoomId()).SendAsync("ShowGameWinner", 3);
            }
            await Clients.Group(GetRoomId()).SendAsync("EnableNewGame");
            await Clients.Group(GetRoomId()).SendAsync("EnableRadios");
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

        public async Task HubNominateSuit(Call call)
        {

            log.Information("Entering HubNominateSuit.");
            BelotGame game = GetGame();
            game.NominateSuit(call);

            // this method is only called by human interaction with the html elements
            game.WaitCall = false;
            //await Task.Run(() => AnnounceSuit());
            await AnnounceSuit();
            if (--game.Turn == -1) game.Turn = 3;
            GameController();
            log.Information("Leaving HubNominateSuit.");
        }

        public async Task AnnounceSuit()
        {
            log.Information("Entering AnnounceSuit.");
            BelotGame game = GetGame();
            string username = GetDisplayName(game.Turn);

            string message = username;

            Call call = game.Calls[^1];

            if (call == Call.Pass)
            {
                message += " passed.";
            }
            else
            {
                await Clients.Group(GetRoomId()).SendAsync("SuitNominated", call);
                await Clients.Group(GetRoomId()).SendAsync("setCallerIndicator", game.Turn);

                if (call == Call.Double)
                {
                    message += " doubled!";
                }
                else if (call == Call.Redouble)
                {
                    message += " redoubled!!";
                }
                else if (call == Call.FiveUnderNine)
                {
                    message += " called five-under-nine.";
                }
                else
                {
                    message += " called " + BelotHelpers.GetSuitNameFromNumber(call) + ".";
                }
            }

            await SysAnnounce(message);
            await Clients.Group(GetRoomId()).SendAsync("EmoteSuit", call, game.Turn);
            await Emote(game.Turn, game.BotDelay);
            log.Information("Leaving AnnounceSuit.");
        }

        // -------------------- Card Validation --------------------

        // -------------------- Gameplay --------------------

        public async Task HubPlayCard(Card card)
        {
            log.Information("Entering HubPlayCard.");
            BelotGame game = GetGame();
            game.PlayCard(card);
            await Clients.Caller.SendAsync("SetTableCard", game.Turn, game.TableCards[game.Turn]);

            List<string> extras = [];

            if (game.CheckBelot(card))
            {
                extras.Add("Belot: " + BelotHelpers.GetSuitNameFromNumber((Call)card.Suit));
            }

            if (game.RoundCall != Call.NoTrumps && game.NumCardsPlayed < 5)
            {
                for (int i = 0; i < game.Runs[game.Turn].Count; i++)
                {
                    string extra = "";
                    if (!game.Runs[game.Turn][i].Declarable) extra += "#";
                    extra += BelotHelpers.GetRunNameFromLength(game.Runs[game.Turn][i].Length) + ": " +
                        BelotHelpers.GetSuitNameFromNumber((Call)game.Runs[game.Turn][i].Suit) + " " +
                        BelotHelpers.GetCardRankFromNumber(game.Runs[game.Turn][i].Rank - game.Runs[game.Turn][i].Length + 1) +
                        "→" + BelotHelpers.GetCardRankFromNumber(game.Runs[game.Turn][i].Rank);
                    extras.Add(extra);
                }

                for (int i = 0; i < game.Carres[game.Turn].Count; i++)
                {
                    extras.Add("Carre: " + BelotHelpers.GetCardRankFromNumber(game.Carres[game.Turn][i].Rank));
                }
            }
            game.CurrentExtras = extras; // store extras in case client disconnects after playing card, before declaring extras
            await Clients.Caller.SendAsync("DeclareExtras", extras);
            log.Information("Leaving HubPlayCard.");
        }

        public async Task CardPlayEnd()
        {
            log.Information("Entering CardPlayEnd.");
            BelotGame game = GetGame();
            await Clients.Group(GetRoomId()).SendAsync("SetTableCard", game.Turn, game.TableCards[game.Turn]);
            Thread.Sleep(game.BotDelay);
            if (game.NumCardsPlayed % 4 != 0) if (--game.Turn == -1) game.Turn = 3;
            if (game.NumCardsPlayed < 32) Clients.Group(GetRoomId()).SendAsync("SetTurnIndicator", game.Turn);
            log.Information("Leaving CardPlayEnd.");
        }

        public async Task HubExtrasDeclared(bool belot, bool[] runs, bool[] carres)
        {
            log.Information("Entering HubExtrasDeclared.");
            BelotGame game = GetGame();
            if (game.RoundCall != Call.NoTrumps)
            {
                if (belot) game.DeclareBelot(belot);
                if (game.NumCardsPlayed < 5)
                {
                    game.DeclareRuns(runs);
                    game.DeclareCarres(carres);
                }
                //await Task.Run(() => AnnounceExtras(belot));
                await AnnounceExtras(belot);
            }
            //await Task.Run(() => CardPlayEnd());
            await CardPlayEnd();
            game.WaitCard = false;
            GameController();
            log.Information("Leaving HubExtrasDeclared.");
        }

        public async Task AnnounceExtras(bool belotDeclared)
        {
            log.Information("Entering AnnounceExtras.");
            BelotGame game = GetGame();
            List<string> emotes = new List<string>();
            if (belotDeclared)
            {
                //await Task.Run(() => SysAnnounce(game.GetDisplayName(game.Turn) + " called a Belot."));
                await SysAnnounce(game.GetDisplayName(game.Turn) + " called a Belot.");
                emotes.Add("Belot");
            }
            if (game.NumCardsPlayed < 5)
            {
                foreach (Run run in game.Runs[game.Turn])
                {
                    if (run.Declared)
                    {
                        string runName = BelotHelpers.GetRunNameFromLength(run.Length);
                        //await Task.Run(() => SysAnnounce(game.GetDisplayName(game.Turn) + " called a " + runName + "."));
                        await SysAnnounce(game.GetDisplayName(game.Turn) + " called a " + runName + ".");
                        emotes.Add(runName);
                    }
                }
                foreach (Carre carre in game.Carres[game.Turn])
                {
                    if (carre.Declared)
                    {
                        //await Task.Run(() => SysAnnounce(game.GetDisplayName(game.Turn) + " called a Carre."));
                        await SysAnnounce(game.GetDisplayName(game.Turn) + " called a Carre.");
                        emotes.Add("Carre");
                    }
                }
            }
            if (emotes.Count > 0)
            {
                await Clients.Group(GetRoomId()).SendAsync("SetExtrasEmote", emotes, game.Turn);
                await Emote(game.Turn, game.BotDelay);
            }
            log.Information("Leaving AnnounceExtras.");
        }

        public async Task Emote(int seat, int duration)
        {
            log.Information("Entering Emote.");
            await Clients.Group(GetRoomId()).SendAsync("ShowEmote", seat);
            Thread.Sleep(duration);
            await Clients.Group(GetRoomId()).SendAsync("HideEmote", seat);
            log.Information("Leaving Emote.");
        }

        public async Task ThrowCards()
        {
            log.Information("Entering ThrowCards.");
            BelotGame game = GetGame();

            game.Log.Information("Throw: " + game.Turn);

            int points = 10; // stoch

            for (int i = 0; i < 4; i++)
            {
                foreach (var card in game.Hand[i].Where(c => !c.Played))
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

            await Clients.Group(GetRoomId()).SendAsync("ThrowCards", GetDisplayName(game.Turn), game.Hand);
            Thread.Sleep(3500);
            await Clients.Group(GetRoomId()).SendAsync("CloseThrowModal");

            GameController();

            //Thread.Sleep(duration);
            log.Information("Leaving ThrowCards.");
        }

        // -------------------- Points --------------------

        public async Task HubFinalisePoints()
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
            //await Task.Run(() => SysAnnounce(String.Join(" ", message) + "."));
            await SysAnnounce(String.Join(" ", message) + ".");
            log.Information("Leaving HubFinalisePoints.");
        }

        // -------------------- Seat Management --------------------

        public async Task BookSeat(int position) // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot
        {
            log.Information("Entering BookSeat.");
            BelotGame game = GetGame();
            string[] seat = { "West", "North", "East", "South" };

            string requestor = GetCallerUsername();

            if (position == 8) // vacate to Spectator
            {
                if (game.Spectators.Where(s => s.Username == requestor).Count() == 0)
                {
                    //await Task.Run(() => UnbookSeat());
                    await UnbookSeat();
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    //await Task.Run(() => UpdateConnectedUsers());
                    await UpdateConnectedUsers();
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
                //await Task.Run(() => UnbookSeat());
                await UnbookSeat();
                if (game.Spectators.Where(s => s.Username == requestor).Count() == 1) game.Spectators.Remove(game.Spectators.Where(s => s.Username == requestor).First());
                game.Players[position] = new Player(requestor, Context.ConnectionId, true);
                //await Task.Run(() => UpdateConnectedUsers());
                await UpdateConnectedUsers();
                await Clients.OthersInGroup(GetRoomId()).SendAsync("seatBooked", position, requestor, false);
                await Clients.Caller.SendAsync("seatBooked", position, requestor, true);
                await Clients.Caller.SendAsync("enableRotation", true);
                string[] scoreSummary = { "Us", "Them" };
                await Clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(position + 1) % 2], scoreSummary[position % 2]);

                await Clients.OthersInGroup(GetRoomId()).SendAsync("EnableSeatOptions", position, false);
                await Clients.Group(GetRoomId()).SendAsync("EnableOccupySeat", position, false);
                await Clients.OthersInGroup(GetRoomId()).SendAsync("EnableAssignBotToSeat", position, false);
                await Clients.Caller.SendAsync("EnableAssignBotToSeat", position, true);
                await Clients.Caller.SendAsync("EnableVacateSeat", position, true);

                await Clients.Group(GetRoomId()).SendAsync("SetBotBadge", position, false);
                //Clients.Caller.SetRadio(seat[position]);
                //log.Information(requestor + " occupied the " + seat[position] + " seat.");
            }
            else if (occupier == "" && position > 3) // empty seat requested by bot
            {
                position -= 4;
                string botName = GetBotName(position);
                game.Players[position] = new Player(botGUID, "", false);
                //await Task.Run(() => UpdateConnectedUsers());
                await UpdateConnectedUsers();
                await Clients.Group(GetRoomId()).SendAsync("SeatBooked", position, botName, false);
                await Clients.Group(GetRoomId()).SendAsync("SetBotBadge", position, true);
                await Clients.Group(GetRoomId()).SendAsync("EnableAssignBotToSeat", position, false);
                await Clients.Caller.SendAsync("EnableVacateSeat", position, false);
                //log.Information(botName + " occupied the " + seat[position] + " seat.");
            }
            // if bot occupied seat requested by bot -> do nothing
            else if (occupier == requestor && position > 3) // human assigns bot to his own occupied seat
            {
                position -= 4;
                string botName = GetBotName(position);
                //await Task.Run(() => UnbookSeat());
                await UnbookSeat();
                game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                game.Players[position] = new Player(botGUID, "", false);
                //await Task.Run(() => UpdateConnectedUsers());
                await UpdateConnectedUsers();
                await Clients.Group(GetRoomId()).SendAsync("SeatBooked", position, botName, false);
                //Clients.Caller.SetRadio("x");
                await Clients.Group(GetRoomId()).SendAsync("SetBotBadge", position, true);
                await Clients.Group(GetRoomId()).SendAsync("EnableOccupySeat", position, true);
                await Clients.Group(GetRoomId()).SendAsync("EnableAssignBotToSeat", position, false);
                //log.Information(botName + " occupied the " + seat[position] + " seat.");
            }
            // if human tries to occupy his own seat, do nothing
            else if (occupier != "" && occupier != botGUID && occupier != requestor) // human-occupied seat is requested by another human or by a bot on behalf of another human
            {
                await Clients.Caller.SendAsync("SeatAlreadyBooked", occupier);
            }

            if (game.Players.Where(s => s.Username != "").Count() == 4) await Clients.Group(GetRoomId()).SendAsync("EnableNewGame");
            log.Information("Leaving BookSeat.");
        }

        public async Task UnbookSeat()
        {
            log.Information("Entering UnbookSeat.");
            BelotGame game = GetGame();
            string username = GetCallerUsername();

            if (game.Players.Where(s => s.Username == username).Count() == 1)
            {
                int position = Array.IndexOf(game.Players, game.Players.Where(p => p.Username == username).First());
                if (game.IsNewGame)
                {
                    await Clients.Group(GetRoomId()).SendAsync("DisableNewGame");
                    game.Players[position] = new Player();
                }
                else game.Players[position].IsDisconnected = true;
                await Clients.Caller.SendAsync("EnableRotation", false);
                await Clients.Caller.SendAsync("SetScoreTitles", "N /S", "E/W");
                await Clients.Group(GetRoomId()).SendAsync("SeatUnbooked", position);
                await Clients.Group(GetRoomId()).SendAsync("EnableSeatOptions", position, true);
                await Clients.Group(GetRoomId()).SendAsync("EnableOccupySeat", position, true);
                await Clients.Group(GetRoomId()).SendAsync("EnableAssignBotToSeat", position, true);
                await Clients.Group(GetRoomId()).SendAsync("EnableVacateSeat", position, false);
                //string[] seat = { "West", "North", "East", "South" };
                //log.Information(username + " vacated the " + seat[position] + " seat.");
            }
            //await Task.Run(() => UpdateConnectedUsers());
            await UpdateConnectedUsers();
            log.Information("Leaving UnbookSeat.");
        }

        // -------------------- Messaging & Alerts --------------------

        public string MsgHead()
        {
            return GetServerDateTime() + ", " + GetCallerUsername();
        }

        [HubMethodName("announce")] //client-side name for the method may differ from server-side name
        public async void Announce(string message)
        {
            await Clients.Group(GetRoomId()).SendAsync("Announce", MsgHead() + " >> " + message);
            await Clients.Group(GetRoomId()).SendAsync("showChatNotification");
        }

        public async Task SysAnnounce(string message)
        {
            //log.Information(message);
            await Clients.Group(GetRoomId()).SendAsync("Announce", GetServerDateTime() + " >> " + message);
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

        public async Task UpdateConnectedUsers()
        {
            BelotGame game = GetGame();
            string[] playerNames = game.Players.Where(d => d.IsDisconnected == false).Select(s => s.Username).Where(s => s != "").Where(s => s != botGUID).ToArray();
            Array.Sort(playerNames);
            string[] specNames = game.Spectators.Select(s => s.Username).ToArray();
            Array.Sort(specNames);
            await Clients.Group(GetRoomId()).SendAsync("ConnectedUsers", playerNames, specNames);
            //Clients.Caller.ConnectedUsers(playerNames, specNames);
        }

        public async Task LoadContext()
        {
            BelotGame game = GetGame();

            for (int i = 0; i < 4; i++)
            {
                // Update table seats
                if (game.Players[i].IsHuman)
                {
                    await Clients.Caller.SendAsync("EnableOccupySeat", i, false);
                    if (game.Players[i].Username == Context.User.Identity.Name)
                    {
                        await Clients.Caller.SendAsync("SeatBooked", i, game.Players[i].Username, true);
                        await Clients.Caller.SendAsync("EnableVacateSeat", i, true);
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("SeatBooked", i, game.Players[i].Username, false);
                        await Clients.Caller.SendAsync("EnableSeatOptions", i, false);
                        await Clients.Caller.SendAsync("EnableAssignBotToSeat", i, false);
                    }
                }
                else if (game.Players[i].Username == botGUID)
                {
                    string[] seat = ["West", "North", "East", "South"];
                    await Clients.Caller.SendAsync("SeatBooked", i, GetBotName(i), false);
                    await Clients.Caller.SendAsync("SetBotBadge", i, true);
                    await Clients.Caller.SendAsync("EnableAssignBotToSeat", i, false);
                }

                // Update table cards
                if (!game.IsNewGame)
                {
                    Clients.Caller.SendAsync("SetTableCard", i, game.TableCards[i]);
                }
            }

            if (!game.IsNewGame)
            {
                await Clients.Caller.SendAsync("HideDeck", true);

                int dealer = game.FirstPlayer + 1;
                if (dealer == 4) dealer = 0;

                await Clients.Caller.SendAsync("SetDealerMarker", dealer);
                await Clients.Caller.SendAsync("SetTurnIndicator", game.Turn);
                await Clients.Caller.SendAsync("DisableRadios");
                await Clients.Caller.SendAsync("UpdateScoreTotals", game.EWTotal, game.NSTotal);

                for (int i = 0; i < game.ScoreHistory.Count; i++)
                {
                    await Clients.Caller.SendAsync("AppendScoreHistory", game.ScoreHistory[i][0], game.ScoreHistory[i][1]);
                }

                await Clients.Caller.SendAsync("SuitNominated", game.RoundCall);
                if (game.Multiplier == 2)
                {
                    await Clients.Caller.SendAsync("SuitNominated", 7);
                }
                else if (game.Multiplier == 4)
                {
                    await Clients.Caller.SendAsync("SuitNominated", 8);
                }

                if (game.RoundCall > Call.Pass)
                {
                    await Clients.Caller.SendAsync("SetCallerIndicator", game.Caller);
                }

                // if the connecting user is a player
                if (game.Players.Any(u => u.Username == GetCallerUsername()))
                {
                    int pos = Array.IndexOf(game.Players, game.Players.FirstOrDefault(p => p.Username == GetCallerUsername()));

                    await Clients.Caller.SendAsync("EnableRotation", true);
                    string[] scoreSummary = ["Us", "Them"];
                    await Clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(pos + 1) % 2], scoreSummary[pos % 2]);

                    await Clients.Caller.SendAsync("Deal", game.Hand[pos]);

                    for (int i = 0; i < 8; i++)
                    {
                        if (i >= game.Hand[pos].Count || game.Hand[pos][i].Played) // resyncing during call phase when hand size is 5
                        {
                            await Clients.Caller.SendAsync("HideCard", "card" + i);
                        }
                    }
                    await Clients.Caller.SendAsync("RotateCards");

                    if (game.Turn == pos)
                    {
                        // deal
                        if (dealer == pos && game.Hand[pos].Count == 0)
                        {
                            await Clients.Caller.SendAsync("EnableDealBtn");
                        }
                        // if the game is in the suit-calling phase
                        else if (game.Hand[pos].Count == 5)
                        {
                            int[] validCalls = game.ValidCalls();
                            bool fiveUnderNine = game.Calls.Count < 4 && BelotHelpers.FiveUnderNine(game.Hand[game.Turn]);
                            await Clients.Caller.SendAsync("ShowSuitModal", validCalls, fiveUnderNine);
                        }
                        // if the connecting user must declare extras
                        else if (!game.TableCards[game.Turn].IsNull())
                        {
                            await Clients.Caller.SendAsync("DeclareExtras", game.CurrentExtras);
                        }
                        // if the game is in the card-playing phase
                        else if (game.Hand[pos].Count == 8)
                        {
                            await Clients.Caller.SendAsync("EnableCards", game.ValidCards());
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
            else if (game.Players.Where(s => s.Username != "").Count() == 4)
            {
                await Clients.Caller.SendAsync("EnableNewGame");
            }
        }

        public override async Task OnConnectedAsync()
        {
            log.Information("Entering OnConnected.");

            var roomId = Context.GetHttpContext().GetRouteValue("roomId") as string;

            //await Clients.Caller.SendAsync("SetRoomId", roomId);

            allConnections.Add(Context.ConnectionId, roomId);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            BelotGame game = GetGame();

            //await Clients.Caller.SendAsync("SetGameId", game.GameId);

            string username = GetCallerUsername();
            IEnumerable<Player> players = game.Players.Where(p => p.Username == username);
            if (!players.Any())
            {
                game.Spectators.Add(new Spectator(username, Context.ConnectionId));
            }
            else
            {
                players.First().ConnectionId = Context.ConnectionId;
                players.First().IsDisconnected = false;
                int pos = Array.IndexOf(game.Players, players.First());
                await Clients.OthersInGroup(GetRoomId()).SendAsync("SeatBooked", pos, username, false);
            }
            //await Task.Run(() => UpdateConnectedUsers());
            await UpdateConnectedUsers();

            //await Task.Run(() => SysAnnounce(username + " connected."));
            await SysAnnounce(username + " connected.");
            log.Information(username + " Connected.");

            //await Task.Run(() => LoadContext());
            await LoadContext();
            log.Information("Leaving OnConnected.");
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception ex)
        {
            log.Information("Entering OnDisconnected.");

            BelotGame game = GetGame();

            string username = GetCallerUsername();
            if (game.Spectators.Where(p => p.Username == username).Count() == 1) game.Spectators.Remove(game.Spectators.Where(s => s.Username == username).First());
            //await Task.Run(() => UnbookSeat());
            await UnbookSeat();
            //await Task.Run(() => SysAnnounce(username + " disconnected."));
            await SysAnnounce(username + " disconnected.");
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
            if (games.Count == 0)
            {
                log.Dispose();
                log = null;
            }
            await base.OnDisconnectedAsync(ex);
        }
    }
}