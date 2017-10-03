using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace jwhitehead_BugTracker.Models
{
        public class EmailModel
        {
            [Required, Display(Name = "Name")]
            public string FromName { get; set; }
            [Required, Display(Name = "Email"), EmailAddress]  // login view model
            public string FromEmail { get; set; }
            [Required] // if hard coding the subject in the HomeController, remove this line.
            public string Subject { get; set; }
            [Required]
            public string Body { get; set; }
        }
}