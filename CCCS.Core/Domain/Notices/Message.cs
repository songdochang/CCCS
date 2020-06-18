using System;

namespace CCCS.Core.Domain.Notices
{
    public class Message : BaseEntity
    {
        public int ThreadId { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string Text { get; set; }
        public DateTime DateSent { get; set; }
    }
}
