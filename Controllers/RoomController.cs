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

            var player = room.GetPlayerById(userId);
            string[] titles = player == null ? ["N/S", "E/W"] : ["Us", "Them"];

            var ewFirst = room.Game?.Players[0]?.PlayerId == userId || room.Game?.Players[2]?.PlayerId == userId; // if user is not seated or is in seat 1 or 3, score order is NS/EW, else EW/NS

            return PartialView("_ScoreHistoryTable", (scoreHistory: room.Game?.ScoreHistory, titles, ewFirst));
        }
    }
}