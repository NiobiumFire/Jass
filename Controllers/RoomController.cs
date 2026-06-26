using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.ZipService;
using Microsoft.AspNetCore.Mvc;

namespace BelotWebApp.Controllers
{
    //[Authorize(Roles = "Player")]
    public class RoomController : Controller
    {
        private readonly IAppPaths _appPaths;
        private readonly IZipService _zipService;
        private readonly BelotGameRegistry _gameRegistry;

        public RoomController(IAppPaths appPaths, IZipService zipService, BelotGameRegistry gameRegistry)
        {
            _appPaths = appPaths;
            _zipService = zipService;
            _gameRegistry = gameRegistry;
        }

        // Create casual game then join it
        [HttpPost]
        public ActionResult Index(BelotRoomCreator creator)
        {
            string id = Guid.NewGuid().ToString();
            var game = new BelotGame([new(), new(), new(), new()], id, _appPaths, _zipService, true);
            _gameRegistry.AddContext(id, new(game, null));
            return RedirectToAction("Index", new { id });
        }

        // GET: Room - Join casual game
        [HttpGet("/Room/{id}")]
        public ActionResult Index(string id)
        {
            var gameContext = _gameRegistry.GetContext(id);
            if (gameContext == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["roomId"] = id;
            return View("Room");
        }

        public IActionResult PopulateScoreHistoryPartial(string id)
        {
            var username = User?.Identity?.Name;

            if (username == null)
            {
                return Unauthorized();
            }

            var gameContext = _gameRegistry.GetContext(id);

            if (gameContext == null)
            {
                return NotFound();
            }

            var game = gameContext.Game;

            if (!game.Players.Any(p => p.Username != username) && !game.Spectators.Any(p => p.Username != username))
            {
                return Unauthorized();
            }

            return PartialView("_ScoreHistoryTable", game.ScoreHistory);
        }
    }
}