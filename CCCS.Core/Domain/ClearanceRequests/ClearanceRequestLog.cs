using System;

namespace CCCS.Core.Domain.ClearanceRequests
{
    public class ClearanceRequestLog: BaseEntity
    {
        public int ClearanceRequestId { get; set; }
        public DateTime Date { get; set; }
        public string Activity { get; set; }
        public string Comment { get; set; }

        public virtual ClearanceRequest ClearanceRequest { get; set; }
    }
}
