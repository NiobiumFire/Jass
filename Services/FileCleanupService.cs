using BelotWebApp.Services.AppPathService;

namespace BelotWebApp.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly ILogger<FileCleanupService> _logger;
        private readonly IConfiguration _config;
        private readonly IAppPaths _paths;

        public FileCleanupService(ILogger<FileCleanupService> logger, IConfiguration config, IAppPaths paths)
        {
            _logger = logger;
            _config = config;
            _paths = paths;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                RunCleanup(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Startup file cleanup run failed");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextRunUtc();
                _logger.LogInformation("Next file cleanup scheduled in {Delay}", delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // shutting down
                }

                try
                {
                    RunCleanup(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "File cleanup run failed");
                }
            }
        }

        private TimeSpan GetDelayUntilNextRunUtc()
        {
            var runAt = TimeSpan.Parse(_config["Cleanup:RunAtUtc"] ?? "10:00");
            var now = DateTime.UtcNow;
            var nextRun = DateTime.UtcNow.Date.Add(runAt);

            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            return nextRun - now;
        }

        private void RunCleanup(CancellationToken stoppingToken)
        {
            var jobs = _config.GetSection("Cleanup:Jobs").Get<List<CleanupJob>>() ?? [];

            foreach (var job in jobs)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var path = ResolvePath(job);

                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    _logger.LogWarning("Cleanup path does not exist: {Path} (key: {Key})", path, job.PathKey);
                    continue;
                }

                var cutoff = DateTime.UtcNow - TimeSpan.FromDays(job.MaxAgeDays);
                var pattern = string.IsNullOrEmpty(job.SearchPattern) ? "*" : job.SearchPattern;

                var files = Directory.GetFiles(path, pattern,
                    job.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                int deleted = 0;
                foreach (var file in files)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                    {
                        try
                        {
                            File.Delete(file);
                            deleted++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete {File}", file);
                        }
                    }
                }

                _logger.LogInformation("Cleanup job {PathKey}: deleted {Count} file(s) older than {Days}d with file type {Pattern}", job.PathKey ?? path, deleted, job.MaxAgeDays, pattern);
            }
        }

        private string? ResolvePath(CleanupJob job)
        {
            // Named folder from IAppPaths takes priority; falls back to a raw path if given
            if (!string.IsNullOrEmpty(job.PathKey))
            {
                return job.PathKey switch
                {
                    "Data" => _paths.DataFolder,
                    "HubLog" => _paths.HubLogFolder,
                    "CleanupLog" => _paths.CleanupLogFolder,
                    "Replay" => _paths.ReplayFolder,
                    "IncompleteGame" => _paths.IncompleteGameFolder,
                    _ => null
                };
            }

            return job.Path;
        }

        private class CleanupJob
        {
            public string? PathKey { get; set; }      // e.g. "IncompleteGames" - resolved via IAppPaths
            public string? Path { get; set; }         // raw path override, used if PathKey not set
            public string? SearchPattern { get; set; }
            public int MaxAgeDays { get; set; } = 7;
            public bool Recursive { get; set; } = false;
        }
    }
}
