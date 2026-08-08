using Microsoft.Extensions.Options;

namespace BelotWebApp.BelotClasses.IdleRoomHandling
{
    public class IdleRoomClosureService : BackgroundService
    {
        private readonly BelotRoomRegistry _registry;
        private readonly ILogger<IdleRoomClosureService> _logger;
        private readonly IdleRoomClosureOptions _options;

        public IdleRoomClosureService(BelotRoomRegistry registry, ILogger<IdleRoomClosureService> logger, IOptions<IdleRoomClosureOptions> options)
        {
            _registry = registry;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_options.ScanInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ScanAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Idle room closure scan failed");
                }
            }
        }

        private async Task ScanAsync(CancellationToken stoppingToken)
        {
            var now = DateTime.UtcNow;

            foreach (var room in _registry.GetRooms())
            {
                var idleFor = now - room.LastActivityTime;

                if (room.RoomCloseWarningIssued is null && idleFor >= _options.IdleTimeLimit)
                {
                    await IssueImminentClosureWarningAsync(room);
                    continue;
                }

                if (room.RoomCloseWarningIssued is DateTime warnedAt && now - warnedAt >= _options.GracePeriodAfterWarning)
                {
                    await CloseIdleRoomAsync(room);
                }
            }
        }

        private async Task IssueImminentClosureWarningAsync(BelotRoom room)
        {
            await room.IssueImminentClosureWarningAsync(_options.GracePeriodAfterWarning);

            _logger.LogInformation("Room {RoomId} idle since {LastActivity}, issuing closure warning", room.RoomId, room.LastActivityTime);
        }

        private async Task CloseIdleRoomAsync(BelotRoom room)
        {
            _logger.LogInformation("Closing idle room {RoomId}", room.RoomId);

            await room.CloseIdleRoomAsync();

            if (!room.Game.IsNewGame)
            {
                room.Game.IsRunning = false;
                room.Game.FinaliseReplay();
            }
            _registry.RemoveRoom(room.RoomId);
        }
    }
}