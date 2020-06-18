using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Contractors
{
    public class ContractorModel
    {
        public int ContractorID { get; set; }
        public string CompanyName { get; set; }
        public string DCO { get; set; }
        public string AlternateDCO { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactExtension { get; set; }
        public int NumberPastDueDocuments { get; set; }
        public int RecentProjectID { get; set; }
        public string RecentJOC { get; set; }
        public string RecentProjectName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RecentPS { get; set; }
        public string TaxId { get; set; }
    }
}
