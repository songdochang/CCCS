using System;

namespace CCCS.Core.Domain.Projects
{
    public class ServiceRequest
    {
        public int ID { get; set; }
        public string FiscalYear { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime? DateRegistered { get; set; }
    }
}
