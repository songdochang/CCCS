using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models
{
    public class NonActivePrimeModel
    {
        public int ContractorID { get; set; }

        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }
        public string DCO { get; set; }
        public DateTime? PastPerf { get; set; }
        public DateTime? NonSeg { get; set; }
        public DateTime? GoodFaith { get; set; }
        public DateTime? WorkInBidArea { get; set; }
        public DateTime? EUR { get; set; }
    }
}
