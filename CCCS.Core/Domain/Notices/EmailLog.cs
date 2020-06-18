using System;

namespace CCCS.Core.Domain.Notices
{
    public class EmailLog: BaseEntity
    {
        public int ProjectId { get; set; }
        public string EmailTo { get; set; }
        public string EmailCc { get; set; }
        public string Subject { get; set; }
        public string EmailBody { get; set; }
        public DateTime DateSent { get; set; }
        public string NoticeType { get; set; }
    }
}
