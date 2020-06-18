using CCCS.Core.Domain.Inspection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Inspection
{
    public class InspectionWorksheetModel
    {
        public int InspectionId { get; set; }
        public int ProjectId { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public int PrimeContractorId { get; set; }
        public string CompanyName { get; set; }

        public DateTime? DateOfVisit { get; set; }

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

        public List<InterviewModel> Interviews { get; set; }
   }
}
