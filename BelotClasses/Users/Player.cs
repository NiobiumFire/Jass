using BelotWebApp.BelotClasses.Agents;

namespace BelotWebApp.BelotClasses.Users
{
    public class Player
    {
        public static readonly string _botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";

        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public PlayerType PlayerType { get; set; }
        public bool IsDisconnected { get; set; }
        public AgentAdvanced? Agent { get; set; }

        public Player(int position)
        {
            PlayerId = _botGUID;
            PlayerType = PlayerType.Basic;
            PlayerName = GetBotName(position);
        }

        public Player(string userId, string username, PlayerType playerType, BelotGame? game = null)
        {
            PlayerId = userId;
            PlayerName = username;
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

        private static string GetBotName(int position)
        {
            string[] seat = ["West", "North", "East", "South"];
            return $"Robot {seat[position]}";
        }
    }
}
