using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using BelotWebApp.Services.AppPathService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BelotWebApp.Controllers
{

    [Authorize(Roles = "Player")]
    public class RoomController : Controller
    {
        private readonly IAppPaths _appPaths;
        private readonly BelotGameRegistry _gameRegistry;

        public RoomController(IAppPaths appPaths, BelotGameRegistry gameRegistry)
        {
            _appPaths = appPaths;
            _gameRegistry = gameRegistry;
        }

        [HttpPost]
        public ActionResult Index(BelotRoomCreator creator)
        {
            if (creator != null)
            {
                string id = Guid.NewGuid().ToString();
                var game = new BelotGame([new(), new(), new(), new()], id, _appPaths, true);
                _gameRegistry.AddContext(id, new(game, null));
                return RedirectToAction("Index", new { id });
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: Room
        public ActionResult Index(string id)
        {
            var game = _gameRegistry.GetContext(id);
            if (game != null)
            {
                ViewData["roomId"] = id;
                return View();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}