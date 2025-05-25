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

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

        // Belot lobby
        public int GetNumRooms()
        {
            return BelotRoom.games.Count;
        }

        public string PopulateLobby()
        {
            List<BelotLobbyGame> games = new List<BelotLobbyGame>();
            foreach (BelotGame game in BelotRoom.games)
            {
                games.Add(new BelotLobbyGame(game));
            }
            return JsonSerializer.Serialize(games);
        }
    }
}