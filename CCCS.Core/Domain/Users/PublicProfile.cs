using System;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Users
{
    public class PublicProfile: BaseEntity
    {
        public string UserID { get; set; }
        public string EmployeeNumber { get; set; }
        [Required]
        public string Name { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        [Display(Name = "Date Registered")]
        public DateTime DateRegistered { get; set; }
        [Display(Name = "Date Modified")]
        public DateTime? DateModified { get; set; }
        public bool IsActive { get; set; }
    }
}
