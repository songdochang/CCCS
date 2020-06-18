using System.Collections.Generic;

namespace CCCS.Web.Models.Worksheets
{
    public class TimesheetModel
    {
        public int TimesheetID { get; set; }
        public string Event { get; set; }
        public string Phase { get; set; }
        public string Unit { get; set; }
        public string Activity { get; set; }
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public List<DayColumn> Hours { get; set; }
    }

    public class DayColumn
    {
        public int Day { get; set; }
        public string DayOfWeek { get; set; }
        public decimal Hours { get; set; }
    }
}
