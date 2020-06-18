using System;
using System.Collections.Generic;

namespace CCCS.Core.Domain.ClearanceRequests
{
    public class ClearanceRequest: BaseEntity
    {
        public int ProjectId { get; set; }
        public DateTime DateRequested { get; set; }
        public string RequestedBy { get; set; }
        public DateTime? DateModified { get; set; }
        public string CurrentStatus { get; set; }
        public string ProcessedBy { get; set; }

        public virtual List<ClearanceRequestLog> Logs { get; set; }
        public virtual List<ClearanceRequestException> Exceptions { get; set; }
    }
}
