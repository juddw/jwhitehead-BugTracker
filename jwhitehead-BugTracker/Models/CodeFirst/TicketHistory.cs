using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace jwhitehead_BugTracker.Models.CodeFirst
{
    public class TicketHistory
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Property { get; set; } // if ticket status was changed, if ticket was created, etc. You can check the property with code
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTimeOffset Created { get; set; }
        public string AuthorId { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }
}