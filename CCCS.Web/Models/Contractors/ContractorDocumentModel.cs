using CCCS.Core.Domain.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Contractors
{
    public class ContractorDocumentModel
    {
        public int ContractorID { get; set; }
        public string CompanyName { get; set; }
        public bool IsPrime { get; set; }
        public string DocumentName { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? DateReceived { get; set; }
        public bool HardCopy { get; set; }
        public bool Electronic { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
    }
}
