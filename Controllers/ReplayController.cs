using BelotWebApp.Services.AppPathService;
using BelotWebApp.BelotClasses.Replays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BelotWebApp.BelotClasses;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Player")]
    public class ReplayController : Controller
    {

        private readonly IAppPaths _appPaths;

        public ReplayController(IAppPaths appPaths)
        {
            _appPaths = appPaths;

        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult? GetReplay(string replayId)
        {
            BelotReplay replay = new();

            string path = Path.Combine($"{_appPaths.LogFolder}", replayId + ".txt");

            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    BelotReplayDiff diff = JsonSerializer.Deserialize<BelotReplayDiff>(line);
                    replay.StateChanges.Add(diff);
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return Json(new { Replay = replay, ViewerName = User.Identity.Name });
        }

        public IActionResult PopulateReplaysPartial()
        {
            List<ReplayTableRow> replays = [];

            string[] allLogs = new DirectoryInfo(_appPaths.LogFolder).GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .Where(f => !f.Contains("BelotServerLog"))
                .ToArray();
            foreach (string log in allLogs)
            {
                try
                {
                    string line = System.IO.File.ReadLines(log).First();
                    var diff = JsonSerializer.Deserialize<BelotReplayDiff>(line);
                    if (diff != null & diff.After.Players.Contains(User.Identity.Name))
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
