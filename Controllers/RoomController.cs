using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BelotWebApp.Controllers
{

    [Authorize(Roles = "Player")]
    public class RoomController : Controller
    {
        private readonly IConfiguration _config;

        public RoomController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public ActionResult Index(BelotRoomCreator creator)
        {
            if (creator != null)
            {
                string id = Guid.NewGuid().ToString();
                BelotRoom.games.Add(new BelotGame([new(), new(), new(), new()], id, _config.GetSection("SerilogPath:Path").Value));
                //BelotRoom.log.Information("Creating new room. Redirecting to room " + id);
                return RedirectToAction("Index", new { id });
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: Room
        public ActionResult Index(string id)
        {
            if (BelotRoom.games.Any(g => g.RoomId == id))
            {
                //BelotRoom.log.Information("Entering room: " + id);
                ViewData["roomId"] = id;
                return View();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}