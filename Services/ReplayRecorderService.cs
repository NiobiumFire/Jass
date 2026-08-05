using BelotWebApp.BelotClasses.Replays;
using BelotWebApp.BelotClasses.Users;
using BelotWebApp.Configuration;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.ZipService;
using System.Text.Json;

namespace BelotWebApp.Services
{
    public class ReplayRecorderService
    {
        private readonly IAppPaths _appPaths;
        private readonly IZipService _zipService;
        private readonly ILogger _logger;

        public ReplayRecorderService(IAppPaths appPaths, IZipService zipService, ILogger<ReplayRecorderService> logger)
        {
            _appPaths = appPaths;
            _zipService = zipService;
            _logger = logger;
        }

        public void CreateReplay(string gameId)
        {
            var logPath = Path.Combine(_appPaths.ReplayFolder, gameId + ".txt");

            if (File.Exists(logPath))
            {
                _logger.LogError("Failed to create replay file - {LogPath} already exists", logPath);
                return;
            }

            File.Create(logPath).Close();
        }

        public void RecordInitialReplayFrame(string gameId, Player?[] players, BelotStateDiff replayState)
        {
            if (players.Any(p => p == null))
            {
                _logger.LogError("Game {GameId} has null players", gameId);
                return;
            }

            var logPath = Path.Combine(_appPaths.ReplayFolder, gameId + ".txt");

            if (!File.Exists(logPath))
            {
                _logger.LogError("Failed to write initial replay state - {LogPath} doesn't exist", logPath);
                return;
            }

            try
            {
                File.AppendAllText(logPath, JsonSerializer.Serialize(new BelotReplayDiff
                {
                    Before = new()
                    {
                        Players = players.Select(p => p!.PlayerId).ToArray(), // for lookup of replays by user id
                    },
                    After = replayState
                }, JsonSettings.Compact) + "\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write initial replay state for {LogPath}", logPath);
            }

        }

        public void RecordReplayFrame(string gameId, bool gameIsRunning, BelotReplayDiff diff)
        {
            if (!gameIsRunning)
            {
                _logger.LogWarning("Game {GameId} is not running", gameId);
                return;
            }

            var logPath = Path.Combine(_appPaths.ReplayFolder, gameId + ".txt");

            if (!File.Exists(logPath))
            {
                _logger.LogError("Failed to append replay state - {LogPath} doesn't exist", logPath);
                return;
            }

            try
            {
                File.AppendAllText(logPath, JsonSerializer.Serialize(diff, JsonSettings.Compact) + "\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append replay state for {LogPath}", logPath);
            }
        }

        public void FinaliseReplay(string gameId, bool gameCompletedNormally)
        {
            var source = Path.Combine(_appPaths.ReplayFolder, $"{gameId}.txt");

            if (!File.Exists(source))
            {
                _logger.LogError("Failed to finalise replay - {Source} doesn't exist", source);
                return;
            }

            if (!gameCompletedNormally) // e.g. all users left the room early
            {
                var destination = Path.Combine(_appPaths.IncompleteGameFolder, $"{gameId}.txt");

                if (File.Exists(destination))
                {
                    _logger.LogError("Replay file {Destination} already exists", destination);
                    return;
                }

                try
                {
                    File.Move(source, destination);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to move incomplete replay for {GameID}", gameId);
                    return;
                }
            }
            else
            {
                string zip = Path.Combine(_appPaths.ReplayFolder, $"{gameId}.zip");
                if (File.Exists(zip))
                {
                    _logger.LogError("Replay file {Zip} already exists", zip);
                    return;
                }

                try
                {
                    _zipService.Zip(source, zip, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to compress and move replay {Source}", source);
                    return;
                }
            }
        }
    }
}
