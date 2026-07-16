using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Players;
using BelotWebApp.Services.AppPathService;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoomHub : Hub
    {
        private readonly BelotRoomRegistry _gameRegistry;

        public static Dictionary<string, string> allConnections = []; // connectionId, roomId

        public static readonly string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";

        public static Serilog.Core.Logger log;// = new LoggerConfiguration().WriteTo.File(ConfigurationManager.AppSettings["logfilepath"] + "BelotServerLog-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        public BelotRoomHub(IAppPaths appPaths, BelotRoomRegistry gameRegistry)
        {
            _gameRegistry = gameRegistry;

            log ??= new LoggerConfiguration().WriteTo.File(Path.Combine(appPaths.LogFolder, "BelotServerLog-.txt"), rollingInterval: RollingInterval.Day).CreateLogger();
        }

        #region Game Loop Continuation

        public Task HubGameController() // called by client
        {
            log?.Information("[HubGameController] enter");

            var gameContext = GetRoom();
            var game = gameContext?.Game;

            if (game == null || gameContext?.Observer == null)
            {
                log?.Warning("[HubGameController] GameContext/Game/Observer was null");
                log?.Information("[HubGameController] exit");
                return Task.CompletedTask;
            }

            if (!game.Players.All(p => p.Username != ""))
            {
                log?.Warning("[HubGameController] Game does not have 4 players");
                log?.Information("[HubGameController] exit");
                return Task.CompletedTask;
            }

            if (game.Players.Any(p => p.PlayerType == PlayerType.Human) && game.Spectators.Any(s => s.Username == GetCallerUsername()))
            {
                log?.Warning("[HubGameController] Game start not called by valid player");
                log?.Information("[HubGameController] exit");
                return Task.CompletedTask;
            }

            BelotGameEngine engine = new(game, gameContext.Observer);

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

            var gameContext = GetRoom();
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

            var gameContext = GetRoom();
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

            var gameContext = GetRoom();
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

            var gameContext = GetRoom();
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

            var room = GetRoom();
            if (room?.Game == null || room.Observer == null)
            {
                log?.Warning("[HubThrowCards] GameContext/Game/Observer was null");
                return;
            }

            var game = room.Game;
            var group = Clients.Group(room.RoomId);

            BelotGameEngine engine = new(game, room.Observer);

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

        public async Task HubGetSeatActions(int position)
        {
            log?.Information("[HubGetSeatActions] enter");

            var gameContext = GetRoom();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[HubGetSeatActions] GameContext/Game/Observer was null");
                return;
            }

            var game = gameContext.Game;
            var clients = Clients;

            string requestor = GetCallerUsername();

            var players = game.Players;

            bool occupiedByHuman = players[position].PlayerType == PlayerType.Human;
            bool occupiedByBot = !string.IsNullOrEmpty(players[position].Username) && players[position].PlayerType != PlayerType.Human;
            bool isMe = players[position].Username == requestor;

            var actions = new
            {
                CanOccupy = !occupiedByHuman,
                CanAssignBot = !occupiedByBot && (!occupiedByHuman || isMe), // not occuped by bot already and not occupied by another human (not me)
                CanVacate = isMe
            };

            await clients.Caller.SendAsync("SetSeatActions", actions, position);

            log?.Information("[HubGetSeatActions] exit");
        }

        public async Task HubBookSeat(int position) // called by client // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot, 8 = vacate
        {
            log?.Information("[HubBookSeat] enter");

            var room = GetRoom();
            if (room?.Game == null || room.Observer == null)
            {
                log?.Warning("[HubBookSeat] GameContext/Game/Observer was null");
                return;
            }

            var game = room.Game;
            var clients = Clients;
            var group = Clients.Group(room.RoomId);

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
                    await UnbookSeat(room.RoomId, game, clients, false);
                    var spectator = game.Spectators.FirstOrDefault(s => s.Username == requestor);
                    if (spectator != null)
                    {
                        game.Spectators.Remove(spectator);
                    }
                    game.Players[position] = new Player(requestor, Context.ConnectionId, PlayerType.Human);
                    await UpdateConnectedUsers(room.RoomId, game, clients);
                    await clients.OthersInGroup(room.RoomId).SendAsync("seatBooked", position, requestor, false);
                    await clients.Caller.SendAsync("seatBooked", position, requestor, true);
                    string[] scoreSummary = ["Us", "Them"];
                    await clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(position + 1) % 2], scoreSummary[position % 2]);

                    await group.SendAsync("SetBotBadge", position, false);
                }
                else if (isEmpty && position > 3) // empty seat requested by bot
                {
                    position -= 4;
                    string botName = GetBotName(position);
                    game.Players[position] = new Player(botGUID, "", PlayerType.Basic);
                    await UpdateConnectedUsers(room.RoomId, game, clients);
                    await group.SendAsync("SeatBooked", position, botName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                }
                // if bot occupied seat requested by bot -> do nothing
                else if (isSelf && position > 3) // human assigns bot to his own occupied seat
                {
                    position -= 4;
                    string botName = GetBotName(position);
                    await UnbookSeat(room.RoomId, game, clients, true);
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    game.Players[position] = new Player(botGUID, "", PlayerType.Basic);
                    await UpdateConnectedUsers(room.RoomId, game, clients);
                    await group.SendAsync("SeatBooked", position, botName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                }
                // if human tries to occupy his own seat, do nothing
                // human-occupied seat is requested by another human or by a bot on behalf of another human

                await EnableGameStart(game, clients, group);
            }
            else // vacate and become spectator
            {
                if (!game.Spectators.Any(s => s.Username == requestor))
                {
                    await UnbookSeat(room.RoomId, game, clients, true);
                    game.Spectators.Add(new Spectator(requestor, Context.ConnectionId));
                    await UpdateConnectedUsers(room.RoomId, game, clients);
                }
            }

            log?.Information("[HubBookSeat] exit");
        }

        private async Task UnbookSeat(string roomId, BelotGame game, IHubCallerClients clients, bool resetSeatOrientations)
        {
            log?.Information("[UnbookSeat] enter");
            var group = clients.Group(roomId);
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
                await clients.Caller.SendAsync("SetScoreTitles", "N /S", "E/W");
                await clients.Caller.SendAsync("SeatUnbooked", position, resetSeatOrientations);
                await clients.OthersInGroup(roomId).SendAsync("SeatUnbooked", position, false);
            }

            await UpdateConnectedUsers(roomId, game, clients);
            log?.Information("[UnbookSeat] exit");
        }

        #endregion

        #region Messaging

        public async void HubAnnounce(string message) // called by client
        {
            log?.Information("[HubAnnounce] enter");

            if (string.IsNullOrEmpty(message))
            {
                log?.Information("[HubAnnounce] exit");
                return;
            }

            var room = GetRoom();
            if (room?.Game == null || room.Observer == null)
            {
                log?.Warning("[HubAnnounce] GameContext/Game/Observer was null");
                log?.Information("[HubAnnounce] exit");
                return;
            }

            if (!room.Options.AllowChat)
            {
                log?.Warning("[HubAnnounce] Tried chat when chat is disabled");
                log?.Information("[HubAnnounce] exit");
                return;
            }

            var group = Clients.Group(room.RoomId);
            await group.SendAsync("AppendChatLog", $"[{GetServerDateTime()} • {GetCallerUsername()}] {message}");
            await group.SendAsync("showChatNotification");

            log?.Information("[HubAnnounce] exit");
        }

        #endregion

        #region RoundSummary

        public Task RoundSummaryVoteToContinue(Guid roundToken)
        {
            log?.Information("[RoundSummaryVoteToContinue] enter");

            var gameContext = GetRoom();
            if (gameContext?.Game == null || gameContext.Observer == null)
            {
                log?.Warning("[RoundSummaryVoteToContinue] GameContext/Game/Observer was null");
                return Task.CompletedTask;
            }

            var username = GetCallerUsername();

            if (gameContext.Game.Players.Any(p => p.Username == username) && gameContext.Observer is LiveBelotObserver live)
            {
                live.RoundSummaryGate.RegisterContinueVote(GetCallerUsername(), roundToken);
            }

            log?.Information("[RoundSummaryVoteToContinue] exit");

            return Task.CompletedTask;
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
            return Context.User?.Identity?.Name ?? "Unknown Entity";
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

        private BelotRoom? GetRoom()
        {
            if (!allConnections.TryGetValue(Context.ConnectionId, out string? roomId))
            {
                return null;
            }
            return _gameRegistry.GetRoom(roomId);
        }

        #endregion

        #region Connection

        public async Task UpdateConnectedUsers(string roomId, BelotGame game, IHubCallerClients clients)
        {
            string[] playerNames = game.Players.Where(d => !d.IsDisconnected && d.Username != "" && d.Username != botGUID).Select(s => s.Username).ToArray();
            Array.Sort(playerNames);
            string[] specNames = game.Spectators.Select(s => s.Username).ToArray();
            Array.Sort(specNames);
            await clients.Group(roomId).SendAsync("ConnectedUsers", playerNames, specNames);
        }

        public async Task LoadContext(string roomId, BelotGame game, IHubCallerClients clients)
        {
            await clients.Caller.SendAsync("SetGameId", game.GameId);

            for (int i = 0; i < 4; i++)
            {
                // Update table seats
                if (game.Players[i].PlayerType == PlayerType.Human && !game.Players[i].IsDisconnected)
                {
                    if (game.Players[i].Username == Context.User.Identity.Name)
                    {
                        await clients.Caller.SendAsync("SeatBooked", i, game.Players[i].Username, true);
                    }
                    else
                    {
                        await clients.Caller.SendAsync("SeatBooked", i, game.Players[i].Username, false);
                    }
                }
                else if (game.Players[i].Username == botGUID)
                {
                    string[] seat = ["West", "North", "East", "South"];
                    await clients.Caller.SendAsync("SeatBooked", i, GetBotName(i), false);
                    await clients.Caller.SendAsync("SetBotBadge", i, true);
                }

                // Update table cards
                if (!game.IsNewGame)
                {
                    await clients.Caller.SendAsync("SetTableCard", i, game.TableCards[i]);
                }
            }

            bool playerIsInGame = game.Players.Any(u => u.Username == GetCallerUsername());
            int pos = -1;

            if (playerIsInGame)
            {
                string[] scoreSummary = ["Us", "Them"];
                pos = Array.IndexOf(game.Players, game.Players.FirstOrDefault(p => p.Username == GetCallerUsername()));
                await clients.Caller.SendAsync("SetScoreTitles", scoreSummary[(pos + 1) % 2], scoreSummary[pos % 2]);
            }

            if (!game.IsNewGame)
            {
                await clients.Caller.SendAsync("HideDeck", true);

                int dealer = game.FirstPlayer + 1;
                if (dealer == 4) dealer = 0;

                await clients.Caller.SendAsync("SetDealerMarker", dealer);
                await clients.Caller.SendAsync("SetTurnIndicator", game.Turn, game.GetCurrentTurnActionType()?.ToString().ToLower());
                await clients.Caller.SendAsync("DisableRadios");

                await clients.Caller.SendAsync("UpdateScoreTotals", game.EWTotal, game.NSTotal);
                await clients.Caller.SendAsync("UpdateScoreHistoryTable");

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
                if (playerIsInGame)
                {
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
            else
            {
                await EnableGameStart(game, clients, Clients.Group(roomId));
            }
        }

        private static async Task EnableGameStart(BelotGame game, IHubCallerClients clients, IClientProxy group)
        {
            if (!game.Players.All(p => p.Username != ""))
            {
                return;
            }

            if (game.Players.All(p => p.PlayerType != PlayerType.Human))
            {
                await group.SendAsync("EnableNewGame");
            }
            else
            {
                foreach (var spectator in game.Spectators)
                {
                    await clients.Client(spectator.ConnectionId).SendAsync("DisableNewGame");
                }
                foreach (var player in game.Players.Where(p => p.PlayerType == PlayerType.Human && !p.IsDisconnected))
                {
                    await clients.Client(player.ConnectionId).SendAsync("EnableNewGame");
                }
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

            var room = GetRoom();
            if (room?.Game == null)
            {
                log?.Warning("[OnConnected] GameContext/Game was null");
                return;
            }

            if (room.Observer == null)
            {
                room.Observer = new LiveBelotObserver(room.RoomId, room.Game, Clients);
            }
            else
            {
                _gameRegistry.RefreshObserver(roomId, Clients);
            }

            BelotGame game = room.Game;

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
                await clients.OthersInGroup(room.RoomId).SendAsync("SeatBooked", pos, username, false);
            }
            await UpdateConnectedUsers(room.RoomId, game, clients);

            if (room.Observer is LiveBelotObserver liveObserver)
            {
                await liveObserver.SysAnnounce(username + " connected.");
            }

            log?.Information(username + " Connected.");

            await LoadContext(room.RoomId, game, clients);

            log?.Information("[OnConnected] exit");
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? ex)
        {
            log?.Information("[OnDisconnected] enter");

            var room = GetRoom();
            if (room?.Game == null || room.Observer == null)
            {
                log?.Warning("[OnDisconnected] GameContext/Game/Observer was null");
                return;
            }

            BelotGame game = room.Game;
            var clients = Clients;

            string username = GetCallerUsername();
            bool playerReallyDisconnected = false; // guards against race condition of a reconnect updating ConnectionId but not setting player.IsDisconnected = false before continuing here

            var spectator = game.Spectators.FirstOrDefault(p => p.Username == username);
            if (spectator != null)
            {
                game.Spectators.Remove(spectator);
                await UpdateConnectedUsers(room.RoomId, game, clients);
                if (room.Observer is LiveBelotObserver live)
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
                    await UnbookSeat(room.RoomId, game, clients, true); // this will mark them as disconnected
                    if (room.Observer is LiveBelotObserver live)
                    {
                        await live.SysAnnounce(username + " disconnected.");
                        live.RoundSummaryGate.RegisterDisconnect(username);
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
                if (room.Observer is LiveBelotObserver live)
                {
                    int oldwinnerDelay = game.WinnerDelay;
                    int oldBotDelay = game.BotDelay;
                    int oldRoundSummaryDelay = live.RoundSummaryGate.RoundSummaryDelay;
                    game.WinnerDelay = 0;
                    game.BotDelay = 0;
                    live.RoundSummaryGate.RoundSummaryDelay = 0;
                    await Task.Delay(1500);
                    game.WinnerDelay = oldwinnerDelay;
                    game.BotDelay = oldBotDelay;
                    live.RoundSummaryGate.RoundSummaryDelay = oldRoundSummaryDelay;
                }

                if (game.Spectators.Count + game.Players.Count(p => p.PlayerType == PlayerType.Human && !p.IsDisconnected) == 0)
                {
                    _gameRegistry.RemoveRoom(room.RoomId);
                    game.IsRunning = false;
                    game.CloseLog();
                }
            }

            allConnections.Remove(Context.ConnectionId);

            _gameRegistry.RefreshObserver(room.RoomId, Clients);

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