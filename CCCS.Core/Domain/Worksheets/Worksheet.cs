using System;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Worksheets
{
    public enum OT
    {
        A, B, C
    }
    public class Worksheet: BaseEntity
    {
        [Display(Name = "Work Date")]
        public DateTime WorkDate { get; set; }
        public string DCO { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Event Code")]
        public string EventCode { get; set; }
        [Display(Name = "Project ID")]
        public int? ProjectId { get; set; }   
        public int? ContractorId { get; set; }
        public string JOC { get; set; } 
        public OT? OT { get; set; }
        public string Phase { get; set; }
        public int Activity { get; set; }
        [Display(Name = "Activity Code")]
        [Required]
        public string ActivityCode { get; set; }

        public int Hours { get; set; }
        public int Minutes { get; set; }
        public string Comment { get; set; }
        public int? CommentCategoryId { get; set; }
        public bool IsBillable { get; set; }
        public string CreatedBy { get; set; }
    }
}
