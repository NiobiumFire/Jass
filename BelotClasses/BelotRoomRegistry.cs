using BelotWebApp.BelotClasses.Observers;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoomRegistry
    {
        private readonly ConcurrentDictionary<string, BelotRoom> _rooms = new();

        public BelotRoom? GetRoom(string roomId)
        {
            if (roomId == null)
            {
                return null;
            }
            _rooms.TryGetValue(roomId, out var context);
            return context;
        }

        public void AddRoom(string roomId, BelotRoom context)
        {
            _rooms[roomId] = context;
        }

        public void RemoveRoom(string roomId)
        {
            _rooms.TryRemove(roomId, out _);
        }

        public void RefreshObserver(string roomId, IHubCallerClients newClients)
        {
            if (_rooms.TryGetValue(roomId, out var context) && context.Observer is LiveBelotObserver liveObserver)
            {
                liveObserver.UpdateClients(newClients);
            }
        }

        public IEnumerable<BelotRoomRecord> GetRoomRecords() => _rooms.Values
            .Select(r => new BelotRoomRecord(r.RoomId, r.Game.Players.Select(p => p.Username != "" ? p.Username : "<empty>").ToArray(), !r.Game.IsNewGame, r.Options.ScoreTarget, r.Options.AllowChat));
       
        public IEnumerable<BelotGame> GetAllGames() => _rooms.Values.Select(g => g.Game);

        public bool GamesOngoing() => !_rooms.IsEmpty;
    }
}
