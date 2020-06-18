
namespace CCCS.Web.Models
{
    public class ProjectStats
    {
        public string DCO { get; set; }
        public string ContractAmount { get; set; }
        public string FederalFunds { get; set; }
        public string ProjectType { get; set; }
        public string LastSiteVisit { get; set; }
        public int NumberSiteVisit { get; set; }
        public int NumberSubs { get; set; }
        public string AvailableHours { get; set; }
        public string RemainingHours { get; set; }
    }

    public class ContractorStats
    {
        public string DCO { get; set; }
        public string AlternateDCO { get; set; }
        public int NumberProjects { get; set; }
        public int FySiteVisits { get; set; }
        public string DateRegistered { get; set; }
    }
}
