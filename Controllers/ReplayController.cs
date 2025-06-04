using BelotWebApp.BelotClasses.Replays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Player")]
    public class ReplayController : Controller
    {

        private readonly IConfiguration _config;

        public ReplayController(IConfiguration config)
        {
            _config = config;

        }

        public IActionResult Index()
        {
            return View();
        }

        public BelotReplay? GetReplay(string replayId)
        {
            BelotReplay replay = new();

            string path = _config.GetSection("SerilogPath:Path").Value + replayId + ".txt";

            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                return null;
            }
            foreach (string line in lines)
            {
                BelotReplayDiff diff = JsonSerializer.Deserialize<BelotReplayDiff>(line);
                replay.StateChanges.Add(diff);
            }

            return replay;
        }

        public string GetMyReplays()
        {
            List<string[]> myReplays = [];
            string[] allFiles = new DirectoryInfo(_config.GetSection("SerilogPath:Path").Value).GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .Where(f => !f.Contains("BelotServerLog"))
                .ToArray();
            foreach (string log in allFiles)
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
                        myReplays.Add([names[0], names[1], names[2], names[3], creation, id]);
                    }
                }
                catch (Exception e)
                {

                }
            }
            return JsonSerializer.Serialize(myReplays);
        }
    }
}
