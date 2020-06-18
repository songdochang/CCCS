using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Home
{
    public class ContractorSummaryRowModel
    {
        public int Level { get; set; }
        public int ContractorID { get; set; }
        public string CompanyName { get; set; }
        public int NumberOpenProjects { get; set; }
        public string AmountOpenProjects { get; set; }
        public int NumberFederalProjects { get; set; }
        public string AmountFederalProjects { get; set; }
        public int NumberSiteVisits { get; set; }
        public string LastSiteVisit { get; set; }
        public int NumberInCompliance { get; set; }
        public int NumberNotInCompliance { get; set; }
        public int NumberSubcontractors { get; set; }
        public int NumberSubcontractorsDistinct { get; set; }

        public List<ContractorSummaryRowModel> Subcontractors { get; set; }
    }
}
