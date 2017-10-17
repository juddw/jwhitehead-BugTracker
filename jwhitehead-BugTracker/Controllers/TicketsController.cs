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
using Microsoft.AspNet.Identity;
using System.IO;
using jwhitehead_BugTracker.Models.Helpers;

namespace jwhitehead_BugTracker.Controllers
{
    [Authorize]
    public class TicketsController : Universal
    {

        // GET: Tickets
        [Authorize]
        public ActionResult Index()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var tickets = db.Tickets.Include(t => t.AssignToUser).Include(t => t.OwnerUser).Include(t => t.Project).Include(t => t.TicketPriority).Include(t => t.TicketStatus);
            List<Ticket> Tickets = new List<Ticket>();
            ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;
            if (User.IsInRole("Admin"))
            {
                return View(db.Tickets.ToList());
            }
            else if (User.IsInRole("Project Manager"))
            {
                return View(db.Tickets.Where(t => t.Project.Users.Any(u => u.Id == user.Id)).ToList());
                //tickets = db.Tickets.Include(t => t.AssignToUser).Include(p => p.Project);
            }
            else if (User.IsInRole("Developer"))
            {
                return View(db.Tickets.Where(t => t.AssignToUserId == user.Id).ToList());
            }
            else if (User.IsInRole("Submitter"))
            {
                return View(db.Tickets.Where(t => t.OwnerUserId == user.Id).ToList());
            }

            return View("NotAuthNoTickets");
        }


