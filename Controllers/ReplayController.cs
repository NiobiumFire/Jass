using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BelotWebApp.Controllers
{
    [Authorize]
    public class ReplayController : Controller
    {
        // GET: Replay
        public ActionResult Index()
        {
            return View();
        }
    }
}