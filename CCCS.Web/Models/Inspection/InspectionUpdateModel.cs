using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Inspection
{
    public class InspectionUpdateModel
    {
        public int InspectionID { get; set; }
        public string DCO { get; set; }
        public string ProjectName { get; set; }
        public string ContractorName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        [Display(Name = "Round Trip Hours")]
        public int RoundTripHours { get; set; }
        [Display(Name = "Date of Visit")]
        public DateTime DateOfVisit { get; set; }
        [Display(Name = "Number of Interviews")]
        public int NumberInterviews { get; set; }
        public bool Violations { get; set; }
        public bool PhotosTaken { get; set; }
    }
}
