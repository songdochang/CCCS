using System;

namespace CCCS.Web.Models.Worksheets
{
    public class DepartmentalBillingModel
    {
        public int WorksheetID { get; set; }
        public DateTime WorkDate { get; set; }
        public string Unit { get; set; }
        public string ActivityCode { get; set; }
        public int ProjectID { get; set; }
        public string Unit1 { get; set; }
        public string ActivityCode1 { get; set; }
        public string JOC { get; set; }
        public string JOC1 { get; set; }
        public string ProjectDescription { get; set; }
        public string DepartmentID { get; set; }
        public int? DepartmentContactID { get; set; }
        public string DepartmentContactName { get; set; }
        public string DepartmentContactPhone { get; set; }
        public string EmployeeName { get; set; }
        public string DCO { get; set; }
        public decimal TotalHours { get; set; }
    }
}
