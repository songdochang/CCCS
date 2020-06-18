using CCCS.Core.Domain.Worksheets;

namespace CCCS.Web.Models.Worksheets
{
   public class WorksheetListModel
    {
        public Worksheet Worksheet { get; set; }

        public string AccountNo { get; set; }
        public string DepartmentID { get; set; }
        public string JOC { get; set; }
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public int PrimaryContractorID { get; set; }
        public string CompanyName { get; set; }
        public decimal TotalHours { get; set; }

        public bool Editable { get; set; }
    }
}
