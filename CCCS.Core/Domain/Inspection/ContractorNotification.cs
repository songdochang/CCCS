using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCCS.Core.Domain.Inspection
{
    public class ContractorNotification
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }

        [Display(Name = "Project ID")]
        public string JOC { get; set; }
        [Display(Name = "Contractor Representative")]
        public string ContractorRepresentative { get; set; }
        public string ProjectDescription { get; set; }

        [Display(Name = "Date of Visit")]
        [Required]
        public string DateOfVisit { get; set; }
        [Display(Name = "Time of Visit")]
        public string TimeOfVisit { get; set; }
        public string Location { get; set; }
        [Required]
        public string DCO { get; set; }
        [Display(Name = "DCO Contact Info")]
        public string DcoContactInfo { get; set; }

        [Display(Name = "Site Inspection")]
        public bool SiteInspection { get; set; }
        [Display(Name = "Employee Interviews")]
        public bool EmployeeInterviews { get; set; }
        [Display(Name = "Good Faith Review")]
        public bool GoodFaithReview { get; set; }
   }
}
