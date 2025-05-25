namespace BelotWebApp.BelotClasses.Players
{
    public class Spectator
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }

        public Spectator(string username, string connectionId)
        {
            Username = username;
            ConnectionId = connectionId;
        }
    }
}
