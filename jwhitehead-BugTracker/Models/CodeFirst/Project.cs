using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace jwhitehead_BugTracker.Models.CodeFirst
{
    public class Project
    {
        public Project()
        {
            Users = new HashSet<ApplicationUser>();
            Tickets = new HashSet<Ticket>();
        }

        public int Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AuthorId { get; set; }

        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}