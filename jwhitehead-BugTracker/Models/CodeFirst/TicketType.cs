using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace jwhitehead_BugTracker.Models.CodeFirst
{
    public class TicketType
    {
        // this is a lookup table. We are not changing these values.
        public int Id { get; set; }
        public string Name { get; set; }
    }
}