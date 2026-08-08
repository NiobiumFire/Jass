using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Users;
using BelotWebApp.Models;
using BelotWebApp.Services;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoom
    {
        private readonly GameResultRecorder _gameResultRecorder;
        private readonly List<ConnectedUser> _connectedUsers = [];
        private int _engineIsMarked; // 0 = not marked, 1 = marked

        public BelotRoom(string roomId, BelotGame game, IBelotObserver? observer, BelotRoomCreationOptions options, GameResultRecorder gameResultRecorder)
        {
            RoomId = roomId;
            RoomName = options.RoomName;
            Game = game;
            Observer = observer;
            Options = options;
            _gameResultRecorder = gameResultRecorder;
            LastActivityTime = DateTime.UtcNow;
            RoomCloseWarningIssued = null;
        }

        public string RoomId { get; private set; }
        public string RoomName { get; private set; }
        public BelotGame Game { get; set; }
        public IBelotObserver? Observer { get; set; }
        public BelotRoomCreationOptions Options { get; private set; }

        public IReadOnlyList<ConnectedUser> ConnectedUsers => _connectedUsers;

        public DateTime LastActivityTime { get; private set; }
        public DateTime? RoomCloseWarningIssued { get; private set; }

        #region Users

        public ConnectedUser AddUser(string userId, string username, string connectionId)
        {
            ConnectedUser user = new(userId, username, connectionId);
            _connectedUsers.Add(user);
            return user;
        }

        public ConnectedUser UpdateUser(ConnectedUser user, string username, string connectionId)
        {
            user.Update(username, connectionId);
            return user;
        }

        public void RemoveUser(ConnectedUser user)
        {
            _connectedUsers.Remove(user);
        }

        public ConnectedUser? GetUserById(string userId)
        {
            return ConnectedUsers.FirstOrDefault(u => u.UserId == userId);
        }

        public ConnectedUser? GetUserBySeat(int position)
        {
            return ConnectedUsers.FirstOrDefault(p => p.UserId == Game.Players[position]?.PlayerId);
        }

        public Player? GetPlayerById(string userId)
        {
            return Game.Players.FirstOrDefault(u => u?.PlayerId == userId);
        }

        public string GetDisplayName(int position)
        {
            var player = Game?.Players[position];
            if (player != null)
            {
                return player.PlayerName;
            }
            return "Unknown Entity";
        }

        public (ConnectedUser[] spectators, ConnectedUser[] players) GetSpectatorsAndConnectedHumanPlayers()
        {
            // Create a lookup (~dictionary) of connected human players keyed by UserId. connectedHumanPlayers[u.UserId].Any() is true if there is at least one entry in the connectedHumanPlayers[u.UserId] collection
            var connectedHumanPlayers = Game.Players.Where(p => p != null && p.PlayerType == PlayerType.Human && !p.IsDisconnected).ToLookup(p => p!.PlayerId);
            // Create simple object for each user: { username, matching player? }. ToArray evaluates the collection so it can be reused
            var connectedUsers = ConnectedUsers.Select(u => new { ConnectedUser = u, IsPlayer = connectedHumanPlayers[u.UserId].Any() }).ToArray();

            var spectators = connectedUsers.Where(u => !u.IsPlayer).Select(u => u.ConnectedUser).ToArray();
            var players = connectedUsers.Where(u => u.IsPlayer).Select(u => u.ConnectedUser).ToArray();

            return (spectators, players);
        }

        #endregion

        #region Idle Room

        public void UpdateLastActivityTime()
        {
            LastActivityTime = DateTime.UtcNow;
            RoomCloseWarningIssued = null;
        }

        public async Task IssueImminentClosureWarningAsync(TimeSpan cutoff)
        {
            RoomCloseWarningIssued = DateTime.UtcNow;
            if (Observer is LiveBelotObserver live)
            {
                await live.IssueImminentClosureWarning(cutoff);
            }
        }

        public async Task CloseIdleRoomAsync()
        {
            if (Observer is LiveBelotObserver live)
            {
                await live.CloseIdleRoomAsync();
            }
        }

        #endregion

        public bool TryMarkEngine() // check out the engine and prevent any other code path from concurrently starting another engine
        {
             return Interlocked.Exchange(ref _engineIsMarked, 1) == 0;
        }

        public void UnmarkEngine()
        {
            Interlocked.Exchange(ref _engineIsMarked, 0);
        }

        public async Task RecordGameResult(List<(string PlayerId, bool Won)> result)
        {
            await _gameResultRecorder.RecordGameResult(result, Options.MatchType);
        }
    }

}
