using jwhitehead_BugTracker.Models;
using jwhitehead_BugTracker.Models.Helpers;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace jwhitehead_BugTracker.Controllers
{
    [RequireHttps] // one of the steps to force the page to render secure page.
    [Authorize(Roles = "Admin")]
    public class AdminController : Universal
    {
         // GET: Admin
        public ActionResult Index()
        {
            List<AdminUserViewModels> users = new List<AdminUserViewModels>();
            UserRoleHelper helper = new UserRoleHelper();
            foreach (var user in db.Users.ToList())
            {
                var eachUser = new AdminUserViewModels();
                eachUser.User = user;
                eachUser.SelectedRoles = helper.ListUserRoles(user.Id).ToArray();

                users.Add(eachUser);
            }
            return View(users.OrderBy(u => u.User.LastName).ToList());
        }

        // GET: EditUserRoles
        public ActionResult EditUserRoles(string id)
        {
            // implicit stating of variable
            var user = db.Users.Find(id);
            var helper = new UserRoleHelper();
            var model = new AdminUserViewModels();
            model.User = user;
            model.SelectedRoles = helper.ListUserRoles(id).ToArray();
            model.Roles = new MultiSelectList(db.Roles, "Name", "Name", model.SelectedRoles);

            return View(model);
        }

        // POST: EditUserRoles
        [HttpPost]
        public ActionResult EditUserRoles(AdminUserViewModels model)
        {
            var user = db.Users.Find(model.User.Id);
            UserRoleHelper helper = new UserRoleHelper();

            foreach (var role in db.Roles.Select(r => r.Name).ToList())
            {
                helper.RemoveUserFromRole(user.Id, role);
            }

            if (model.SelectedRoles != null)
            {
                foreach (var role in model.SelectedRoles)
                {
                    helper.AddUserToRole(user.Id, role);
                }

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
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