using BelotWebApp.BelotClasses.Agents;

namespace BelotWebApp.BelotClasses.Players
{
    public class Player
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public bool IsHuman { get; set; }
        public bool IsDisconnected { get; set; }
        public AgentAdvanced Agent { get; set; }

        public Player()
        {
            Username = "";
            ConnectionId = "";
            IsHuman = false;
        }

        public Player(string username, string connectionId, bool isHuman)
        {
            Username = username;
            ConnectionId = connectionId;
            IsHuman = isHuman;
        }
    }
}
