using System;

namespace CCCS.Core.Domain.ClearanceRequests
{
    public class ClearanceRequestException: BaseEntity
    {
        public int ClearanceRequestId { get; set; }
        public string Comment { get; set; }
        public DateTime? DateCommented { get; set; }
        public string DCO { get; set; }

        public virtual ClearanceRequest ClearanceRequest { get; set; }
    }
}
