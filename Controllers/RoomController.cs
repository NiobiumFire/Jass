using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using BelotWebApp.Services;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.ZipService;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BelotWebApp.Controllers
{
    public class RoomController : Controller
    {
        private readonly IAppPaths _appPaths;
        private readonly IZipService _zipService;
        private readonly ReplayRecorderService _replayRecorderService;
        private readonly GameResultRecorder _gameResultRecorder;
        private readonly BelotRoomRegistry _roomRegistry;

        public RoomController(IAppPaths appPaths, IZipService zipService, ReplayRecorderService replayRecorderService, GameResultRecorder gameResultRecorder, BelotRoomRegistry roomRegistry)
        {
            _replayRecorderService = replayRecorderService;
            _appPaths = appPaths;
            _zipService = zipService;
            _gameResultRecorder = gameResultRecorder;
            _roomRegistry = roomRegistry;
        }

        // Create casual room
        [HttpPost]
        public IActionResult Create(BelotRoomCreationOptions options)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid options selected." });
            }

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is not string userId)
            {
                return BadRequest(new { error = "Unknown entity." });
            }

            options.RoomName = options.RoomName.Trim();
            options.MatchType = Data.MatchType.Casual;

            if (string.IsNullOrWhiteSpace(options.RoomName))
            {
                return BadRequest(new { error = "Please enter a room name." });
            }

            if (_roomRegistry.RoomNameExists(options.RoomName))
            {
                return Conflict(new { error = "A room with that name already exists." });
            }

            if (_roomRegistry.UserIsInAnyRoom(userId) || _roomRegistry.UserIsPlayerInRoom(userId))
            {
                return Conflict(new { error = "You are already in another room." });
            }

            string roomId = Guid.NewGuid().ToString();
            var game = new BelotGame(_replayRecorderService, true, options.ScoreTarget);
            _roomRegistry.AddRoom(roomId, new(roomId, game, null, options, _gameResultRecorder));
            return Ok(new { redirectUrl = Url.Action("Index", "Room", new { roomId }) });
        }

        // GET: Room - Join casual room from browser or redirect
        [HttpGet("/room/{roomId:guid}")]
        public ActionResult Index(string roomId)
        {
            var validation = ValidateRoomEntry(roomId);

            if (!validation.Success)
            {
                return RedirectToAction("Index", "Home");
            }

            var room = validation.Room!;

            ViewData["RoomId"] = roomId;
            return View("Room", room.Options);
        }

        // GET: Room - Join casual room from lobby modal
        // Ajax request from client lobby join button runs this validation and then redirects to Index action
        // Validation runs a second time, but advantage is any errors can be shown immediately in the lobby modal
        [HttpGet("/room/ValidateJoin/{roomId:guid}")]
        public IActionResult ValidateJoin(string roomId)
        {
            var validation = ValidateRoomEntry(roomId);

            if (!validation.Success)
            {
                return validation.Error!;
            }

            return Ok(new { redirectUrl = Url.Action("Index", "Room", new { roomId }) });
        }

        private RoomEntryValidation ValidateRoomEntry(string roomId)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is not string userId)
            {
                return new(BadRequest(new { error = "Unknown entity." }), null);
            }

            var room = _roomRegistry.GetRoom(roomId);
            if (room == null)
            {
                return new(NotFound(new { error = "The room no longer exists." }), null);
            }

            if (!room.ConnectedUsers.Any(u => u.UserId == userId) && !room.Game.Players.Any(u => u?.PlayerId == userId) && (_roomRegistry.UserIsInAnyRoom(userId) || _roomRegistry.UserIsPlayerInRoom(userId)))
            {
                return new(Conflict(new { error = "You are already in another room." }), null);
            }

            return new(null, room);
        }

        private record RoomEntryValidation(IActionResult? Error, BelotRoom? Room)
        {
            public bool Success => Error == null;
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