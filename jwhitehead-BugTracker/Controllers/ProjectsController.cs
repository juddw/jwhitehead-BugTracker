using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using jwhitehead_BugTracker.Models;
using jwhitehead_BugTracker.Models.CodeFirst;
using jwhitehead_BugTracker.Models.Helpers;
using Microsoft.AspNet.Identity;

namespace jwhitehead_BugTracker.Controllers
{
    public class ProjectsController : Universal
    {
        // GET: Projects
        private ProjectAssignHelper helper = new ProjectAssignHelper();

        [Authorize]
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;
                var userId = User.Identity.GetUserId();
                var userProjects = helper.ListUserProjects(userId);
                return View(userProjects); //PagedList??
            }
            else
            {
                ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;
                return View();
            }
        }

        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult AllProjectsIndex()


        {
            ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;
            return View(db.Projects.ToList());
        }

        // GET all projects if Admin or Project Manager
        [Authorize]
        public ActionResult ProjectAdmin()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Project Manager"))
            {
                return View(db.Projects.ToList());
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        // GET: Projects/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            var user = db.Users.Find(User.Identity.GetUserId());
            ProjectAssignHelper helper = new ProjectAssignHelper();
            if (helper.IsUserOnProject(user.Id, project.Id) || User.IsInRole("Admin"))
            {
                return View(project);
            }
            return RedirectToAction("Index");
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin, Project Manager")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Created,Updated,Title,Description,AuthorId")] Project project)
        {
            if (ModelState.IsValid)
            {
                project.Created = DateTimeOffset.UtcNow; // added in View/Web.config and CustomHelpers.cs
                project.AuthorId = User.Identity.GetUserId();

                db.Projects.Add(project);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Edit([Bind(Include = "Id,Created,Updated,Title,Description,AuthorId")] Project project)
        {
            if (ModelState.IsValid)
            {
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(project);
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Project project = db.Projects.Find(id);
            db.Projects.Remove(project);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        // GET: Projects
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult ProjectUser(int id)
        {
            var project = db.Projects.Find(id);
            ProjectUserViewModel projectuserVM = new ProjectUserViewModel();
            projectuserVM.AssignProject = project;
            projectuserVM.AssignProjectId = id; // where is the AssignProject assigned
            projectuserVM.SelectedUsers = project.Users.Select(u => u.Id).ToArray();
            // can call fullname if prefer.
            projectuserVM.Users = new MultiSelectList(db.Users.ToList(), "Id", "FirstName", projectuserVM.SelectedUsers);
            return View(projectuserVM);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Project Manager")]
        [ValidateAntiForgeryToken]
        public ActionResult ProjectUser(ProjectUserViewModel model)
        {
            ProjectAssignHelper helper = new ProjectAssignHelper();

            // remove people assigned first, then add the people selected.
            foreach (var userId in db.Users.Select(r => r.Id).ToList())
            {
                helper.RemoveUserFromProject(userId, model.AssignProjectId);
            }
            foreach (var userId in model.SelectedUsers)
            {
                helper.AddUserToProject(userId, model.AssignProjectId);
            }
            return RedirectToAction("AllProjectsIndex");
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
