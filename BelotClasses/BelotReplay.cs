using BelotWebApp.BelotClasses;

namespace BelotWebApp.BelotClasses
{
    public class BelotReplay
    {
        public BelotReplay()
        {

        }
        // Room control
        //public bool PlayerIsActive { get; set; } = true;
        //public string ReplayId { get; set; }
        //public bool Paused { get; set; } = true;
        //public int Speed { get; set; } = 1;
        public string[] Players { get; set; } = new string[4];
        //public int CurrentState { get; set; }
        public List<BelotReplayState> States { get; set; } = new List<BelotReplayState>(); // per round per trick per card {turn, tablecard0 -> 4}

        public static string[] GetPlayers(string line)
        {
            int s = line.IndexOf("Players: ") + "Players: ".Length;
            string[] names = line.Substring(s).Split(new[] { "," }, StringSplitOptions.None);
            return names;
        }

        public static int GetPosition(string line, string type)
        {
            int l = line.IndexOf(type + ": ") + type.Length + ": ".Length;
            return int.Parse(line.Substring(l, 1));
        }

        public static int GetHandPos(string line)
        {
            int l = line.IndexOf("Hand ") + "Hand ".Length;
            line = line.Substring(l, 1);
            return int.Parse(line);
        }

        public static string[] GetHand(string line)
        {
            int l = line.IndexOf("Hand ") + "Hand ".Length + 3;
            line = line.Substring(l);
            string[] hand = { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" };
            string[] cards = line.Split(',');
            for (int i = 0; i < cards.Count(); i++)
            {
                hand[i] = cards[i];
            }
            return hand;
        }

        public static int[] GetIntArray(string line, string type)
        {
            int l = line.IndexOf(type + ": ") + type.Length + ": ".Length;
            int[] calls = Array.ConvertAll(line.Substring(l).Split(','), c => int.Parse(c));
            return calls;
        }

        public static string[] GetPlays(string line)
        {
            int l = line.IndexOf("Play: ") + "Play: ".Length;
            line = line.Substring(l);
            string[] plays = line.Split(',');
            return plays;
        }
    }

    public class BelotReplayState
    {
        public BelotReplayState()
        {
            Scores = new int[] { 0, 0 };
            Dealer = 4;
            RoundSuit = 0;
            Caller = 4;
            Turn = 4;
            Emotes = new string[] { "", "", "", "" };
            TableCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
            Hand = new string[][] { new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" }, new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" } };
            ShowTrickWinner = false;
        }
        public BelotReplayState(BelotReplayState current)
        {
            Scores = new int[] { current.Scores[0], current.Scores[1] };
            Dealer = current.Dealer;
            RoundSuit = current.RoundSuit;
            Caller = current.Caller;
            Turn = current.Turn;
            Emotes = new string[4];
            current.Emotes.CopyTo(Emotes, 0);
            TableCards = new string[4];
            current.TableCards.CopyTo(TableCards, 0);
            Hand = new string[4][];
            for(int i = 0; i < 4; i++)
            {
                Hand[i] = new string[8];
                current.Hand[i].CopyTo(Hand[i], 0);
            }
            ShowTrickWinner = current.ShowTrickWinner;
        }

        // Table stuff
        public int[] Scores { get; set; } // {NS, EW}
        public int Dealer { get; set; }
        public int RoundSuit { get; set; }
        public int Caller { get; set; }
        public int Turn { get; set; }
        // Player moves
        public string[] Emotes { get; set; } // WNES
        public string[] TableCards { get; set; } // WNES
        public string[][] Hand { get; set; } // [WNES][card]
        public bool ShowTrickWinner { get; set; }
    }
}