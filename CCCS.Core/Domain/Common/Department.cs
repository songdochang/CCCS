using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Common
{
    public class Department
    {
        public string DepartmentId { get; set; }
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; }
    }
}
