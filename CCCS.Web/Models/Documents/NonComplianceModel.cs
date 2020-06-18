using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCCS.Web.Models.Documents
{
    public class NonComplianceModel
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string DCO { get; set; }
        public int ContractorID { get; set; }
        public string ContractorName { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public string ProjectManager { get; set; }
        public string DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string DocumentType { get; set; }
        public DateTime? DateRequired { get; set; }
        public DateTime? DateReceived { get; set; }
        public int PastDueDays { get; set; }
        public int PastDueMonths { get; set; }
        public bool IsExcluded { get; set; }
        public string Comment { get; set; }
        public bool IsPrime { get; set; }

        public int NumberOfDocs { get; set; }
    }
}