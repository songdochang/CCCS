using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Documents
{
    public class Document
    {
        public int DocumentID { get; set; }
        public int ProjectID { get; set; }
        public int? ContractorID { get; set; }
        public int? InspectionID { get; set; }
        public string DocumentName { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? DateReceived { get; set; }
        public bool HardCopy { get; set; }
        public bool Electronic { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public DateTime? DateUploaded { get; set; }
    }
}
