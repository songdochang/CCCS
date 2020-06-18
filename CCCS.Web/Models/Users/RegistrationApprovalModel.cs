using System;

namespace CCCS.Web.Models.Users
{
    public class RegistrationApprovalModel
    {
        public string UserID { get; set; }
        public string EmployeeNumber { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateRegistered { get; set; }
        public DateTime? DateModified { get; set; }
        public bool IsActive { get; set; }
    }
}
