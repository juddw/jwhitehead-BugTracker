using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace jwhitehead_BugTracker.Controllers
{
    [Authorize]
    public class HomeController : Universal
    {
        [RequireHttps] // one of the steps to force the page to render secure page.
        public ActionResult Index()
        {
            ViewBag.AssignedTk = db.Tickets.Where(t => t.TicketStatus.Name == "Assigned").Count();
            ViewBag.UnassignedTk = db.Tickets.Where(t => t.TicketStatus.Name == "Unassigned").Count();
            ViewBag.CompleteTk = db.Tickets.Where(t => t.TicketStatus.Name == "Complete").Count();
            ViewBag.InProgressTk = db.Tickets.Where(t => t.TicketStatus.Name == "In Progress").Count();

            ViewBag.TotalProjects = db.Projects.Count();
            ViewBag.TotalTickets = db.Tickets.Count();
            return View();
        }

        [AllowAnonymous]
        public ActionResult LandingPage()
        {
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}