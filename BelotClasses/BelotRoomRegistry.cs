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
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public void AddRoom(string roomId, BelotRoom room)
        {
            _rooms[roomId] = room;
        }

        public void RemoveRoom(string roomId)
        {
            _rooms.TryRemove(roomId, out _);
        }

        public bool RoomNameExists(string roomName)
        {
            return _rooms.Values.Any(r => string.Equals(r.Options.RoomName, roomName, StringComparison.OrdinalIgnoreCase));
        }

        public bool UserIsInAnyRoom(string userId)
        {
            return _rooms.Values.SelectMany(r => r.ConnectedUsers).Any(p => p?.UserId == userId);
        }

        public bool UserIsPlayerInRoom(string userId)
        {
            return _rooms.Values.SelectMany(r => r.Game.Players).Any(p => p?.PlayerId == userId);
        }

        public void RefreshObserver(string roomId, IHubCallerClients newClients)
        {
            if (_rooms.TryGetValue(roomId, out var room) && room.Observer is LiveBelotObserver liveObserver)
            {
                liveObserver.UpdateClients(newClients);
            }
        }

        public List<BelotRoom> GetRooms()
        {
            return _rooms.Values.ToList(); // ToList for snapshot of rooms
        }

        // Players are always stored in seating order: West, North, East, South
        // GetDisplayName expects the player index, so preserve this ordering
        public IEnumerable<BelotRoomRecord> GetRoomRecords() => _rooms.Values
            .Select(r => new BelotRoomRecord(r.RoomId,
                r.RoomName,
                r.Game.Players.Select(p => p == null ? "<empty>" : p.PlayerName).ToArray(),
                !r.Game.IsNewGame,
                r.Options.ScoreTarget,
                r.Options.TurnTime,
                r.Options.AllowChat));

        public IEnumerable<string> GetAllConnectedUsers() => _rooms.Values.SelectMany(r => r.ConnectedUsers).Select(u => u.Username);
    }
}
