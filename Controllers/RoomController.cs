using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChatWebApp.Controllers
{

    public static class URLHelper
    {
        public static string BuildURL(this HttpRequestBase request, string id)
        {
            return string.Format("{0}://{1}{2}{3}",
                request.Url.Scheme,
                request.Headers["host"],
                request.RawUrl.Substring(0, request.RawUrl.Length - 3),
                id);
        }
    }

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
                ChatRoom.log.Information("Creating new room. Redirecting to " + URLHelper.BuildURL(Request, id));
                //return Redirect(Request.Url.ToString().Substring(0, Request.Url.ToString().Length - 3) + id);
                return Redirect(URLHelper.BuildURL(Request, id));
            }
            else if (ChatRoom.games.Where(g => g.GameId == id).Count() > 0)
            {
                ChatRoom.log.Information("Entering room: " + id);
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}