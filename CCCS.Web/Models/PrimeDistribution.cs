using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models
{
    public class PrimeDistribution
    {
        public string DCO { get; set; }
        public int PrimeContractorID { get; set; }
        public string PrimeContractor { get; set; }
        public int TotalNumberProjects { get; set; }
        public int NumberCapProjects { get; set; }
        public decimal TotalContractAmount { get; set; }
        public int NumberSubs { get; set; }
        public int NumberFederalFunds { get; set; }
        public int FySiteVisits { get; set; }
        public string ProjectStatus { get; set; }
        public Dictionary<string, int> NumberProjectsByDCO { get; set; }
    }
}
