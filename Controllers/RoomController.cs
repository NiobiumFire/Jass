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
        private readonly BelotGameRegistry _gameRegistry;

        public RoomController(IConfiguration config, BelotGameRegistry gameRegistry)
        {
            _config = config;
            _gameRegistry = gameRegistry;
        }

        [HttpPost]
        public ActionResult Index(BelotRoomCreator creator)
        {
            if (creator != null)
            {
                string id = Guid.NewGuid().ToString();
                _gameRegistry.AddGame(id, new BelotGame([new(), new(), new(), new()], id, _config.GetSection("SerilogPath:Path").Value));
                return RedirectToAction("Index", new { id });
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: Room
        public ActionResult Index(string id)
        {
            var game = _gameRegistry.GetGame(id);
            if (game != null)
            {
                ViewData["roomId"] = id;
                return View();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}