using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Users;
using BelotWebApp.Models;
using System.Xml.Linq;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoom
    {
        public BelotRoom(string roomId, BelotGame game, IBelotObserver? observer, BelotRoomCreationOptions options)
        {
            RoomId = roomId;
            RoomName = options.RoomName;
            ConnectedUsers = [];
            Game = game;
            Observer = observer;
            Options = options;
        }

        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public BelotGame Game { get; set; }
        public IBelotObserver? Observer { get; set; }
        public BelotRoomCreationOptions Options { get; set; }
        public List<ConnectedUser> ConnectedUsers { get; } = [];

        public ConnectedUser AddUser(string userId, string username, string connectionId)
        {
            ConnectedUser user = new(userId, username, connectionId);
            ConnectedUsers.Add(user);
            return user;
        }

        public void RemoveUser(ConnectedUser user)
        {
            ConnectedUsers.Remove(user);
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
    }

}
