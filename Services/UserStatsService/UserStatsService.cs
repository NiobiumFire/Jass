using BelotWebApp.Data;
using Microsoft.EntityFrameworkCore;
using MatchType = BelotWebApp.Data.MatchType;

namespace BelotWebApp.Services.UserStatsService
{
    public class UserStatsService : IUserStatsService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UserStatsService> _logger;

        public UserStatsService(AuthDbContext context, ILogger<UserStatsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RecordGameResult(string userId, MatchType matchType, bool won)
        {
            try
            {
                var periodsToUpdate = GetRelevantPeriods(matchType);

                // NOTE: No concurrency guard on this read-modify-write. Two overlapping calls for the same user/period could race and lose an increment (last write wins)
                // fix at scale: either add a lock (SemaphoreSlim) for single-instance server, or
                // switch to an atomic DB update (ExecuteUpdateAsync) e.g. SET GamesTotal = GamesTotal + 1, works across multiple instances
                foreach (var (periodType, periodKey) in periodsToUpdate)
                {
                    var stats = await _context.UserStats.FirstOrDefaultAsync(s =>
                        s.UserId == userId &&
                        s.MatchType == matchType &&
                        s.PeriodType == periodType &&
                        s.PeriodKey == periodKey);

                    if (stats is null)
                    {
                        stats = new UserStats
                        {
                            UserId = userId,
                            MatchType = matchType,
                            PeriodType = periodType,
                            PeriodKey = periodKey,
                            GamesTotal = 0,
                            GamesWon = 0
                        };
                        _context.UserStats.Add(stats);
                    }

                    stats.GamesTotal++;
                    if (won)
                    {
                        stats.GamesWon++;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record game result: UserId={UserId}, MatchType={MatchType}", userId, matchType);
                throw; // LiveBelotOberver handles the higher-level exception
            }
        }

        private static List<(PeriodType, string)> GetRelevantPeriods(MatchType matchType)
        {
            List<(PeriodType, string)> periods = [(PeriodType.Lifetime, "all")];

            if (matchType == MatchType.Ranked)
            {
                periods.Add((PeriodType.Monthly, CurrentMonthKey()));
                periods.Add((PeriodType.Biweekly, CurrentBiweeklyKey()));
            }

            return periods;
        }

        private static string CurrentMonthKey() => DateTime.UtcNow.ToString("yyyy-MM");

        private static string CurrentBiweeklyKey()
        {
            // Fixed anchor so periods never drift regardless of when this code runs
            var anchor = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc); // pick any Monday
            var daysSinceAnchor = (DateTime.UtcNow.Date - anchor.Date).Days;
            var periodIndex = (int)Math.Floor(daysSinceAnchor / 14.0);
            var periodStart = anchor.AddDays(periodIndex * 14);
            return periodStart.ToString("yyyy-MM-dd");
        }
    }
}
