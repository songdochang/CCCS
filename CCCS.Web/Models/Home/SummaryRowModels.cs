using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Home
{
    public class SummaryRowModel
    {
        public string DCO { get; set; }
        public int NumberOpenProjects { get; set; }
        public string AmountOpenProjects { get; set; }
        public int NumberFederalProjects { get; set; }
        public string AmountFederalProjects { get; set; }
        public int NumberSiteVisits { get; set; }
        public string LastSiteVisit { get; set; }
        public int NumberReceivedProjects { get; set; }
        public string AmountReceivedProjects { get; set; }
    }
}
