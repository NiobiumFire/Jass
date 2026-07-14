using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BelotWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BelotRoomRegistry _roomRegistry;

        public HomeController(ILogger<HomeController> logger, BelotRoomRegistry gameRegistry)
        {
            _logger = logger;
            _roomRegistry = gameRegistry;
        }

        public IActionResult Index()
        {
            return View("Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region Lobby

        public IActionResult PopulateLobbyPartial()
        {
            return PartialView("_LobbyTableRows", _roomRegistry.GetRoomRecords());
        }

        #endregion
    }
}