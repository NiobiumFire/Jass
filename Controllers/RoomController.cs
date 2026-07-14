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
        private readonly BelotRoomRegistry _roomRegistry;

        public RoomController(IAppPaths appPaths, IZipService zipService, BelotRoomRegistry gameRegistry)
        {
            _appPaths = appPaths;
            _zipService = zipService;
            _roomRegistry = gameRegistry;
        }

        // Create casual game then join it
        [HttpPost]
        public ActionResult Create(BelotRoomCreationOptions options)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            string roomId = Guid.NewGuid().ToString();
            var game = new BelotGame([new(), new(), new(), new()], _appPaths, _zipService, true, options.ScoreTarget);
            _roomRegistry.AddRoom(roomId, new(roomId, game, null, options));
            return RedirectToAction("Index", new { roomId });
        }

        // GET: Room - Join casual game
        [HttpGet("/Room/{roomId:guid}")]
        public ActionResult Index(string roomId)
        {
            var gameContext = _roomRegistry.GetRoom(roomId);
            if (gameContext == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["roomId"] = roomId;
            return View("Room");
        }

        [HttpGet("/Room/PopulateScoreHistoryPartial")]
        public IActionResult PopulateScoreHistoryPartial(string id)
        {
            var username = User?.Identity?.Name;

            if (username == null)
            {
                return Unauthorized();
            }

            var gameContext = _roomRegistry.GetRoom(id);

            if (gameContext == null)
            {
                return NotFound();
            }

            var game = gameContext.Game;

            if (!game.Players.Any(p => p.Username == username) && !game.Spectators.Any(p => p.Username == username))
            {
                return Unauthorized();
            }

            return PartialView("_ScoreHistoryTable", game.ScoreHistory);
        }
    }
}