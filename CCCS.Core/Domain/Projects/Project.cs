using System;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Projects
{
    public enum ProjectType
    {
        Capital, NonCapital
    }
    public class Project: BaseEntity
    {
        [Display(Name="Project ID")]
        [Required]
        public string JOC { get; set; }
        [Display(Name = "Project Name")]
        public string ProjectName { get; set; }
        [Display(Name = "Date Received")]
        public DateTime? DateReceived { get; set; }
        public string Phase { get; set; }
        public string Unit { get; set; }
        [Display(Name = "Federal Funds")]
        public Boolean FederalFunds { get; set; }
        [Display(Name = "Hours Available")]
        public Decimal? HoursAvailable { get; set; }
        [Display(Name = "Hours Remaining")]
        public Decimal? HoursRemaining { get; set; }
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        [Display(Name = "Number of Notices")]
        public int? NumberNotice { get; set; }
        [Display(Name = "Last Update Date")]
        public DateTime? LastUpdateDate { get; set; }
        public string DCO { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        [Display(Name = "Project Type")]
        public ProjectType? ProjectType { get; set; }
        [Display(Name = "Contract Amount")]
        public decimal? ContractAmount { get; set; }
        [Display(Name = "Prime Contractor")]
        public int PrimeContractorID { get; set; }
        [Display(Name = "Number of Subcontractors")]
        public int? NumberSubcontractors { get; set; }

        [Display(Name = "Project Closed")]
        public DateTime? DateClosed { get; set; }
        public string DepartmentID { get; set; }
    }
}
