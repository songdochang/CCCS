using CCCS.Core.Domain.ClearanceRequests;
using System;
using System.Collections.Generic;

namespace CCCS.Web.Models.ClearanceRequests
{
    public class ClearanceRequestExceptionListModel
    {
        public int ClearanceRequestID { get; set; }
        public string DCO { get; set; }
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public int PrimeContractorId { get; set; }
        public string PrimeContractor { get; set; }

        public DateTime DateLastDoc { get; set; }
        public DateTime DateRequested { get; set; }
        public DateTime DateSM { get; set; }

        public int DaysDCO { get; set; }
        public int DaysSM { get; set; }
        public int DaysDEPT { get; set; }
        public int PastDueDays { get; set; }

        public string ExceptionType { get; set; }

        public List<ClearanceRequestException> Comments { get; set; }
    }
}
