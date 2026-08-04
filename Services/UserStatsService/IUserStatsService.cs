using BelotWebApp.Data;
using MatchType = BelotWebApp.Data.MatchType;

namespace BelotWebApp.Services.UserStatsService
{
    public interface IUserStatsService
    {
        Task RecordGameResult(string userId, MatchType matchType, bool won);
        //Task<UserStats?> GetStats(string userId, MatchType matchType, PeriodType periodType, string periodKey);
        //Task<List<UserStats>> GetLeaderboard(MatchType matchType, PeriodType periodType, string periodKey, int top = 50);
    }
}
