using BelotWebApp.Services.UserStatsService;

namespace BelotWebApp.Services
{
    public class GameResultRecorder
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public GameResultRecorder(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public async Task RecordGameResult(List<(string, bool)> result, Data.MatchType matchType)
        {
            using var scope = _scopeFactory.CreateScope();

            var statsService = scope.ServiceProvider.GetRequiredService<IUserStatsService>();

            foreach (var (userId, won) in result)
            {
                await statsService.RecordGameResult(userId, matchType, won);
            }
        }
    }
}
