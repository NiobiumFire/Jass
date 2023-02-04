using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChatWebApp.Controllers
{
    //[Authorize]
    public class RoomController : Controller
    {

        // GET: Room
        public ActionResult Index()
        {
            var v = System.Web.HttpContext.Current;
            return View();
        }
    }
}