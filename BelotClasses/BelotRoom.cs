using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Players;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoom : Hub
    {
        private readonly BelotGameRegistry _gameRegistry;

        public static Dictionary<string, string> allConnections = [];

        public static readonly string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";

        public static Serilog.Core.Logger log;// = new LoggerConfiguration().WriteTo.File(ConfigurationManager.AppSettings["logfilepath"] + "BelotServerLog-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        #region Game Loop Continuation

        public BelotRoom(IConfiguration config, BelotGameRegistry gameRegistry)
        {
            _gameRegistry = gameRegistry;
            log ??= new LoggerConfiguration().WriteTo.File(config.GetSection("SerilogPath:Path").Value + "BelotServerLog-.txt", rollingInterval: RollingInterval.Day).CreateLogger();
            //log?.Information("Creating new Chat Room");
        }

        public Task HubGameController() // called by client
        {
            log?.Information("[HubGameController] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubGameController] GameContext/Game/Observer was null");
                return Task.CompletedTask;
            }

            BelotGameEngine engine = new(gameContext.Game, gameContext.Observer);

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error($"[HubGameController] Unhandled exception: {ex}");
                }
            });

            log?.Information("[HubGameController] exit");

            return Task.CompletedTask;
        }

        public Task HubShuffle() // called by client
        {
            log?.Information("[HubShuffle] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubShuffle] GameContext/Game/Observer was null");
                return Task.CompletedTask;
            }

            BelotGameEngine engine = new(gameContext.Game, gameContext.Observer);

            gameContext.Game.WaitDeal = false;

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error($"[HubShuffle] Unhandled exception: {ex}");
                }
            });

            log?.Information("[HubShuffle] exit");

            return Task.CompletedTask;
        }

        public async Task HubNominateSuit(Call call) // called by client
        {
            log?.Information("[HubNominateSuit] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubNominateSuit] GameContext/Game/Observer was null");
                return;
            }

            BelotGameEngine engine = new(gameContext.Game, gameContext.Observer);

            gameContext.Game.NominateSuit(call);
            gameContext.Game.WaitCall = false;
            if (gameContext.Observer is LiveBelotObserver live)
            {
                await live.AnnounceSuit();
            }
            if (--gameContext.Game.Turn == -1) gameContext.Game.Turn = 3;

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error($"[HubNominateSuit] Unhandled exception: {ex}");
                }
            });

            log?.Information("[HubNominateSuit] exit");

            return;
        }

        public async Task HubPlayCard(Card card) // called by client
        {
            log?.Information("[HubPlayCard] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubPlayCard] GameContext/Game/Observer was null");
                return;
            }

            var game = gameContext.Game;
            var clients = Clients;

            BelotGameEngine engine = new(game, gameContext.Observer);

            gameContext.Game.PlayCard(card);
            await clients.Caller.SendAsync("SetTableCard", game.Turn, game.TableCards[game.Turn]);
            await clients.Caller.SendAsync("DeclareExtras", game.Declarations.Where(d => d.Player == game.Turn && d.IsDeclarable));

            log?.Information("[HubPlayCard] exit");
        }

        public async Task HubExtrasDeclared(List<Declaration> declarations) // called by client
        {
            log?.Information("[HubExtrasDeclared] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubExtrasDeclared] GameContext/Game/Observer was null");
                return;
            }

            var game = gameContext.Game;

            BelotGameEngine engine = new(game, gameContext.Observer);

            var validDeclarations = declarations?.Where(d => d != null) ?? [];

            List<string> messages = [];
            List<string> emotes = [];

            if (game.RoundCall != Call.NoTrumps)
            {
                var declaredDeclarations = game.DeclareDeclarations(validDeclarations);
                (messages, emotes) = BelotHelpers.GetDeclarationMessagesAndEmotes(declaredDeclarations, game);

                if (gameContext.Observer is LiveBelotObserver live)
                {
                    await live.OnDeclaration(messages, emotes);
                }
            }

            game.RecordCardPlayed(emotes);
            await engine.CardPlayEnd();
            game.WaitCard = false;

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error($"[HubExtrasDeclared] Unhandled exception: {ex}");
                }
            });

            log?.Information("[HubExtrasDeclared] exit");
        }

        public async Task HubThrowCards() // called by client
        {
            log?.Information("[HubThrowCards] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubThrowCards] GameContext/Game/Observer was null");
                return;
            }

            var game = gameContext.Game;
            var group = Clients.Group(game.RoomId);

            BelotGameEngine engine = new(game, gameContext.Observer);

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

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error($"[HubThrowCards] Unhandled exception: {ex}");
                }
            });

            log?.Information("[HubThrowCards] exit");
        }

        #endregion

        #region Seat Management

        public async Task HubBookSeat(int position) // called by client // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot, 8 = vacate
        {
            log?.Information("[HubBookSeat] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubBookSeat] GameContext/Game/Observer was null");
                return;
            }

            var game = gameContext.Game;
            var clients = Clients;
            var group = Clients.Group(game.RoomId);

            string[] seats = ["West", "North", "East", "South"];

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

                var isBot = occupier == botGUID;
                var isEmpty = string.IsNullOrEmpty(occupier);
                var isSelf = occupier == requestor;

                if ((isEmpty || isBot) && position < 4) // empty seat or bot-occupied requested by human
                {
                    await UnbookSeat(game, clients);
                    var spectator = game.Spectators.FirstOrDefault(s => s.Username == requestor);
                    if (spectator != null)
                    {
                        game.Spectators.Remove(spectator);
                    }
                    game.Players[position] = new Player(requestor, Context.ConnectionId, PlayerType.Human);
                    await UpdateConnectedUsers(game, clients);
                    await clients.OthersInGroup(game.RoomId).SendAsync("seatBooked", position, requestor, false);
                    await clients.Caller.SendAsync("seatBooked", position, requestor, true);
                    await clients.Caller.SendAsync("enableRotation", true);
                    string[] scoreSummary = { "Us", "Them" };
                    await clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(position + 1) % 2], scoreSummary[position % 2]);

                    await clients.OthersInGroup(game.RoomId).SendAsync("EnableSeatOptions", position, false);
                    await group.SendAsync("EnableOccupySeat", position, false);
                    await clients.OthersInGroup(game.RoomId).SendAsync("EnableAssignBotToSeat", position, false);
                    await clients.Caller.SendAsync("EnableAssignBotToSeat", position, true);
                    await clients.Caller.SendAsync("EnableVacateSeat", position, true);

                    await group.SendAsync("SetBotBadge", position, false);
                }
                else if (isEmpty && position > 3) // empty seat requested by bot
                {
                    position -= 4;
                    string botName = GetBotName(position);
                    game.Players[position] = new Player(botGUID, "", PlayerType.Basic);
                    await UpdateConnectedUsers(game, clients);
                    await group.SendAsync("SeatBooked", position, botName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                    await group.SendAsync("EnableAssignBotToSeat", position, false);
                    await clients.Caller.SendAsync("EnableVacateSeat", position, false);
                }
                // if bot occupied seat requested by bot -> do nothing
                else if (isSelf && position > 3) // human assigns bot to his own occupied seat
                {
                    position -= 4;
                    string botName = GetBotName(position);
                    await UnbookSeat(game, clients);
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    game.Players[position] = new Player(botGUID, "", PlayerType.Basic);
                    await UpdateConnectedUsers(game, clients);
                    await group.SendAsync("SeatBooked", position, botName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                    await group.SendAsync("EnableOccupySeat", position, true);
                    await group.SendAsync("EnableAssignBotToSeat", position, false);
                }
                // if human tries to occupy his own seat, do nothing
                else if (!isEmpty && !isBot && !isSelf) // human-occupied seat is requested by another human or by a bot on behalf of another human
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

            log?.Information("[HubBookSeat] exit");
        }

        private async Task UnbookSeat(BelotGame game, IHubCallerClients clients)
        {
            log?.Information("[UnbookSeat] enter");
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
            log?.Information("[UnbookSeat] exit");
        }

        #endregion

        #region Messaging

        private string MsgHead()
        {
            return GetServerDateTime() + ", " + GetCallerUsername();
        }

        public async void HubAnnounce(string message) // called by client
        {
            log?.Information("[HubAnnounce] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubBookSeat] GameContext/Game/Observer was null");
                return;
            }

            var group = Clients.Group(gameContext.Game.RoomId);
            await group.SendAsync("Announce", MsgHead() + " >> " + message);
            await group.SendAsync("showChatNotification");

            log?.Information("[HubAnnounce] exit");
        }

        #endregion

        #region Helpers

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
            if (game.Players[game.Turn].PlayerType == PlayerType.Human)
            {
                return game.Players[game.Turn].Username;
            }
            else
            {
                return GetBotName(game.Turn);
            }
        }

        private BelotGameContext? GetGameContext()
        {
            if (!allConnections.TryGetValue(Context.ConnectionId, out string? roomId))
            {
                return null;
            }
            return _gameRegistry.GetContext(roomId);
        }

        #endregion

        #region Connection

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
                if (game.Players[i].PlayerType == PlayerType.Human)
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
                    await clients.Caller.SendAsync("SetTableCard", i, game.TableCards[i]);
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
            log?.Information("[OnConnected] enter");

            if (Context?.GetHttpContext()?.GetRouteValue("roomId") is not string roomId)
            {
                log?.Warning("[OnConnected] roomId was null");
                return;
            }

            allConnections.Add(Context.ConnectionId, roomId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            var clients = Clients;

            var gameContext = GetGameContext();
            if (gameContext?.Game == null)
            {
                log?.Warning("[OnConnected] GameContext/Game was null");
                return;
            }

            if (gameContext.Observer == null)
            {
                gameContext.Observer = new LiveBelotObserver(gameContext.Game, Clients);
            }
            else
            {
                _gameRegistry.RefreshObserver(roomId, Clients);
            }

            BelotGame game = gameContext.Game;

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

            if (gameContext.Observer is LiveBelotObserver liveObserver)
            {
                await liveObserver.SysAnnounce(username + " connected.");
            }

            log?.Information(username + " Connected.");

            await LoadContext(game, clients);

            log?.Information("[OnConnected] exit");
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? ex)
        {
            log?.Information("[OnDisconnected] enter");

            var gameContext = GetGameContext();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[OnDisconnected] GameContext/Game/Observer was null");
                return;
            }

            BelotGame game = gameContext.Game;
            var clients = Clients;

            string username = GetCallerUsername();
            bool playerReallyDisconnected = false; // guards against race condition of a reconnect updating ConnectionId but not setting player.IsDisconnected = false before continuing here

            var spectator = game.Spectators.FirstOrDefault(p => p.Username == username);
            if (spectator != null)
            {
                game.Spectators.Remove(spectator);
                await UpdateConnectedUsers(game, clients);
                if (gameContext.Observer is LiveBelotObserver live)
                {
                    await live.SysAnnounce(username + " disconnected.");
                }
                playerReallyDisconnected = true;
            }
            else
            {
                await Task.Delay(500); // allow for possible reconnect
                var player = game.Players.FirstOrDefault(p => p.Username == username);
                if (player != null && player.ConnectionId == Context.ConnectionId) // player has not reconnected, connectionId is stale
                {
                    await UnbookSeat(game, clients); // this will mark them as disconnected
                    if (gameContext.Observer is LiveBelotObserver live)
                    {
                        await live.SysAnnounce(username + " disconnected.");
                    }
                    log?.Information($"[OnDisconnected] {username} disconnected after delay.");
                    playerReallyDisconnected = true;
                }
                else // player reconnected, don't proceed with disconnection
                {
                    log?.Information($"[OnDisconnected] {username} reconnected before disconnect cleanup.");
                }

            }

            if (playerReallyDisconnected && game.Spectators.Count + game.Players.Count(p => p.PlayerType == PlayerType.Human && !p.IsDisconnected) == 0)
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

                if (game.Spectators.Count + game.Players.Count(p => p.PlayerType == PlayerType.Human && !p.IsDisconnected) == 0)
                {
                    _gameRegistry.RemoveContext(game.RoomId);
                    game.IsRunning = false;
                    game.CloseLog();
                }
            }

            allConnections.Remove(Context.ConnectionId);

            _gameRegistry.RefreshObserver(game.RoomId, Clients);

            log?.Information("[OnDisconnected] exit");

            if (!_gameRegistry.GamesOngoing())
            {
                log?.Dispose();
                log = null;
            }

            await base.OnDisconnectedAsync(ex);
        }

        #endregion
    }
}

// when human to deal, nominate suit modal is shown before dealing
// agent still console logging for the play decisions they make