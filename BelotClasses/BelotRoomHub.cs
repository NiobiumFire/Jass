using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Users;
using BelotWebApp.Services.AppPathService;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Context;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoomHub : Hub
    {
        private readonly BelotRoomRegistry _roomRegistry;

        private static ConcurrentDictionary<string, string> allConnections = []; // connectionId, roomId

        private static Serilog.Core.Logger log;

        public BelotRoomHub(IAppPaths appPaths, BelotRoomRegistry gameRegistry)
        {
            _roomRegistry = gameRegistry;

            log ??= new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Combine(appPaths.LogFolder, "BelotServerLog-.txt"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message} RoomId={RoomId}{NewLine}{Exception}")
                .CreateLogger();
        }

        private static IDisposable BeginRoomLogScope(BelotRoom? room)
        {
            return LogContext.PushProperty("RoomId", room?.RoomId ?? "Unknown");
        }

        private (BelotRoom? room, ConnectedUser? user) ValidateEntry(string entryPoint)
        {
            var room = GetRoom();

            if (room?.Game == null || room.Observer == null)
            {
                log?.Warning($"[{entryPoint}] Room/Game/Observer was null");
                return (null, null);
            }

            using var logScope = BeginRoomLogScope(room);

            var user = room.GetUserById(GetCallerUserId());
            if (user == null)
            {
                log?.Warning($"[{entryPoint}] user was null");
                return (null, null);
            }

            var connectionId = Context.ConnectionId;
            if (user.ConnectionId != connectionId)
            {
                log?.Warning($"[{entryPoint}] connection {connectionId} has been superseded by {user.ConnectionId}");
                Context.Abort();
                return (null, null);
            }

            return (room, user);
        }

        #region Game Loop Continuation

        public Task HubGameController() // called by client
        {
            string entryPoint = "HubGameController";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return Task.CompletedTask;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            var game = room.Game;

            if (game.Players.Any(p => p == null))
            {
                log?.Warning($"[{entryPoint}] Game does not have 4 players");
                return Task.CompletedTask;
            }

            if (game.Players.Any(p => p != null && p.PlayerType == PlayerType.Human) && room.GetPlayerById(GetCallerUserId()) == null)
            {
                log?.Warning($"[{entryPoint}] Game start not called by valid player");
                return Task.CompletedTask;
            }

            BelotGameEngine engine = new(game, room.Observer);

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error(ex, $"[{entryPoint}] Unhandled exception");
                }
            });

            log?.Information($"[{entryPoint}] exit");

            return Task.CompletedTask;
        }

        public Task HubShuffle() // called by client
        {
            string entryPoint = "HubShuffle";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return Task.CompletedTask;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            BelotGameEngine engine = new(room.Game, room.Observer);

            room.Game.WaitDeal = false;

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error(ex, $"[{entryPoint}] Unhandled exception");
                }
            });

            log?.Information($"[{entryPoint}] exit");

            return Task.CompletedTask;
        }

        public async Task HubNominateSuit(Call call) // called by client
        {
            string entryPoint = "HubNominateSuit";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            BelotGameEngine engine = new(room.Game, room.Observer);

            room.Game.NominateSuit(call);
            room.Game.WaitCall = false;
            if (room.Observer is LiveBelotObserver live)
            {
                await live.AnnounceSuit();
            }
            if (--room.Game.Turn == -1) room.Game.Turn = 3;

            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.GameController();
                }
                catch (Exception ex)
                {
                    log?.Error(ex, $"[{entryPoint}] Unhandled exception");
                }
            });

            log?.Information($"[{entryPoint}] exit");

            return;
        }

        public async Task HubPlayCard(Card card) // called by client
        {
            string entryPoint = "HubPlayCard";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            var game = room.Game;
            var clients = Clients;

            room.Game.PlayCard(card);
            await clients.Caller.SendAsync("SetTableCard", game.Turn, game.TableCards[game.Turn]);
            await clients.Caller.SendAsync("DeclareExtras", game.Declarations.Where(d => d.Player == game.Turn && d.IsDeclarable));

            log?.Information($"[{entryPoint}] exit");
        }

        public async Task HubExtrasDeclared(List<Declaration> declarations) // called by client
        {
            string entryPoint = "HubExtrasDeclared";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            var game = room.Game;

            BelotGameEngine engine = new(game, room.Observer);

            var validDeclarations = declarations?.Where(d => d != null) ?? [];

            List<string> messages = [];
            List<string> emotes = [];

            if (game.RoundCall != Call.NoTrumps)
            {
                var declaredDeclarations = game.DeclareDeclarations(validDeclarations);


                if (room.Observer is LiveBelotObserver live)
                {
                    //await live.OnDeclaration(messages, emotes);
                    await live.OnDeclaration(declaredDeclarations);
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
                    log?.Error(ex, $"[{entryPoint}] Unhandled exception");
                }
            });

            log?.Information($"[{entryPoint}] exit");
        }

        public async Task HubThrowCards() // called by client
        {
            string entryPoint = "HubThrowCards";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

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

            await group.SendAsync("ThrowCards", room.GetDisplayName(game.Turn), game.Hand);
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
                    log?.Error(ex, $"[{entryPoint}] Unhandled exception");
                }
            });

            log?.Information($"[{entryPoint}] exit");
        }

        #endregion

        #region Seat Management

        public async Task HubGetSeatActions(int position) // called by client
        {
            string entryPoint = "HubGetSeatActions";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            var game = room.Game;
            var clients = Clients;

            string requestorId = GetCallerUserId();

            var players = game.Players;

            bool occupiedByHuman = players[position]?.PlayerType == PlayerType.Human;
            bool occupiedByBot = players[position] != null && players[position]?.PlayerType != PlayerType.Human;
            bool isMe = room.Game.Players[position]?.PlayerId == requestorId;

            var actions = new
            {
                CanOccupy = !occupiedByHuman,
                CanAssignBot = !occupiedByBot && (!occupiedByHuman || isMe), // not occuped by bot already and not occupied by another human (not me)
                CanVacate = isMe
            };

            await clients.Caller.SendAsync("SetSeatActions", actions, position);

            log?.Information($"[{entryPoint}] exit");
        }

        public async Task HubBookSeat(int position) // called by client // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot, 8 = vacate
        {
            string entryPoint = "HubBookSeat";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            if (position is < 0 or > 8)
            {
                log?.Warning($"[{entryPoint}] invalid position {position}", position);
                return;
            }

            if (!room.Game.IsNewGame)
            {
                log?.Warning($"[{entryPoint}] booking requested after game start");
                return;
            }

            var game = room.Game;
            var clients = Clients;
            var group = Clients.Group(room.RoomId);

            string requestorId = GetCallerUserId();
            string requestorUsername = GetCallerUsername();

            if (position != 8)
            {
                string? occupierId = game.Players[position % 4]?.PlayerId;

                var isBot = occupierId == Player._botGUID;
                var isEmpty = string.IsNullOrEmpty(occupierId);
                var isSelf = occupierId == requestorId;

                if ((isEmpty || isBot) && position < 4) // empty seat or bot-occupied requested by human
                {
                    await UnbookSeat(room, clients, false);
                    game.Players[position] = new(requestorId, requestorUsername, PlayerType.Human);
                    await UpdateConnectedUsers(room, clients);
                    await clients.OthersInGroup(room.RoomId).SendAsync("seatBooked", position, requestorUsername, false);
                    await clients.Caller.SendAsync("seatBooked", position, requestorUsername, true);
                    await clients.Caller.SendAsync("SetScoreTitles", "Us", "Them");

                    await group.SendAsync("SetBotBadge", position, false);
                }
                else if (isEmpty && position > 3) // empty seat requested for bot
                {
                    position -= 4;
                    game.Players[position] = new Player(position);
                    await UpdateConnectedUsers(room, clients);
                    await group.SendAsync("SeatBooked", position, game.Players[position]!.PlayerName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                }
                // if bot-occupied seat requested for bot -> do nothing
                else if (isSelf && position > 3) // human assigns bot to his own occupied seat
                {
                    position -= 4;
                    await UnbookSeat(room, clients, true);
                    game.Players[position] = new Player(position);
                    await UpdateConnectedUsers(room, clients);
                    await group.SendAsync("SeatBooked", position, game.Players[position]!.PlayerName, false);
                    await group.SendAsync("SetBotBadge", position, true);
                }
                // if human tries to occupy his own seat, do nothing
                // human-occupied seat is requested by another human or by a bot on behalf of another human

                await EnableGameStart(room, clients, group);
            }
            else // vacate and become spectator
            {
                await UnbookSeat(room, clients, true);
            }

            log?.Information($"[{entryPoint}] exit");
        }

        private async Task UnbookSeat(BelotRoom room, IHubCallerClients clients, bool resetSeatOrientations) // only for players: we cannot unbook a bot directly but we can take their seat
        {
            string entryPoint = "UnbookSeat";
            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            string userId = GetCallerUserId();
            var player = room.GetPlayerById(userId);
            if (player != null) //unbook only if I was booked
            {
                var group = clients.Group(room.RoomId);

                var position = Array.IndexOf(room.Game.Players, player);
                if (room.Game.IsNewGame)
                {
                    await group.SendAsync("DisableNewGame");
                    room.Game.Players[position] = null;
                }
                else
                {
                    player.IsDisconnected = true;
                }
                await clients.Caller.SendAsync("SetScoreTitles", "N/S", "E/W");
                await clients.Caller.SendAsync("SeatUnbooked", position, resetSeatOrientations);
                await clients.OthersInGroup(room.RoomId).SendAsync("SeatUnbooked", position, false);
                await UpdateConnectedUsers(room, clients);
            }

            log?.Information($"[{entryPoint}] exit");
        }

        #endregion

        #region Messaging

        public async void HubAnnounce(string message) // called by client
        {
            string entryPoint = "HubAnnounce";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            if (string.IsNullOrEmpty(message))
            {
                log?.Warning($"[{entryPoint}] empty chat message");
                return;
            }

            if (!room.Options.AllowChat)
            {
                log?.Warning($"[{entryPoint}] chat when chat is disabled");
                return;
            }

            var group = Clients.Group(room.RoomId);
            await group.SendAsync("AppendChatLog", $"[{GetServerDateTime()} • {GetCallerUsername()}] {message}");
            await group.SendAsync("showChatNotification");

            log?.Information($"[{entryPoint}] exit");
        }

        #endregion

        #region RoundSummary

        public Task RoundSummaryVoteToContinue(Guid roundToken) // called by client
        {
            string entryPoint = "RoundSummaryVoteToContinue";
            var (room, user) = ValidateEntry(entryPoint);
            if (room == null || user == null || room.Game == null || room.Observer == null)
            {
                return Task.CompletedTask;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            var userId = GetCallerUserId();

            if (room.Game.Players.Any(p => p?.PlayerId == userId) && room.Observer is LiveBelotObserver live)
            {
                live.RoundSummaryGate.RegisterContinueVote(GetCallerUsername(), roundToken);
            }

            log?.Information($"[{entryPoint}] exit");

            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private static string GetServerDateTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }

        private string GetCallerUserId()
        {
            return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown Entity";
        }

        private string GetCallerUsername()
        {
            return Context.User?.Identity?.Name ?? "Unknown Entity";
        }

        private BelotRoom? GetRoom()
        {
            if (!allConnections.TryGetValue(Context.ConnectionId, out string? roomId))
            {
                return null;
            }
            return _roomRegistry.GetRoom(roomId);
        }

        #endregion

        #region Connection

        private static async Task UpdateConnectedUsers(BelotRoom room, IHubCallerClients clients)
        {
            var (spectators, players) = room.GetSpectatorsAndConnectedHumanPlayers();
            var spectatorNames = spectators.Select(u => u.Username).OrderBy(u => u).ToArray();
            var playerNames = players.Select(u => u.Username).OrderBy(u => u).ToArray();
            await clients.Group(room.RoomId).SendAsync("ConnectedUsers", playerNames, spectatorNames);
        }

        private async Task LoadContext(BelotRoom room, ConnectedUser user, IHubCallerClients clients)
        {
            var game = room.Game;

            if (game.Players == null || user == null)
            {
                return;
            }

            await clients.Caller.SendAsync("SetGameId", game.GameId);

            for (int i = 0; i < 4; i++)
            {
                // Update table seats
                var player_i = game.Players[i];
                if (player_i != null)
                {
                    if (player_i.PlayerType != PlayerType.Human)
                    {
                        await clients.Caller.SendAsync("SeatBooked", i, player_i.PlayerName, false);
                        await clients.Caller.SendAsync("SetBotBadge", i, true);
                    }
                    else if (!player_i.IsDisconnected)
                    {
                        if (player_i.PlayerName == GetCallerUsername())
                        {
                            await clients.Caller.SendAsync("SeatBooked", i, player_i.PlayerName, true);
                        }
                        else
                        {
                            await clients.Caller.SendAsync("SeatBooked", i, player_i.PlayerName, false);
                        }
                    }
                }

                // Update table cards
                if (!game.IsNewGame)
                {
                    await clients.Caller.SendAsync("SetTableCard", i, game.TableCards[i]);
                }
            }

            var player = room.GetPlayerById(user.UserId);
            int pos = Array.IndexOf(room.Game.Players, player);

            if (pos >= 0) // user is player
            {
                await clients.Caller.SendAsync("SetScoreTitles", "Us", "Them");
            }

            if (game?.IsNewGame == false)
            {
                await clients.Caller.SendAsync("HideDeck", true);

                int dealer = game.FirstPlayer + 1;
                if (dealer == 4) dealer = 0;

                await clients.Caller.SendAsync("SetDealerMarker", dealer);
                await clients.Caller.SendAsync("SetTurnIndicator", game.Turn, game.GetCurrentTurnActionType()?.ToString().ToLower());
                await clients.Caller.SendAsync("DisableRadios");

                var ewFirst = game?.Players[0]?.PlayerId == user.UserId || game?.Players[2]?.PlayerId == user.UserId;

                await clients.Caller.SendAsync("UpdateScoreTotals", game.EWTotal, game.NSTotal, ewFirst);
                await clients.Caller.SendAsync("UpdateScoreHistoryTable", ewFirst);

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
                if (pos >= 0)
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
                await EnableGameStart(room, clients, Clients.Group(room.RoomId));
            }
        }

        private static async Task EnableGameStart(BelotRoom room, IHubCallerClients clients, IClientProxy group)
        {
            if (room.Game.Players.Any(p => p == null))
            {
                return;
            }

            if (room.Game.Players.All(p => p!.PlayerType != PlayerType.Human))
            {
                await group.SendAsync("EnableNewGame");
            }
            else
            {
                var (spectators, players) = room.GetSpectatorsAndConnectedHumanPlayers();
                foreach (var spectator in spectators)
                {
                    await clients.Client(spectator.ConnectionId).SendAsync("DisableNewGame");
                }
                foreach (var player in players)
                {
                    await clients.Client(player.ConnectionId).SendAsync("EnableNewGame");
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            string entryPoint = "OnConnected";
            if (Context?.GetHttpContext()?.GetRouteValue("roomId") is not string roomId)
            {
                log?.Warning($"[{entryPoint}] roomId was null");
                return;
            }

            allConnections.TryAdd(Context.ConnectionId, roomId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            var clients = Clients;

            var room = GetRoom();

            if (room?.Game == null)
            {
                allConnections.TryRemove(Context.ConnectionId, out _);
                Context.Abort();
                log?.Warning($"[{entryPoint}] Room/Game was null");
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            if (room.Observer == null)
            {
                room.Observer = new LiveBelotObserver(room, Clients);
            }
            else
            {
                _roomRegistry.RefreshObserver(roomId, Clients);
            }

            BelotGame game = room.Game;

            string connectionType;

            string userId = GetCallerUserId();
            string username = GetCallerUsername();
            var user = room.GetUserById(userId); // check for a user reconnecting from a quick page refresh
            if (user != null)
            {
                await clients.Client(user.ConnectionId).SendAsync("ConnectionSuperseded");
                room.UpdateUser(user, username, Context.ConnectionId);
                connectionType = "reconnected";
            }
            else
            {
                user = room.AddUser(userId, username, Context.ConnectionId);
                connectionType = "connected";
                var player = room.GetPlayerById(userId); // check for an existing booking and reassign
                if (player != null)
                {
                    var pos = Array.IndexOf(game.Players, player);
                    player.PlayerName = username;
                    player.IsDisconnected = false;
                    await clients.OthersInGroup(room.RoomId).SendAsync("SeatBooked", pos, username, false);
                }
            }
            await UpdateConnectedUsers(room, clients);

            if (room.Observer is LiveBelotObserver liveObserver)
            {
                await liveObserver.SysAnnounce($"{username} {connectionType}.");
            }

            log?.Information($"[{entryPoint}] {username} {connectionType}");

            await LoadContext(room, user, clients);

            log?.Information($"[{entryPoint}] exit");
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? ex) //
        {
            string entryPoint = "OnDisconnected";

            var room = GetRoom();

            if (room?.Game == null || room.Observer == null)
            {
                log?.Warning($"[{entryPoint}] Room/Game/Observer was null");
                return;
            }

            using var logScope = BeginRoomLogScope(room);

            log?.Information($"[{entryPoint}] enter");

            BelotGame game = room.Game;
            var clients = Clients;

            string userId = GetCallerUserId();
            var user = room.GetUserById(userId);

            if (user == null)
            {
                log?.Warning($"[{entryPoint}] User was null");
                return;
            }

            await Task.Delay(1000); // allow for possible reconnect

            if (user.ConnectionId == Context.ConnectionId) // player has not reconnected, connectionId is stale
            {
                await UnbookSeat(room, clients, true); // this will mark them as disconnected
                room.RemoveUser(user);
                await UpdateConnectedUsers(room, clients);
                log?.Information($"[{entryPoint}] {userId} disconnected after delay.");
                if (room.Observer is LiveBelotObserver live)
                {
                    await live.SysAnnounce(user.Username + " disconnected.");
                    live.RoundSummaryGate.RegisterDisconnect(userId);

                    if (room.ConnectedUsers.Count == 0)
                    {
                        int oldwinnerDelay = game.WinnerDelay;
                        int oldBotDelay = game.BotDelay;
                        int oldRoundSummaryDelay = live.RoundSummaryGate.RoundSummaryDelay;
                        game.WinnerDelay = 0;
                        game.BotDelay = 0;
                        live.RoundSummaryGate.RoundSummaryDelay = 0;
                        await Task.Delay(1500); // let all bots play out remaining actions and a final wait for users to rejoin before deleting the room
                        game.WinnerDelay = oldwinnerDelay;
                        game.BotDelay = oldBotDelay;
                        live.RoundSummaryGate.RoundSummaryDelay = oldRoundSummaryDelay;

                        if (room.ConnectedUsers.Count == 0)
                        {
                            _roomRegistry.RemoveRoom(room.RoomId);
                            game.IsRunning = false;
                            game.CloseLog();
                        }
                    }
                }
            }
            else // player reconnected, don't proceed with disconnection
            {
                log?.Information($"[{entryPoint}] connection for user <{userId}> was superseded (reconnected or switched session)");
            }

            allConnections.TryRemove(Context.ConnectionId, out _);

            _roomRegistry.RefreshObserver(room.RoomId, Clients);

            log?.Information($"[{entryPoint}] exit");

            await base.OnDisconnectedAsync(ex);
        }

        #endregion
    }
}