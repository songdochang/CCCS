using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCCS.Core.Domain.Inspection
{
    public class SiteInspection
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }
        [Display(Name = "Project Number")]
        public string ProjectNumber { get; set; }
        [Display(Name = "Project Description")]
        public string ProjectDescription { get; set; }
        [Display(Name = "Project Address")]
        public string ProjectAddress { get; set; }
        public string Contractor { get; set; }
        [Required]
        public string DCO { get; set; }
        [Display(Name = "Date of Visit")]
        public string DateOfVisit { get; set; }
    }
}
