using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Players;
using Microsoft.AspNetCore.SignalR;
using Serilog;

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
            //log?.Information("Creating new Chat Room");
        }

        // -------------------- Main --------------------

        public async Task HubGameController() // called by client
        {
            BelotGame game = GetGame();
            GameController(game, Clients);
        }

        public async Task GameController(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering GameController.");

            var group = clients.Group(game.RoomId);
            if (game.IsNewGame)
            {
                game.IsNewGame = false;
                await group.SendAsync("HideDeck", true);
                await group.SendAsync("DisableNewGame");
                await group.SendAsync("CloseModalsAndButtons");
                await group.SendAsync("DisableRadios");
                game.NewGame();
                await group.SendAsync("NewGame", game.GameId); // reset score table (offcanvas), reset score totals (card table), hide winner markers, set game id
            }
            while (game.IsRunning && ((game.EWTotal < scoreTarget && game.NSTotal < scoreTarget) || game.EWTotal == game.NSTotal || game.Capot) && !game.WaitDeal && !game.WaitCall & !game.WaitCard)
            {
                await RoundController(game, clients);
            }

            if (!game.WaitDeal && !game.WaitCall & !game.WaitCard)
            {
                game.RecordGameEnd();
                await EndGame(game, clients);
            }
            log?.Information("Leaving GameController.");
        }

        public async Task RoundController(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering RoundController.");

            var group = clients.Group(game.RoomId);

            if (game.IsNewRound)
            {
                game.IsNewRound = false;
                game.NewRound();
                await group.SendAsync("SetTurnIndicator", game.Turn); // show dealer
                await group.SendAsync("SetTurnIndicator", game.Turn); // show dealer
                await group.SendAsync("SetDealerMarker", game.Turn);
                await group.SendAsync("NewRound"); // reset table, reset board, disable cards, reset suit selection 
                if (game.Players[game.Turn].IsHuman)
                {
                    game.WaitDeal = true;
                    await clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("EnableDealBtn");
                    return;
                }
                else
                {
                    await Task.Delay(game.BotDelay);
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
                        await clients.Client(game.Players[i].ConnectionId).SendAsync("Deal", game.Hand[i]);
                        await clients.Client(game.Players[i].ConnectionId).SendAsync("RotateCards");
                    }
                }
            }

            if (game.NumCardsPlayed == 0)
            {
                while (game.IsRunning && !game.SuitDecided() && !game.WaitCall)
                {
                    await group.SendAsync("SetTurnIndicator", game.Turn);
                    await CallController(game, clients);
                }
            }

            if (game.RoundCall == Call.Pass && !game.WaitCall)
            {
                await SysAnnounce("No suit chosen.", game, clients);
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
                    await SysAnnounce("The round will be played in " + BelotHelpers.GetSuitNameFromNumber(game.RoundCall) + ".", game, clients);
                    game.Turn = game.FirstPlayer;
                    await group.SendAsync("SetTurnIndicator", game.Turn);
                    game.Deal(3);
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.Players[i].IsHuman)
                        {
                            await clients.Client(game.Players[i].ConnectionId).SendAsync("Deal", game.Hand[i]);
                            await clients.Client(game.Players[i].ConnectionId).SendAsync("RotateCards");
                        }
                    }
                    if (game.RoundCall != Call.NoTrumps)
                    {
                        game.FindCarres();
                        game.FindRuns();
                        game.FindBelots();
                    }
                }
                while (game.IsRunning && game.NumCardsPlayed < 32 && !game.WaitCard)
                {
                    await group.SendAsync("SetTurnIndicator", game.Turn);
                    await TrickController(game, clients);
                }
                if (game.NumCardsPlayed == 32)
                {
                    await group.SendAsync("NewRound");
                    await SysAnnounce(game.FinalisePoints(), game, clients);
                    game.ScoreHistory.Add([game.EWRoundPoints, game.NSRoundPoints]);
                    await group.SendAsync("AppendScoreHistory", game.EWRoundPoints, game.NSRoundPoints);
                    await group.SendAsync("UpdateScoreTotals", game.EWTotal, game.NSTotal);
                    await group.SendAsync("ShowRoundSummary", game.TrickPoints, game.DeclarationPoints, game.BelotPoints, game.Result, game.EWRoundPoints, game.NSRoundPoints);
                    await Task.Delay(game.RoundSummaryDelay);
                    await group.SendAsync("HideRoundSummary");
                    game.IsNewRound = true;
                    game.RecordTrickEnd();
                }
            }
            log?.Information("Leaving RoundController.");
        }

        public async Task CallController(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering CallController.");

            int[] validCalls = game.ValidCalls();
            if (validCalls.Sum() == 0)
            {
                game.NominateSuit(0); // auto-pass
                await AnnounceSuit(game, clients);
                if (--game.Turn == -1) game.Turn = 3;
            }
            else if (game.Players[game.Turn].IsHuman)
            {
                bool fiveUnderNine = game.Calls.Count < 4 && BelotHelpers.FiveUnderNine(game.Hand[game.Turn]);
                await clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("ShowSuitModal", validCalls, fiveUnderNine);
                game.WaitCall = true;
            }
            else // bot
            {
                game.NominateSuit(AgentBasic.CallSuit(game.Hand[game.Turn], validCalls));
                await AnnounceSuit(game, clients);
                if (--game.Turn == -1) game.Turn = 3;
            }
            log?.Information("Leaving CallController.");
        }

        public async Task TrickController(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering TrickController.");

            var group = clients.Group(game.RoomId);

            while (game.TableCards.Count(c => !c.IsNull()) < 4 && !game.WaitCard)
            {
                if (game.Hand[game.Turn].Count(c => !c.Played) == 1) // auto-play last card
                {
                    if (game.Players[game.Turn].IsHuman)
                    {
                        await clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("PlayFinalCard");
                    }
                    game.PlayCard(game.Hand[game.Turn].FirstOrDefault(c => !c.Played)); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                    game.RecordCardPlayed([]);
                    await CardPlayEnd(game, clients);
                    continue;
                }
                int[] validCards = game.ValidCards();
                if (game.Players[game.Turn].IsHuman)
                {
                    game.WaitCard = true;
                    if (game.TableCards.All(c => c.IsNull()))
                    {
                        if (game.GetWinners(game.Turn).Count(w => w == 2) == game.Hand[game.Turn].Count(c => !c.Played) && game.NumCardsPlayed > 3)
                        {
                            await clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("ShowThrowBtn");
                        }
                    }
                    await clients.Client(game.Players[game.Turn].ConnectionId).SendAsync("EnableCards", validCards);
                    // once a card is clicked, declarable extras are calculated in hub method, human selects and declares extras, then the card is played and game loop reinitiates
                }
                else
                {
                    var card = AgentBasic.SelectCard(game.Hand[game.Turn], validCards, game.GetWinners(game.Turn), game.TableCards, game.Turn, game.DetermineWinner(), game.RoundCall, game.TrickSuit, game.EWCalled);

                    game.PlayCard(card);

                    List<string> emotes = [];

                    if (game.RoundCall != Call.NoTrumps)
                    {
                        List<Declaration> declaredDeclarations = [];
                        foreach (var declaration in game.Declarations.Where(d => d.Player == game.Turn && (d is not Run run || run.IsValid) && d.IsDeclarable))
                        {
                            declaration.Declared = true;
                            declaredDeclarations.Add(declaration);
                        }

                        emotes = await AnnounceExtras(declaredDeclarations, game, clients);
                    }

                    game.RecordCardPlayed(emotes);
                    await CardPlayEnd(game, clients);
                }
            }
            if (!game.WaitCard) // trick end
            {
                int winner = game.DetermineWinner();
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

                await group.SendAsync("ShowTrickWinner", winner);
                await Task.Delay(1000);

                if (game.NumCardsPlayed < 32)
                {
                    await group.SendAsync("ResetTable");
                    game.Turn = winner;
                    game.RecordTrickEnd();
                    game.TableCards = [new(), new(), new(), new()];
                }
                game.HighestTrumpInTrick = 0;
                game.TrickSuit = null;
            }
            log?.Information("Leaving TrickController.");
        }

        // -------------------- Reset --------------------

        public async Task EndGame(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering EndGame.");

            var group = clients.Group(game.RoomId);

            string winner = game.EWTotal > game.NSTotal ? "E/W" : "N/S";

            await SysAnnounce(winner + " win the game: " + game.EWTotal + " to " + game.NSTotal + ".", game, clients);

            await group.SendAsync("SetDealerMarker", 4);
            await group.SendAsync("NewRound");
            await group.SendAsync("SetTurnIndicator", 4);
            // animation and modal to indicate winning team?
            if (game.EWTotal > game.NSTotal)
            {
                await group.SendAsync("ShowGameWinner", 0);
                await group.SendAsync("ShowGameWinner", 2);
            }
            else
            {
                await group.SendAsync("ShowGameWinner", 1);
                await group.SendAsync("ShowGameWinner", 3);
            }
            await group.SendAsync("EnableNewGame");
            await group.SendAsync("EnableRadios");
            game.IsNewGame = true;
            game.CloseLog();
            log?.Information("Leaving EndGame.");
        }

        // -------------------- Setup --------------------

        public void HubShuffle() // called by client
        {
            log?.Information("Entering HubShuffle.");

            BelotGame game = GetGame();
            var clients = Clients;

            game.WaitDeal = false;
            GameController(game, clients);

            log?.Information("Leaving HubShuffle.");
        }

        // -------------------- Suit Nomination --------------------

        public async Task HubNominateSuit(Call call) // called by client
        {
            log?.Information("Entering HubNominateSuit.");
            BelotGame game = GetGame();
            var clients = Clients;

            game.NominateSuit(call);

            // this method is only called by human interaction with the html elements
            game.WaitCall = false;
            await AnnounceSuit(game, clients);
            if (--game.Turn == -1) game.Turn = 3;
            GameController(game, clients);
            log?.Information("Leaving HubNominateSuit.");
        }

        public async Task AnnounceSuit(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering AnnounceSuit.");

            var group = clients.Group(game.RoomId);

            string username = GetDisplayName(game);

            string message = username;

            Call call = game.Calls[^1];

            if (call == Call.Pass)
            {
                message += " passed.";
            }
            else
            {
                await group.SendAsync("SuitNominated", call);
                await group.SendAsync("setCallerIndicator", game.Turn);

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

            await SysAnnounce(message, game, clients);
            await group.SendAsync("EmoteSuit", call, game.Turn);
            await Emote(game.Turn, game.BotDelay, game, clients);
            log?.Information("Leaving AnnounceSuit.");
        }

        // -------------------- Card Validation --------------------

        // -------------------- Gameplay --------------------

        public async Task HubPlayCard(Card card) // called by client
        {
            log?.Information("Entering HubPlayCard.");

            BelotGame game = GetGame();
            var clients = Clients;

            game.PlayCard(card);
            await clients.Caller.SendAsync("SetTableCard", game.Turn, game.TableCards[game.Turn]);

            await clients.Caller.SendAsync("DeclareExtras", game.Declarations.Where(d => d.Player == game.Turn && d.IsDeclarable));

            log?.Information("Leaving HubPlayCard.");
        }

        public async Task CardPlayEnd(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering CardPlayEnd.");

            var group = clients.Group(game.RoomId);

            await group.SendAsync("SetTableCard", game.Turn, game.TableCards[game.Turn]);
            await Task.Delay(game.BotDelay);
            if (game.NumCardsPlayed % 4 != 0 && --game.Turn == -1)
            {
                game.Turn = 3;
            }
            if (game.NumCardsPlayed < 32)
            {
                group.SendAsync("SetTurnIndicator", game.Turn);
            }
            log?.Information("Leaving CardPlayEnd.");
        }

        public async Task HubExtrasDeclared(List<Declaration> declarations) // called by client
        {
            log?.Information("Entering HubExtrasDeclared.");

            BelotGame game = GetGame();
            var clients = Clients;

            var validDeclarations = declarations.Where(d => d != null);

            List<string> emotes = [];

            if (game.RoundCall != Call.NoTrumps)
            {
                var declaredDeclarations = game.DeclareDeclarations(validDeclarations);
                emotes = await AnnounceExtras(declaredDeclarations, game, clients);
            }

            game.RecordCardPlayed(emotes);
            await CardPlayEnd(game, clients);
            game.WaitCard = false;
            GameController(game, clients);

            log?.Information("Leaving HubExtrasDeclared.");
        }

        public async Task<List<string>> AnnounceExtras(List<Declaration> declarations, BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering AnnounceExtras.");

            List<string> emotes = [];

            foreach (var declaration in declarations.OfType<Belot>())
            {
                await SysAnnounce(game.GetDisplayName(game.Turn) + " called a Belot.", game, clients);
                emotes.Add("Belot");
            }
            foreach (var declaration in declarations.OfType<Carre>())
            {
                await SysAnnounce(game.GetDisplayName(game.Turn) + " called a Carre.", game, clients);
                emotes.Add("Carre");
            }
            foreach (var declaration in declarations.OfType<Run>())
            {
                string runName = BelotHelpers.GetRunNameFromLength(declaration.Length);
                await SysAnnounce(game.GetDisplayName(game.Turn) + " called a " + runName + ".", game, clients);
                emotes.Add(runName);
            }

            if (emotes.Count > 0)
            {
                emotes = emotes.OrderBy(i =>
                {
                    int index = BelotHelpers.declarationDisplayOrder.IndexOf(i);
                    return index == -1 ? int.MaxValue : BelotHelpers.declarationDisplayOrder.IndexOf(i);
                }).ToList();

                await clients.Group(game.RoomId).SendAsync("SetExtrasEmote", emotes, game.Turn);
                await Emote(game.Turn, game.BotDelay, game, clients);
            }

            log?.Information("Leaving AnnounceExtras.");

            return emotes;
        }

        public async Task Emote(int seat, int duration, BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering Emote.");

            var group = clients.Group(game.RoomId);

            await group.SendAsync("ShowEmote", seat);
            await Task.Delay(duration);
            await group.SendAsync("HideEmote", seat);

            log?.Information("Leaving Emote.");
        }

        public async Task HubThrowCards() // called by client
        {
            log?.Information("Entering ThrowCards.");

            BelotGame game = GetGame();
            var clients = Clients;
            var group = clients.Group(game.RoomId);

            int points = 10; // stoch

            for (int i = 0; i < 4; i++)
            {
                foreach (var card in game.Hand[i].Where(c => !c.Played))
                {
                    points += game.CalculateCardPoints(card);
                }
            }

            if (game.Turn % 2 == 0)
            {
                game.EWRoundPoints += points;
            }
            else
            {
                game.NSRoundPoints += points;
            }

            foreach (var belot in game.Declarations.OfType<Belot>().Where(b => b.Unplayed()))
            {
                belot.Declared = true;
            }

            game.NumCardsPlayed = 32;
            game.WaitCard = false;

            await group.SendAsync("ThrowCards", GetDisplayName(game), game.Hand);
            await Task.Delay(5000);
            await group.SendAsync("CloseThrowModal");

            GameController(game, clients);

            log?.Information("Leaving ThrowCards.");
        }

        #region Seat Management

        public async Task HubBookSeat(int position) // called by client // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot, 8 = vacate
        {
            log?.Information("Entering BookSeat.");

            BelotGame game = GetGame();
            var clients = Clients;

            var roomId = game.RoomId;
            var group = clients.Group(roomId);

            string[] seat = ["West", "North", "East", "South"];

            string requestor = GetCallerUsername();

            if (position != 8)
            {
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
                    await UnbookSeat(game, clients);
                    var spectator = game.Spectators.FirstOrDefault(s => s.Username == requestor);
                    if (spectator != null)
                    {
                        game.Spectators.Remove(spectator);
                    }
                    game.Players[position] = new Player(requestor, Context.ConnectionId, true);
                    await UpdateConnectedUsers(game, clients);
                    await clients.OthersInGroup(roomId).SendAsync("seatBooked", position, requestor, false);
                    await clients.Caller.SendAsync("seatBooked", position, requestor, true);
                    await clients.Caller.SendAsync("enableRotation", true);
                    string[] scoreSummary = { "Us", "Them" };
                    await clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(position + 1) % 2], scoreSummary[position % 2]);

                    await clients.OthersInGroup(roomId).SendAsync("EnableSeatOptions", position, false);
                    await group.SendAsync("EnableOccupySeat", position, false);
                    await clients.OthersInGroup(roomId).SendAsync("EnableAssignBotToSeat", position, false);
                    await clients.Caller.SendAsync("EnableAssignBotToSeat", position, true);
                    await clients.Caller.SendAsync("EnableVacateSeat", position, true);

                    await group.SendAsync("SetBotBadge", position, false);
                }
                else if (occupier == "" && position > 3) // empty seat requested by bot
                {
                    position -= 4;
                    string botName = GetBotName(position);
                    game.Players[position] = new Player(botGUID, "", false);
                    await UpdateConnectedUsers(game, clients);
                    await group.SendAsync("SeatBooked", position, botName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                    await group.SendAsync("EnableAssignBotToSeat", position, false);
                    await clients.Caller.SendAsync("EnableVacateSeat", position, false);
                }
                // if bot occupied seat requested by bot -> do nothing
                else if (occupier == requestor && position > 3) // human assigns bot to his own occupied seat
                {
                    position -= 4;
                    string botName = GetBotName(position);
                    await UnbookSeat(game, clients);
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    game.Players[position] = new Player(botGUID, "", false);
                    await UpdateConnectedUsers(game, clients);
                    await group.SendAsync("SeatBooked", position, botName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                    await group.SendAsync("EnableOccupySeat", position, true);
                    await group.SendAsync("EnableAssignBotToSeat", position, false);
                }
                // if human tries to occupy his own seat, do nothing
                else if (occupier != "" && occupier != botGUID && occupier != requestor) // human-occupied seat is requested by another human or by a bot on behalf of another human
                {
                    await clients.Caller.SendAsync("SeatAlreadyBooked", occupier);
                }

                if (game.Players.Where(s => s.Username != "").Count() == 4)
                {
                    await group.SendAsync("EnableNewGame");
                }
            }
            else // vacate and become spectator
            {
                if (!game.Spectators.Any(s => s.Username == requestor))
                {
                    await UnbookSeat(game, clients);
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    await UpdateConnectedUsers(game, clients);
                }
            }

            log?.Information("Leaving BookSeat.");
        }

        public async Task UnbookSeat(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("Entering UnbookSeat.");
            var group = clients.Group(game.RoomId);
            string username = GetCallerUsername();

            var player = game.Players.FirstOrDefault(s => s.Username == username);
            if (player != null)
            {
                int position = Array.IndexOf(game.Players, player);
                if (game.IsNewGame)
                {
                    await group.SendAsync("DisableNewGame");
                    game.Players[position] = new();
                }
                else
                {
                    player.IsDisconnected = true;
                }
                await clients.Caller.SendAsync("EnableRotation", false);
                await clients.Caller.SendAsync("SetScoreTitles", "N /S", "E/W");
                await group.SendAsync("SeatUnbooked", position);
                await group.SendAsync("EnableSeatOptions", position, true);
                await group.SendAsync("EnableOccupySeat", position, true);
                await group.SendAsync("EnableAssignBotToSeat", position, true);
                await group.SendAsync("EnableVacateSeat", position, false);
            }

            await UpdateConnectedUsers(game, clients);
            log?.Information("Leaving UnbookSeat.");
        }

        #endregion

        // -------------------- Messaging & Alerts --------------------

        public string MsgHead()
        {
            return GetServerDateTime() + ", " + GetCallerUsername();
        }

        public async void HubAnnounce(string message) // called by client
        {
            BelotGame game = GetGame();
            var group = Clients.Group(game.RoomId);
            await group.SendAsync("Announce", MsgHead() + " >> " + message);
            await group.SendAsync("showChatNotification");
        }

        public async Task SysAnnounce(string message, BelotGame game, IHubCallerClients clients)
        {
            await clients.Group(game.RoomId).SendAsync("Announce", GetServerDateTime() + " >> " + message);
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

        public string GetDisplayName(BelotGame game)
        {
            if (game.Players[game.Turn].IsHuman)
            {
                return game.Players[game.Turn].Username;
            }
            else
            {
                return GetBotName(game.Turn);
            }
        }

        public BelotGame GetGame()
        {
            allConnections.TryGetValue(Context.ConnectionId, out string roomId);
            return games.FirstOrDefault(i => i.RoomId == roomId);
        }

        // -------------------- Connection --------------------

        public async Task UpdateConnectedUsers(BelotGame game, IHubCallerClients clients)
        {
            string[] playerNames = game.Players.Where(d => !d.IsDisconnected && d.Username != "" && d.Username != botGUID).Select(s => s.Username).ToArray();
            Array.Sort(playerNames);
            string[] specNames = game.Spectators.Select(s => s.Username).ToArray();
            Array.Sort(specNames);
            await clients.Group(game.RoomId).SendAsync("ConnectedUsers", playerNames, specNames);
        }

        public async Task LoadContext(BelotGame game, IHubCallerClients clients)
        {
            for (int i = 0; i < 4; i++)
            {
                // Update table seats
                if (game.Players[i].IsHuman)
                {
                    await clients.Caller.SendAsync("EnableOccupySeat", i, false);
                    if (game.Players[i].Username == Context.User.Identity.Name)
                    {
                        await clients.Caller.SendAsync("SeatBooked", i, game.Players[i].Username, true);
                        await clients.Caller.SendAsync("EnableVacateSeat", i, true);
                    }
                    else
                    {
                        await clients.Caller.SendAsync("SeatBooked", i, game.Players[i].Username, false);
                        await clients.Caller.SendAsync("EnableSeatOptions", i, false);
                        await clients.Caller.SendAsync("EnableAssignBotToSeat", i, false);
                    }
                }
                else if (game.Players[i].Username == botGUID)
                {
                    string[] seat = ["West", "North", "East", "South"];
                    await clients.Caller.SendAsync("SeatBooked", i, GetBotName(i), false);
                    await clients.Caller.SendAsync("SetBotBadge", i, true);
                    await clients.Caller.SendAsync("EnableAssignBotToSeat", i, false);
                }

                // Update table cards
                if (!game.IsNewGame)
                {
                    clients.Caller.SendAsync("SetTableCard", i, game.TableCards[i]);
                }
            }

            if (!game.IsNewGame)
            {
                await clients.Caller.SendAsync("HideDeck", true);

                int dealer = game.FirstPlayer + 1;
                if (dealer == 4) dealer = 0;

                await clients.Caller.SendAsync("SetDealerMarker", dealer);
                await clients.Caller.SendAsync("SetTurnIndicator", game.Turn);
                await clients.Caller.SendAsync("DisableRadios");
                await clients.Caller.SendAsync("UpdateScoreTotals", game.EWTotal, game.NSTotal);

                for (int i = 0; i < game.ScoreHistory.Count; i++)
                {
                    await clients.Caller.SendAsync("AppendScoreHistory", game.ScoreHistory[i][0], game.ScoreHistory[i][1]);
                }

                await clients.Caller.SendAsync("SuitNominated", game.RoundCall);
                if (game.Multiplier == 2)
                {
                    await clients.Caller.SendAsync("SuitNominated", 7);
                }
                else if (game.Multiplier == 4)
                {
                    await clients.Caller.SendAsync("SuitNominated", 8);
                }

                if (game.RoundCall > Call.Pass)
                {
                    await clients.Caller.SendAsync("SetCallerIndicator", game.Caller);
                }

                // if the connecting user is a player
                if (game.Players.Any(u => u.Username == GetCallerUsername()))
                {
                    int pos = Array.IndexOf(game.Players, game.Players.FirstOrDefault(p => p.Username == GetCallerUsername()));

                    await clients.Caller.SendAsync("EnableRotation", true);
                    string[] scoreSummary = ["Us", "Them"];
                    await clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(pos + 1) % 2], scoreSummary[pos % 2]);

                    await clients.Caller.SendAsync("Deal", game.Hand[pos]);

                    for (int i = 0; i < 8; i++)
                    {
                        if (i >= game.Hand[pos].Count || game.Hand[pos][i].Played) // resyncing during call phase when hand size is 5
                        {
                            await clients.Caller.SendAsync("HideCard", "card" + i);
                        }
                    }
                    await clients.Caller.SendAsync("RotateCards");

                    if (game.Turn == pos)
                    {
                        // deal
                        if (dealer == pos && game.Hand[pos].Count == 0)
                        {
                            await clients.Caller.SendAsync("EnableDealBtn");
                        }
                        // if the game is in the suit-calling phase
                        else if (game.Hand[pos].Count == 5)
                        {
                            int[] validCalls = game.ValidCalls();
                            bool fiveUnderNine = game.Calls.Count < 4 && BelotHelpers.FiveUnderNine(game.Hand[game.Turn]);
                            await clients.Caller.SendAsync("ShowSuitModal", validCalls, fiveUnderNine);
                        }
                        // if the connecting user must declare extras
                        else if (!game.TableCards[game.Turn].IsNull())
                        {
                            await clients.Caller.SendAsync("DeclareExtras", game.Declarations.Where(d => d.Player == game.Turn && d.IsDeclarable));
                        }
                        // if the game is in the card-playing phase
                        else if (game.Hand[pos].Count == 8)
                        {
                            await clients.Caller.SendAsync("EnableCards", game.ValidCards());
                        }
                    }
                }
            }
            else if (game.Players.Where(s => s.Username != "").Count() == 4)
            {
                await clients.Caller.SendAsync("EnableNewGame");
            }
        }

        public override async Task OnConnectedAsync()
        {
            log?.Information("Entering OnConnected.");

            var roomId = Context.GetHttpContext().GetRouteValue("roomId") as string;

            allConnections.Add(Context.ConnectionId, roomId);

            BelotGame game = GetGame();
            var clients = Clients;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            string username = GetCallerUsername();
            var player = game.Players?.FirstOrDefault(p => p.Username == username);
            if (player == null)
            {
                game.Spectators.Add(new Spectator(username, Context.ConnectionId));
            }
            else
            {
                player.ConnectionId = Context.ConnectionId;
                player.IsDisconnected = false;
                int pos = Array.IndexOf(game.Players, player);
                await clients.OthersInGroup(game.RoomId).SendAsync("SeatBooked", pos, username, false);
            }
            await UpdateConnectedUsers(game, clients);

            await SysAnnounce(username + " connected.", game, clients);
            log?.Information(username + " Connected.");

            await LoadContext(game, clients);

            log?.Information("Leaving OnConnected.");
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? ex)
        {
            log?.Information("Entering OnDisconnected.");

            BelotGame game = GetGame();
            var clients = Clients;

            string username = GetCallerUsername();
            bool playerReallyDisconnected = false; // guards against race condition of a reconnect updating ConnectionId but not setting player.IsDisconnected = false before continuing here

            var spectator = game.Spectators.FirstOrDefault(p => p.Username == username);
            if (spectator != null)
            {
                game.Spectators.Remove(spectator);
                playerReallyDisconnected = true;
            }
            else
            {
                await Task.Delay(500); // allow for possible reconnect
                var player = game.Players.FirstOrDefault(p => p.Username == username);
                if (player != null && player.ConnectionId == Context.ConnectionId) // player has not reconnected, connectionId is stale
                {
                    await UnbookSeat(game, clients); // this will mark them as disconnected
                    await SysAnnounce(username + " disconnected.", game, clients);
                    log?.Information($"{username} marked as disconnected after delay.");
                    playerReallyDisconnected = true;
                }
                else // player reconnected, don't proceed with disconnection
                {
                    log?.Information($"{username} reconnected before disconnect cleanup.");
                }

            }

            if (playerReallyDisconnected && game.Spectators.Count + game.Players.Count(p => p.IsHuman && !p.IsDisconnected) == 0)
            {

                int oldwinnerDelay = game.WinnerDelay;
                int oldBotDelay = game.BotDelay;
                int oldRoundSummaryDelay = game.RoundSummaryDelay;
                game.WinnerDelay = 0;
                game.BotDelay = 0;
                game.RoundSummaryDelay = 0;
                await Task.Delay(1500);
                game.WinnerDelay = oldwinnerDelay;
                game.BotDelay = oldBotDelay;
                game.RoundSummaryDelay = oldRoundSummaryDelay;

                if (game.Spectators.Count + game.Players.Count(p => p.IsHuman && !p.IsDisconnected) == 0)
                {
                    games.Remove(game);
                    game.IsRunning = false;
                    game.CloseLog();
                }
            }

            allConnections.Remove(Context.ConnectionId);
            log?.Information("Leaving OnDisconnected.");
            if (games.Count == 0)
            {
                log?.Dispose();
                log = null;
            }

            await base.OnDisconnectedAsync(ex);
        }
    }
}