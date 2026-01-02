using BelotWebApp.BelotClasses;
using BelotWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace BelotWebApp.Controllers
{
    //[Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BelotGameRegistry _gameRegistry;

        public HomeController(ILogger<HomeController> logger, BelotGameRegistry gameRegistry)
        {
            _logger = logger;
            _gameRegistry = gameRegistry;
        }

        public IActionResult Index()
        {
            ViewBag.numGames = GetNumRooms();
            return View(new BelotRoomCreator());
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

        public int GetNumRooms()
        {
            return _gameRegistry.GetAllGames().Count();
        }

        public IActionResult PopulateLobbyPartial()
        {
            List<BelotLobbyGame> games = [];
            foreach (BelotGame game in _gameRegistry.GetAllGames())
            {
                games.Add(new BelotLobbyGame(game));
            }

            return PartialView("_LobbyTableRows", games);
        }

        #endregion
    }
}