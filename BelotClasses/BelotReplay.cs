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
            line = line.Substring(s);
            string[] names = line.Split(new[] { "," }, StringSplitOptions.None);
            return names;
        }

        public static int GetDealer(string line)
        {
            int l = line.IndexOf("Dealer: ") + "Dealer: ".Length;
            line = line.Substring(l, 1);
            return int.Parse(line);
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

        public static int[] GetCalls(string line)
        {
            int l = line.IndexOf("Call: ") + "Call: ".Length;
            line = line.Substring(l);
            int[] calls = Array.ConvertAll(line.Split(','), c => int.Parse(c));
            return calls;
        }

        public static string[] BuildEmotes(int pos, int call)
        {
            string[] emotes = { "", "", "", "" };
            string[] calls = { "Pass", "Clubs", "Diamonds", "Hearts", "Spades", "No Trumps", "All Trumps", "Double!", "Redouble!", "⁹⁄₅" };
            emotes[pos] = calls[call];
            return emotes;
        }

        public static string[] GetPlays(string line)
        {
            int l = line.IndexOf("Play: ") + "Play: ".Length;
            line = line.Substring(l);
            string[] plays = line.Split(',');
            return plays;
        }

        public static int GetWinner(string line)
        {
            int l = line.IndexOf("Trick: ") + "Trick: ".Length;
            line = line.Substring(l, 1);
            return int.Parse(line);
        }

        public static int[] GetPoints(string line)
        {
            int l = line.IndexOf("Round: ") + "Round: ".Length;
            line = line.Substring(l);
            int[] points = Array.ConvertAll(line.Split(','), p => int.Parse(p));
            return points;
        }
    }

    public class BelotReplayState
    {
        //public BelotReplayState(BelotReplayState current = null, int round = -1, int trick = -1, int card = -1, int[] scores = null,
        //int roundSuit = -1, int caller = -1, int turn = -1, int call = -1, string[] emotes = null, string[] tableCards = null, string[][] hand = null)
        public BelotReplayState(int[] scores, int dealer,
        int roundSuit, int caller, int turn, string[] emotes, string[] tableCards, string[][] hand, bool showTrickWinner)
        {
            //Round = round;
            Scores = new int[] { scores[0], scores[1] };
            Dealer = dealer;
            RoundSuit = roundSuit;
            Caller = caller;
            Turn = turn;
            Emotes = new string[4];
            TableCards = new string[4];
            for (int i = 0; i < 4; i++)
            {
                Emotes[i] = emotes[i];
                TableCards[i] = tableCards[i];
            }
            Hand = new string[4][];
            for (int i = 0; i < 4; i++)
            {
                Hand[i] = new string[8];
                for (int j = 0; j < 8; j++)
                {
                    Hand[i][j] = hand[i][j];
                }
            }
            ShowTrickWinner = showTrickWinner;
        }
        public BelotReplayState(BelotReplayState current)
        {
            //Round = current.Round;
            Scores = new int[] { current.Scores[0], current.Scores[1] };
            Dealer = current.Dealer;
            RoundSuit = current.RoundSuit;
            Caller = current.Caller;
            Turn = current.Turn;
            Emotes = new string[4];
            TableCards = new string[4];
            for (int i = 0; i < 4; i++)
            {
                Emotes[i] = current.Emotes[i];
                TableCards[i] = current.TableCards[i];
            }
            Hand = new string[4][];
            for (int i = 0; i < 4; i++)
            {
                Hand[i] = new string[8];
                for (int j = 0; j < 8; j++)
                {
                    Hand[i][j] = current.Hand[i][j];
                }
            }
            ShowTrickWinner = current.ShowTrickWinner;
        }

        // Table stuff
        //public int Round { get; set; }
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