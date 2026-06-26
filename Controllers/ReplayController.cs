using BelotWebApp.BelotClasses;
using BelotWebApp.BelotClasses.Replays;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.ZipService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Player")]
    public class ReplayController : Controller
    {

        private readonly IAppPaths _appPaths;
        private readonly IZipService _zipService;

        public ReplayController(IAppPaths appPaths, IZipService zipService)
        {
            _appPaths = appPaths;
            _zipService = zipService;
        }

        public IActionResult Index()
        {
            return View("Replay");
        }

        public async Task<IActionResult> GetReplay(string replayId)
        {
            BelotReplay replay = new();

            string path = Path.Combine($"{_appPaths.LogFolder}", $"{replayId}.zip");

            string? log;
            try
            {
                log = await _zipService.ReadTextAsync(path);

                if (log == null)
                {
                    return NoContent();
                }

                string[] lines = log.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (string line in lines)
                {
                    BelotReplayDiff? diff = JsonSerializer.Deserialize<BelotReplayDiff>(line);

                    if (diff == null)
                    {
                        return NoContent();
                    }

                    replay.StateChanges.Add(diff);
                }
            }
            catch (Exception e)
            {
                return NoContent();
            }

            return Json(new { Replay = replay, ViewerName = User.Identity.Name });
        }

        public IActionResult PopulateReplaysPartial()
        {
            List<ReplayTableRow> replays = [];

            string[] allLogs = new DirectoryInfo(_appPaths.LogFolder).GetFiles("*.zip")
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .ToArray();
            foreach (string log in allLogs)
            {
                try
                {
                    var line = _zipService.ReadLines(log).FirstOrDefault();
                    if (line == null)
                    {
                        continue;
                    }

                    var diff = JsonSerializer.Deserialize<BelotReplayDiff>(line);
                    if (diff != null && diff.After.Players.Contains(User.Identity.Name))
                    {
                        string[] names = diff.After.Players;
                        string creation = System.IO.File.GetCreationTime(log).ToString("yyyy-MM-dd HH:mm");
                        string id = Path.GetFileNameWithoutExtension(log);
                        replays.Add(new ReplayTableRow(id, creation, names[0], names[1], names[2], names[3]));
                    }
                }
                catch (Exception e)
                {

                }
            }
            
            return PartialView("_ReplaysTableRows", replays);
        }
    }
}
