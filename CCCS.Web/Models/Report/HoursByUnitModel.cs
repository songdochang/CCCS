namespace CCCS.Web.Models.Report
{
    public class HoursByUnitModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Unit { get; set; }
        public string JOC { get; set; }
        public string Phase { get; set; }
        public string CO { get; set; }
        public decimal? HoursAvailable { get; set; }
        public double? HoursCharged { get; set; }
        public decimal? HoursRemaining { get; set; }
    }
}