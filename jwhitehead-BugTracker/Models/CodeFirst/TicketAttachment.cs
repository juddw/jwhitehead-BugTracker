using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace jwhitehead_BugTracker.Models.CodeFirst
{
    public class TicketAttachment
    {
        // jw added
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Description { get; set; }
        public DateTimeOffset Created { get; set; }
        public string AuthorId { get; set; }
        public string FileUrl { get; set; } // only Url needed to display image

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }
}