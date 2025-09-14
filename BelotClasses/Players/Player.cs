using BelotWebApp.BelotClasses.Agents;

namespace BelotWebApp.BelotClasses.Players
{
    public class Player
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public PlayerType PlayerType { get; set; }
        public bool IsDisconnected { get; set; }
        public AgentAdvanced Agent { get; set; }

        public Player()
        {
            Username = "";
            ConnectionId = "";
            PlayerType = PlayerType.Basic;
        }

        public Player(string username, string connectionId, PlayerType playerType, BelotGame? game = null)
        {
            Username = username;
            ConnectionId = connectionId;
            PlayerType = playerType;

            //if (playerType == PlayerType.Advanced && game != null)
            //{
            //    int inputs = AgentAdvanced.BuildNNInputVector(game).Length;
            //    Agent = new(inputs, 128, 8);
            //}
            //else
            //{
            //    PlayerType = PlayerType.Basic;
            //}
        }
    }
}
