using System;

namespace CCCS.Core.Domain.Documents
{
    public class DocumentFile: BaseEntity
    {
        public string DateReceived { get; set; }
        public int ProjectID { get; set; }
        public int ContractorID { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime? DateUploaded { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateRejected { get; set; }
        public string Comment { get; set; }
        public string UploadedBy { get; set; }
    }
}