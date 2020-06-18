using System;

namespace CCCS.Core.Domain.ClearanceRequests
{
    public class ReviewSubcontractor: BaseEntity
    {
        public int ProjectId { get; set; }
        public int ContractorId { get; set; }
        public int CheckItem5 { get; set; }
        public int CheckItem6 { get; set; }
        public int CheckItem7 { get; set; }
        public int CheckItem8 { get; set; }
        public DateTime? DateLastUpdated { get; set; }
    }

}
