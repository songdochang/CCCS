using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Reference
{
    public class AccountNumber
    {
        public int ID { get; set; }
        public string DepartmentID { get; set; }
        public string Description { get; set; }
        [Display(Name = "Account No")]
        public string AccountNo { get; set; }
        [Display(Name = "Fund Org")]
        [Required, StringLength(5)]
        public string FundOrg { get; set; }
        [Required, StringLength(5)]
        [Display(Name = "Subaccount No")]
        public string SubaccountNo { get; set; }
        public string Phase { get; set; }
        [Display(Name = "Account Description")]
        public string AccountDescription { get; set; }
        [Display(Name = "Activity Codes")]
        public string ActivityCodes { get; set; }
        [Display(Name = "Used For")]
        public string UsedFor { get; set; }
        [Display(Name = "Account Type")]
        public string AccountType { get; set; }
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }

    public class ActivityCode
    {
        [Key]
        public string Code { get; set; }
        public string Description { get; set; }
    }
}