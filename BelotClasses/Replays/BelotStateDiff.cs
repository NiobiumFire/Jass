using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Replays
{
    public class BelotStateDiff
    {
        public string[]? Players { get; set; }
        public int[]? Scores { get; set; }
        public int? Dealer { get; set; }
        public Call? RoundCall { get; set; }
        public int? Caller { get; set; }
        public int? Turn { get; set; }

        public List<ReplayEmote> Emotes { get; set; }
        public List<ReplayTableCard> TableCards { get; set; } // null card means blank table card
        public List<ReplayHandCard> HandCards { get; set; } // null card means played or empty to be hidden in js
    }
}