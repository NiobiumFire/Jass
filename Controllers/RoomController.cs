using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Serilog;

namespace BelotWebApp.Controllers
{

    [Authorize]
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
                BelotRoom.games.Add(new BelotGame(new Player[] { new Player(), new Player(), new Player(), new Player() }, id, _config.GetSection("SerilogPath:Path").Value));
                //BelotRoom.log.Information("Creating new room. Redirecting to room " + id);
                return RedirectToAction("Index", new { id });
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: Room
        public ActionResult Index(string id)
        {
            if (BelotRoom.games.Where(g => g.RoomId == id).Count() > 0)
            {
                //BelotRoom.log.Information("Entering room: " + id);
                ViewData["roomId"] = id;
                return View();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}