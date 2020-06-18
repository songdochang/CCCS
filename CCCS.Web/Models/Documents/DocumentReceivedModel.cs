using System;

namespace CCCS.Web.Models.Documents
{
    public class DocumentReceivedModel
    {
        public int DocumentID { get; set; }
        public int? ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public int ContractorID { get; set; }
        public string CompanyName { get; set; }
        public string DocumentType { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Date { get; set; }
        public DateTime? DateRejected { get; set; }
        public string DCO { get; set; }
        public string FileName { get; set; }
        public string RemovedBy { get; set; }
    }
}
