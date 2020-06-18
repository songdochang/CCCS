using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCCS.Core.Domain.Documents
{
    public class NonCompliance
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public int ContractorID { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string DocumentType { get; set; }
        public DateTime DateRequired { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime DateRegistered { get; set; }
        public bool IsExcluded { get; set; }
        public string Comment { get; set; }
    }
}