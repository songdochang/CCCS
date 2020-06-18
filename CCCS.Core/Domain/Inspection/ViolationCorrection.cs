using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCCS.Core.Domain.Inspection
{
    public class ViolationCorrection
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }
        public string Department { get; set; }
        [Display(Name = "Today's Date")]
        public string TodaysDate { get; set; }
        [Display(Name = "Department Contact")]
        public string DepartmentContact { get; set; }
        [Display(Name = "Project Number")]
        public string ProjectNumber { get; set; }
        [Display(Name = "Project Description")]
        public string ProjectDescription { get; set; }
        [Display(Name = "Project Address")]
        public string ProjectAddress { get; set; }
        public string Contractor { get; set; }
        [Required]
        public string DCO { get; set; }
        [Display(Name = "DCO Contact Info")]
        public string DcoContactInfo { get; set; }

        [Display(Name = "Date of Visit")]
        public string DateOfVisit { get; set; }
        [Display(Name = "No Violations")]
        public bool NoViolations { get; set; }
        [Display(Name = "EEO Postings")]
        public bool EeoPostings { get; set; }
        public bool Graffiti { get; set; }
        [Display(Name = "Segregated Facilities")]
        public bool SegregatedFacilities { get; set; }
        [Display(Name = "Referrals to OFCCA/DFEH/DHR")]
        public bool Referrals { get; set; }

        public string Comments { get; set; }
    }
}
