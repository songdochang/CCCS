using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Projects
{
    public class ProjectContact: BaseEntity
    {
        public int ProjectId { get; set; }
        public string DeptId { get; set; }
        [Display(Name = "Department Contact")]
        public string DeptContact { get; set; }
        [Display(Name = "Email")]
        public string DeptContactEmail { get; set; }
        [Display(Name = "Phone Number")]
        public string DeptContactPhoneNumber { get; set; }
        [Display(Name = "Extension")]
        public string DeptContactExtension { get; set; }
        public string Analyst { get; set; }
        [Display(Name = "Email")]
        public string AnalystEmail { get; set; }
        [Display(Name = "Phone Number")]
        public string AnalystPhoneNumber { get; set; }
        [Display(Name = "Extension")]
        public string AnalystExtension { get; set; }
        [Display(Name = "Project Manager")]
        public string ProjectManager { get; set; }
        [Display(Name = "Email")]
        public string ProjectManagerEmail { get; set; }
        [Display(Name = "Phone Number")]
        public string ProjectManagerPhoneNumber { get; set; }
        [Display(Name = "Extension")]
        public string ProjectManagerExtension { get; set; }
        [Display(Name = "Contractor")]
        public string Contractor { get; set; }
        [Display(Name = "Email")]
        public string ContractorEmail { get; set; }
        [Display(Name = "Phone Number")]
        public string ContractorPhoneNumber { get; set; }
        [Display(Name = "Extension")]
        public string ContractorExtension { get; set; }
    }
}
