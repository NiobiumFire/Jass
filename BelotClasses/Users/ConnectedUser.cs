namespace BelotWebApp.BelotClasses.Users
{
    public class ConnectedUser
    {
        public ConnectedUser(string userId, string username, string connectionId)
        {
            UserId = userId;
            Username = username;
            ConnectionId = connectionId;
        }

        public string UserId { get; set; }
        public string Username { get; set; }
        public string ConnectionId { get; set; }
    }
}
