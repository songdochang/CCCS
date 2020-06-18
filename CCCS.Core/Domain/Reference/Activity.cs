using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Reference
{
    public class Activity
    {
        [Key]
        public int ID { get; set; }
        public string Description { get; set; }
        [Display(Name ="Fund Org")]
        public string FundOrg { get; set; }
        [Display(Name ="Activity Code")]
        public string ActivityCode { get; set; }
        [Display(Name ="Is Active?")]
        public bool IsActive { get; set; }
    }
}
