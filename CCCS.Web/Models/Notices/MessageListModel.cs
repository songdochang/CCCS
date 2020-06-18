using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Web.Models.Notices
{
    public class MessageListModel
    {
        public int ThreadId { get; set; }
        public int ProjectId { get; set; }
        public string JOC { get; set; }
        public string Subject { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public bool IsSent { get; set; }
        public string Text { get; set; }
        public DateTime DateSent { get; set; }
    }
}
