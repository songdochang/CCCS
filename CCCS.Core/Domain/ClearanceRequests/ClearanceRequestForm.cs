using System;

namespace CCCS.Core.Domain.ClearanceRequests
{
    public class ClearanceRequestForm: BaseEntity
    {
        public int ProjectId { get; set; }
        public DateTime Date { get; set; }
        public string DCO { get; set; }
        public string Department { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public string ContractorName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RequestedBy { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public int IsCleared { get; set; }
        public string Comments { get; set; }
        public string DcoName { get; set; }
        public DateTime? DateClearedByDCO { get; set; }
        public DateTime? SmDate { get; set; }
    }
}
