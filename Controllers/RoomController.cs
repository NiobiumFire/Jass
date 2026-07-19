using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.ZipService;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var game = new BelotGame(_appPaths, _zipService, true, options.ScoreTarget);
            _roomRegistry.AddRoom(roomId, new(roomId, game, null, options));
            return RedirectToAction("Index", new { roomId });
        }

        // GET: Room - Join casual game
        [HttpGet("/Room/{roomId:guid}")]
        public ActionResult Index(string roomId)
        {
            var room = _roomRegistry.GetRoom(roomId);
            if (room == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["RoomId"] = roomId;
            ViewData["AllowChat"] = room.Options.AllowChat;
            ViewData["ScoreTarget"] = room.Options.ScoreTarget;
            return View("Room");
        }

        [HttpGet("/Room/PopulateScoreHistoryPartial")]
        public IActionResult PopulateScoreHistoryPartial(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var room = _roomRegistry.GetRoom(id);

            if (room == null)
            {
                return NotFound();
            }

            if (!room.ConnectedUsers.Any(u => u.UserId == userId))
            {
                return Unauthorized();
            }

            return PartialView("_ScoreHistoryTable", room.Game.ScoreHistory);
        }
    }
}