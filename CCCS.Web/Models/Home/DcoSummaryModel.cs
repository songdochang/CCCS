using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCCS.Web.Models.Home
{
    public class DcoSummaryModel
    {
        public int NumberOpenProjects { get; set; }
        public string AmountOpenProjects { get; set; }
        public int NumberFederalProjects { get; set; }
        public string AmountFederalProjects { get; set; }
        public int NumberSiteVisits { get; set; }
        public string LastSiteVisit { get; set; }
    }
}