using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BelotWebApp
{
    public class BelotReplay
    {
        public BelotReplay(string replayId)
        {
            ReplayId = replayId;
        }

        public bool PlayerIsActive { get; set; } = true;
        public string ReplayId { get; set; }
        public bool Paused { get; set; } = false;
        public int Speed { get; set; } = 1;
        public Player[] Players { get; set; }
        public bool CurrentlyCalling { get; set; } = true;
        public int CurrentRound { get; set; } = 0;
        public int CurrentCall { get; set; } = 0;
        public int CurrentTrick { get; set; } = 0;
        public int CurrentCard { get; set; } = 0;
        public List<int[]> Calls { get; set; }
        public List<BelotReplayState[,]> State { get; set; } // per round per trick per card {turn, tablecard0 -> 4}
        public List<int[]> Scores { get; set; } // per round, {NS, EW}
        public List<int[]> TrickSuit { get; set; } // per round per trick
        public List<int[]> Caller { get; set; } // per round per trick

    }

    public class BelotReplayState
    {
        public BelotReplayState()
        {

        }
        public string[] TableCards { get; set; }
        public int Turn { get; set; }
        
    }
}