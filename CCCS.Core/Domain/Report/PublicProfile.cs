using System.Data;

namespace CCCS.Models
{
    public class ReportModel
    {
        public int Sequence { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; }
        public DataTable Table { get; set; }
    }
}
