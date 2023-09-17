using BelotWebApp.Models;
using BelotWebApp.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Threading.Tasks;

namespace BelotWebApp.Controllers
{
    public class HomeController : Controller
    {
        //private readonly IEmailService _emailService;
        private readonly EmailService emailService = new EmailService();
        public HomeController()//IEmailService emailService)
        {
            //_emailService = emailService;
        }
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

        public async Task<ActionResult> Contact()
        {
            UserEmailOptions userEmailOptions = new UserEmailOptions
            {
                ToEmails = new List<string>() { "croftjoel@gmail.com" }
            };

            //_emailService.SendTestEmail(userEmailOptions);
            await emailService.SendTestEmail(userEmailOptions);

            ViewBag.Message = "Your contact page.";

            return View();
        }

        // Belot lobby
        public int GetNumRooms()
        {
            return ChatRoom.games.Count();
        }

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