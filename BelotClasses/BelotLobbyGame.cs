namespace BelotWebApp.BelotClasses
{
    public class BelotLobbyGame
    {
        public BelotLobbyGame(BelotGame g)
        {
            West = g.Players[0].Username != "" ? g.GetDisplayName(0) : "Empty";
            North = g.Players[1].Username != "" ? g.GetDisplayName(1) : "Empty";
            East = g.Players[2].Username != "" ? g.GetDisplayName(2) : "Empty";
            South = g.Players[3].Username != "" ? g.GetDisplayName(3) : "Empty";
            Started = !g.IsNewGame;
            RoomId = g.RoomId;
        }
        public string West { get; set; }
        public string North { get; set; }
        public string East { get; set; }
        public string South { get; set; }
        public bool Started { get; set; }
        public string RoomId { get; set; }
    }
}
