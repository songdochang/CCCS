using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Common
{
    public class DepartmentContact: BaseEntity
    {
        public string DepartmentId { get; set; }
        [Display(Name = "Contact Name")]
        public string Name { get; set; }
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
