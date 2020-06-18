using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Inspection
{
    public class InspectionListModel
    {
        public int InspectionID { get; set; }
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public int ContractorID { get; set; }
        public string CompanyName { get; set; }
        public string PS { get; set;}
        public DateTime? DateRequested { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateContractorNotification { get; set; }
        public DateTime? DateSiteInspectionUploaded { get; set; }
        public DateTime? DateSiteVisitCompletion { get; set; }
        public DateTime? DateOfVisit { get; set; }
        public DateTime? DateViolationCorrection { get; set; }

        public string DCO { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public int NumberInterviews { get; set; }
        public bool Violations { get; set; }
        public bool PhotosTaken { get; set; }
        public decimal MilesOneWay { get; set; }
        public decimal MilesToEastern { get; set; }
        public decimal RoundTripMiles { get; set; }
        public decimal RoundTripHours { get; set; }
        public DateTime? DateCancelled { get; set; }

        public DateTime? DateLastUpdated { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
   }
}
