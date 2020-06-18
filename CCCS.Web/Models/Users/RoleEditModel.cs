using CCCS.Data;
using CCCS.Web.Models;
using System.Collections.Generic;

namespace CCCS.Web.Models.Users
{
    public class RoleEditModel
    {
        public ApplicationRole Role { get; set; }
        public IEnumerable<ApplicationUser> Members { get; set; }
        public IEnumerable<ApplicationUser> NonMembers { get; set; }
    }
}
