using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChatWebApp.Controllers
{
    [Authorize]
    public class RoomController : Controller
    {

        // GET: Room
        public ActionResult Index(string id)
        {
            if (id.ToLower() == "new")
            {
                id = Guid.NewGuid().ToString();
                ChatRoom.games.Add(new BelotGame(new Player[] { new Player(), new Player(), new Player(), new Player() }, id, true));
                return Redirect(Request.Url.ToString().Substring(0, Request.Url.ToString().Length - 3) + id);
            }
            else if (ChatRoom.games.Where(g => g.GameId == id).Count() > 0)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
            //var v = System.Web.HttpContext.Current;
        }
    }
}