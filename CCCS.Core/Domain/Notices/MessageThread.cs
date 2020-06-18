using System;
using System.Collections.Generic;

namespace CCCS.Core.Domain.Notices
{
    public class MessageThread: BaseEntity
    {
        public int ProjectId { get; set; }
        public string JOC { get; set; }
        public string Subject { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateClosed { get; set; }
    }
}
