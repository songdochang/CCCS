using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Inspection
{
    public class Inspection: BaseEntity
    {
        [Display(Name = "Project ID")]
        [Required]
        public int ProjectId { get; set; }
        [Required]
        public int ContractorId { get; set; }
        [Display(Name = "Date Requested")]
        public DateTime DateRequested { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateContractorNotification { get; set; }

        [Display(Name = "Date of Visit")]
        [Required]
        public DateTime? DateOfVisit { get; set; }
        public DateTime? DateSiteInspectionUploaded { get; set; }
        public DateTime? DateSiteVisitCompletion { get; set; }
        public DateTime? DateViolationCorrection { get; set; }

        [Required]
        public string DCO { get; set; }
        public string Address { get; set; }
        [Required]
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        [Display(Name = "Number of Interviews")]
        public int NumberInterviews { get; set; }
        public bool Violations { get; set; }
        [Display(Name = "Photos Taken")]
        public bool PhotosTaken { get; set; }
        [Display(Name = "Miles One Way")]
        public decimal MilesOneWay { get; set; }
        [Display(Name = "Miles to Eastern")]
        public decimal MilesToEastern { get; set; }
        [Display(Name = "Round Trip Miles")]
        public decimal RoundTripMiles { get; set; }
        [Display(Name = "Round Trip Hours")]
        public decimal RoundTripHours { get; set; }
        public DateTime? DateCancelled { get; set; }
        public DateTime? DateLastUpdated { get; set; }
        public string Status { get; set; }
   }

    [Table("InspectionPhotos")]
    public class InspectionPhoto
    {
        [Key]
        public int PhotoID { get; set; }
        public int InspectionID { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public DateTime DateUploaded { get; set; }

        public virtual Inspection Inspection { get; set; }
    }
}
