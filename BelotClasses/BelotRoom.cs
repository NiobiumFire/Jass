using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Players;
using BelotWebApp.Models;

namespace BelotWebApp.BelotClasses
{
    public class BelotRoom
    {
        public BelotRoom(string roomId, BelotGame game, IBelotObserver? observer, BelotRoomCreationOptions options)
        {
            RoomId = roomId;
            RoomName = options.RoomName;
            Spectators = [];
            Game = game;
            Observer = observer;
            Options = options;
        }

        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public List<Spectator> Spectators { get; set; }
        public BelotGame Game { get; set; }
        public IBelotObserver? Observer { get; set; }
        public BelotRoomCreationOptions Options { get; set; }
    }

}
