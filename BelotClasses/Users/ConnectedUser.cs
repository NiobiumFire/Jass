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

        public string UserId { get; }
        public string Username { get; private set; }
        public string ConnectionId { get; private set; }

        internal void Update(string username, string connectionId)
        {
            Username = username;
            ConnectionId = connectionId;
        }
    }
}
