using System;

namespace CCCS.Core.Domain.Inspection
{
    public class SiteVisitException: BaseEntity
    {
        public string Week { get; set; }
        public string DateRange{ get; set; }
        public string CommentText { get; set; }
        public DateTime? DateComment { get; set; }
        public string DCO { get; set; }
    }
}
