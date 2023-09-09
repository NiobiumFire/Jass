using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace BelotWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.numGames = GetNumRooms();
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public int GetNumRooms()
        {
            return ChatRoom.games.Count();
        }

        // Belot lobby
        public string PopulateLobby()
        {
            List<BelotLobbyGame> games = new List<BelotLobbyGame>();
            foreach (BelotGame game in ChatRoom.games)
            {
                games.Add(new BelotLobbyGame(game));
            }
            return new JavaScriptSerializer().Serialize(games);
        }
    }
}