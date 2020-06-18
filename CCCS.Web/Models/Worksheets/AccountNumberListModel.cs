using CCCS.Core.Domain.Reference;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Web.Models.Worksheets
{
    public class AccountNumberListModel
    {
        public int ID { get; set; }
        public string DepartmentID { get; set; }
        [Display(Name = "Department Suffix")]
        public string Description { get; set; }
        [Display(Name = "Account No")]
        [Required, StringLength(5)]
        public string FundOrg { get; set; }
        [Required, StringLength(5)]
        [Display(Name = "Subaccount No")]
        public string SubaccountNo { get; set; }
        public string Phase { get; set; }
        public string AccountDescription { get; set; }
        public List<ActivityCode> ActivityCodes { get; set; }
        [Display(Name = "Used For")]
        public string UsedFor { get; set; }
        public string AccountType { get; set; }
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }
}