        // GET: Tickets/Details/
        [Authorize]
        public ActionResult Details(int? id)
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            ViewBag.UserId = user.Id; // for passing UserId to the Details View for determining to show Edit/Delete buttons.

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (User.IsInRole("Admin"))
            {
                return View(ticket);
            }
            else if (User.IsInRole("Project Manager") && !ticket.Project.Users.Any(u => u.Id == user.Id))
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (User.IsInRole("Developer") && ticket.AssignToUserId != user.Id)
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (User.IsInRole("Submitter") && ticket.OwnerUserId != user.Id)
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (user.Roles.Count == 0)
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return View(ticket);
        }


        // GET: Tickets/Create
        [Authorize(Roles = "Submitter")] // this will only allow access to Submitter.
        public ActionResult Create()
        {
            var user = db.Users.Find(User.Identity.GetUserId());

            ViewBag.ProjectId = new SelectList(db.Projects.Where(p => p.Users.Any(u => u.Id == user.Id)), "Id", "Title");
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
            return View();
        }


        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Submitter")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Description,Created,Updated,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            TicketHistory ticketHistory = new TicketHistory();

            if (ModelState.IsValid)
            {
                //Add ticket to project.
                ticket.OwnerUserId = user.Id;
                ticket.TicketStatusId = 1; // set to unassigned.
                ticket.Created = DateTimeOffset.UtcNow; // added in View/Web.config and CustomHelpers.cs
                ticket.Updated = DateTimeOffset.UtcNow; // added in View/Web.config and CustomHelpers.cs
                db.Tickets.Add(ticket);

                //Take snapshot of initial state of created ticket.
                ticketHistory.AuthorId = User.Identity.GetUserId();
                ticketHistory.Created = ticket.Created;
                ticketHistory.TicketId = ticket.Id;
                ticketHistory.Property = "TICKET CREATED";
                db.TicketHistories.Add(ticketHistory);

                db.SaveChanges();

                return RedirectToAction("Index");
            }


            // catch all - repopulates fields with selected info before error.
            ViewBag.ProjectId = new SelectList(db.Projects.Where(p => p.Users.Any(u => u.Id == user.Id)), "Id", "Title");
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }


        // POST: Attachments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bind ensures the values are all entered
        public ActionResult CreateAttachment([Bind(Include = "Id,TicketId,Description")] TicketAttachment item, HttpPostedFileBase image)
        {
            // Validation.
            if (image != null && image.ContentLength > 0) // checking to make sure there is a file, and that the file has more than 0 bytes of information.
            {
                var ext = Path.GetExtension(image.FileName).ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif" && ext != ".bmp" && ext != ".txt" && ext != ".doc" && ext != ".rtf" && ext != ".pdf" && ext != ".ppt" && ext != ".pptx" && ext != ".xlsx" && ext != ".xls")
                    ModelState.AddModelError("image", "Invalid Format."); // Don't need curly braces with only one line of code.
            }

            if (ModelState.IsValid)
            {
                var filePath = "/Uploads/"; // url path
                var absPath = Server.MapPath("~" + filePath);  // physical file path
                item.FileUrl = filePath + image.FileName; // path of the file
                image.SaveAs(Path.Combine(absPath, image.FileName)); // saves
                item.Created = DateTimeOffset.UtcNow;
                item.AuthorId = User.Identity.GetUserId();
                db.TicketAttachments.Add(item);

                //Add to Ticket Details Page History that Attachment was made.
                TicketHistory ticketHistory = new TicketHistory();
                ticketHistory.AuthorId = User.Identity.GetUserId();
                ticketHistory.Created = item.Created;
                ticketHistory.TicketId = item.TicketId;
                ticketHistory.Property = "NEW TICKET ATTACHMENT";
                ticketHistory.NewValue = item.FileUrl; // put the url in newValue to enable posting it in the History.
                db.TicketHistories.Add(ticketHistory);

                // take snapshot of ticket when it is created
                // put snapshot in OldValue
                // when Ticket is Edited, take snapshot of ticket and put in NewValue
                // compare all fields in OldValue to NewValue
                // anything that changed post to Ticket Details under Edit POST action.
                // set OldValue to NewValue
                // 

                db.SaveChanges();
                return RedirectToAction("Details", new { id = item.TicketId });
            }

            return View(item);
        }


        // Commments/Create/
        [Authorize]
        public ActionResult CreateComments([Bind(Include = "Id,Body,TicketId")] TicketComment comment)
        {
            var userId = User.Identity.GetUserId();

            if (ModelState.IsValid)
            {
                if (!String.IsNullOrWhiteSpace(userId))
                {
                    //ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;
                    comment.Created = DateTimeOffset.UtcNow; // added in View/Web.config and CustomHelpers.cs
                    comment.AuthorId = User.Identity.GetUserId();
                    db.TicketComments.Add(comment);

                    var post = db.Tickets.Find(comment.TicketId);

                    //Add to Ticket Details Page History that Comment was made.
                    TicketHistory ticketHistory = new TicketHistory();
                    ticketHistory.AuthorId = User.Identity.GetUserId();
                    ticketHistory.Created = comment.Created;
                    ticketHistory.TicketId = comment.TicketId;
                    ticketHistory.Property = "NEW TICKET COMMENT";
                    ticketHistory.NewValue = comment.Body; // put the Comment Body in newValue to enable posting in the History.
                    db.TicketHistories.Add(ticketHistory);

                    db.SaveChanges();


                    // NOTIFICATION CREATE
                    // find out which Developer is assigned to Ticket.
                    // grab developer's email and send email - like forgot password
                    // notify when any history is created on ticket.




                    return RedirectToAction("Details", new { id = comment.TicketId });
                }
            }
            return RedirectToAction("Index");
        }


        //GET: Comments/Delete/
        [Authorize]
        public ActionResult CommentDelete(int? id)
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            TicketComment comments = db.TicketComments.Find(id);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (comments == null)
            {
                return HttpNotFound();
            }

            if (User.IsInRole("Admin"))
            {
                return View(comments);
            }
            else if (User.IsInRole("Project Manager") && !comments.Ticket.Project.Users.Any(u => u.Id == user.Id))
            {
                return View("NotAuthNoTickets");
            }
            else if (User.IsInRole("Developer") && comments.AuthorId != user.Id)  // check for Developer owning the comment.
            {
                return View("NotAuthNoTickets");
            }
            else if (User.IsInRole("Submitter") && comments.AuthorId != user.Id)  // check for Submitter owning the comment.
            {
                return View("NotAuthNoTickets");
            }
            else if (user.Roles.Count == 0)
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(comments);
        }


        // POST: Comments/Delete/
        [Authorize]
        [HttpPost, ActionName("CommentDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult CommentDeleteConfirmed(int id)
        {
            TicketComment comment = db.TicketComments.Find(id);

            ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;

            //Add to Ticket Details History that Comment was DELETED.
            TicketHistory ticketHistory = new TicketHistory();
            ticketHistory.AuthorId = User.Identity.GetUserId();
            ticketHistory.Created = DateTimeOffset.UtcNow;
            ticketHistory.TicketId = comment.TicketId;
            ticketHistory.Property = "COMMENT DELETED";
            ticketHistory.NewValue = comment.Body; // put the Comment Body in newValue to enable posting this body info in the History.
            db.TicketHistories.Add(ticketHistory); // adding Comment info to Ticket History
            db.TicketComments.Remove(comment); // now remove Comment from Ticket Comments
            db.SaveChanges(); // saved and takes effect.

            return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
        }


        //GET: Attachment/Delete/
        [Authorize]
        public ActionResult AttachmentDelete(int? id)
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            TicketAttachment attachment = db.TicketAttachments.Find(id);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (attachment == null)
            {
                return HttpNotFound();
            }

            if (User.IsInRole("Admin"))
            {
                return View(attachment);
            }
            else if (User.IsInRole("Project Manager") && !attachment.Ticket.Project.Users.Any(u => u.Id == user.Id))
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (User.IsInRole("Developer") && attachment.AuthorId != user.Id)  // check for Developer owning the ticket.
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (User.IsInRole("Submitter") && attachment.AuthorId != user.Id)  // check for Submitter owning the ticket.
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (user.Roles.Count == 0)
            {
                return View("NotAuthNoTickets");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return View(attachment);
        }


        // POST: Attachment/Delete/
        [Authorize]
        [HttpPost, ActionName("AttachmentDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult AttachmentDeleteConfirmed(int id)
        {
            TicketAttachment attachment = db.TicketAttachments.Find(id);

            //Add to Ticket Details History that Attachment was DELETED.
            TicketHistory ticketHistory = new TicketHistory();
            ticketHistory.AuthorId = User.Identity.GetUserId();
            ticketHistory.Created = DateTimeOffset.UtcNow;
            ticketHistory.TicketId = attachment.TicketId;
            ticketHistory.Property = "ATTACHMENT DELETED";
            ticketHistory.NewValue = attachment.FileUrl; // put the Attachment URL in newValue to enable posting this link info in the History.
            db.TicketHistories.Add(ticketHistory); // adding Attachment info to Ticket History
            db.TicketAttachments.Remove(attachment); // now remove Attachment from Ticket Attachments
            db.SaveChanges(); // saved and takes effect.

            return RedirectToAction("Details", "Tickets", new { id = ticketHistory.TicketId });
        }


        //// GET: TicketComments/Edit/
        //[Authorize]
        //public ActionResult CommentEdit(int? id)
        //{
        //    var user = db.Users.Find(User.Identity.GetUserId());
        //    Ticket ticket = db.Tickets.Find(id);

        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    if (ticket == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    if (User.IsInRole("Admin"))
        //    {
        //        return View(ticket);
        //    }
        //    else if (User.IsInRole("Project Manager") && !ticket.Project.Users.Any(u => u.Id == user.Id))
        //    {
        //        return View("NotAuthNoTickets");
        //    }
        //    else if (User.IsInRole("Developer") && ticket.AssignToUserId != user.Id)
        //    {
        //        return View("NotAuthNoTickets");
        //    }
        //    else if (User.IsInRole("Submitter") && ticket.OwnerUserId != user.Id)
        //    {
        //        return View("NotAuthNoTickets");
        //    }
        //    else if (user.Roles.Count == 0)
        //    {
        //        return View("NotAuthNoTickets");
        //    }

        //    return View(ticket);
        //}


        // GET: Tickets/Edit/
        [Authorize]
        public ActionResult Edit(int? id)
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            Ticket ticket = db.Tickets.Find(id);
            UserRoleHelper userRoleHelper = new UserRoleHelper();

            var developers = userRoleHelper.UsersInRole("Developer");
            var devsOnProj = developers.Where(d => d.Projects.Any(p => p.Id == ticket.ProjectId));

            ViewBag.AssignToUserId = new SelectList(devsOnProj, "Id", "FullName", ticket.AssignToUserId);
            ViewBag.OwnerUserId = new SelectList(userRoleHelper.UsersInRole("Submitter"), "Id", "FullName", ticket.OwnerUserId); // Set the list to only those in Submitter Role.
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Title", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            ViewBag.UserTimeZone = db.Users.Find(User.Identity.GetUserId()).TimeZone;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (User.IsInRole("Admin"))
            {
                return View(ticket);
            }
            else if (User.IsInRole("Project Manager") && !ticket.Project.Users.Any(u => u.Id == user.Id))
            {
                return View("NotAuthNoTickets");
            }
            else if (User.IsInRole("Developer") && ticket.AssignToUserId != user.Id)
            {
                return View("NotAuthNoTickets");
            }
            else if (User.IsInRole("Submitter") && ticket.OwnerUserId != user.Id)
            {
                return View("NotAuthNoTickets");
            }
            else if (user.Roles.Count == 0)
            {
                return View("NotAuthNoTickets");
            }

            return View(ticket);
        }


        // POST: Tickets/Edit/
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignToUserId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                // if ticket has a user and status is Unassigned, switch to assigned.
                if (ticket.AssignToUserId != null && ticket.TicketStatusId == 1) // only set ticket.AssignToUserId if argument passed is a string. Doesn't like setting to null.
                {                                                                // TicketStatusId is found in the Tickets Model and is a Foreign Key. Since it is named the same as the TicketStatus Model with Id added.
                                                                                 // it points to the Primary Key TicketStatus.Id
                    ticket.TicketStatusId = db.TicketStatuses.FirstOrDefault(t => t.Name == "Assigned").Id; // change unassigned to assigned when assigning user to ticket.
                }

                // Get changed values of ticket and compare with original values.
                var newTicket = ticket;
                var ticketList = db.Tickets.ToList();
                var oldTicket = ticketList.FirstOrDefault(t => t.Id == ticket.Id);
                var user = db.Users.Find(User.Identity.GetUserId());

                if (oldTicket.Title != newTicket.Title)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.Title;
                    history.NewValue = newTicket.Title;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }
                if (oldTicket.Description != newTicket.Description)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.Description;
                    history.NewValue = newTicket.Description;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }
                // Project Name
                if (oldTicket.ProjectId != newTicket.ProjectId)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.Project.Title;
                    history.NewValue = db.Projects.Find(newTicket.ProjectId).Title;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }
                // Software or Hardware
                if (oldTicket.TicketType != newTicket.TicketType)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.TicketType.Name;
                    history.NewValue = db.TicketTypes.Find(newTicket.TicketTypeId).Name;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }
                // Low, Medium, High, Urgent
                if (oldTicket.TicketPriorityId != newTicket.TicketPriorityId)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.TicketPriority.Name;
                    history.NewValue = db.TicketPriorities.Find(newTicket.TicketPriorityId).Name;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }
                // Unassigned, Assigned, In Progress, Completed
                if (oldTicket.TicketStatusId != newTicket.TicketStatusId)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.TicketStatus.Name;
                    history.NewValue = db.TicketStatuses.Find(newTicket.TicketStatusId).Name;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }
                if (oldTicket.AssignToUserId != newTicket.AssignToUserId)
                {
                    TicketHistory history = new TicketHistory();
                    history.TicketId = newTicket.Id;
                    history.Property = "TICKET EDITED";
                    history.OldValue = oldTicket.AssignToUser.FullName; // The var ticketList = db.Tickets.ToList(); var oldTicket = ticketList.FirstOrDefault(t => t.Id == ticket.Id); Allows you to not call the db.
                    history.NewValue = db.Users.Find(newTicket.AssignToUserId).FullName;
                    history.Created = DateTimeOffset.UtcNow;
                    history.AuthorId = user.Id;
                    db.TicketHistories.Add(history);
                }

                ticket.Updated = DateTimeOffset.UtcNow;
                db.Entry(oldTicket).State = EntityState.Detached; // So instead of using AsNoTracking() what you can do is Find() and then detach it from the context.I believe that this gives you the same result as AsNoTracking() besides the additional overhead of getting the entity tracked.
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AssignToUserId = new SelectList(db.Users, "Id", "FirstName", ticket.AssignToUserId);
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "FirstName", ticket.OwnerUserId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Title", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }


        // GET: Tickets/Delete/
        [Authorize]
        public ActionResult Delete(int? id)
        {
            Ticket ticket = db.Tickets.Find(id);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }


        //// POST: Tickets/Delete/
        //[HttpPost, ActionName("Delete")]
        //[Authorize]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Ticket ticket = db.Tickets.Find(id);
        //    db.Tickets.Remove(ticket);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}


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
