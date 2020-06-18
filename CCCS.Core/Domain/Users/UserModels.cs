using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Users
{
    public enum UserRole
    {
        DCO, Administrator, Clerical
    }

    public class CreateModel
    {
        [Required]
        public string EmployeeID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        [Display(Name="User Initial")]
        public string UserInitial { get; set; }
    }

    public class RoleModificationModel
    {
        [Required]
        public string RoleName { get; set; }
        public string[] IdsToAdd { get; set; }
        public string[] IdsToDelete { get; set; }
    }
}
