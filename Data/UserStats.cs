namespace BelotWebApp.Data
{
    public class UserStats
    {
        public int Id { get; set; } // surrogate PK

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public MatchType MatchType { get; set; }   // "ranked" | "casual"
        public PeriodType PeriodType { get; set; }  // "lifetime" | "monthly" | "biweekly"
        public string PeriodKey { get; set; } = null!;    // "all" | "2026-08" | "2026-07-28"

        public int GamesTotal { get; set; }
        public int GamesWon { get; set; }

        public int? Rating { get; set; }
        public int? RatingDeviation { get; set; }
    }
}
