using System;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Users
{
    public class UserProfile: BaseEntity
    {
        [Display(Name = "User ID")]
        public string UserID { get; set; }
        [Display(Name = "Employee ID")]
        public string EmployeeID { get; set; }
        [Display(Name = "User Name")]
        public string FullName { get; set; }
        public string UserInitial { get; set; }
        public string UserColor { get; set; }
        [Display(Name = "Date Registered")]
        public DateTime DateRegistered { get; set; }
        [Display(Name = "Date Deactivated")]
        public DateTime? DateDeactivated { get; set; }

        public bool IsActive { get; set; }
    }

}
