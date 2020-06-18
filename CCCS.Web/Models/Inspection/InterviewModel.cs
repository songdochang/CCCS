using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CCCS.Web.Models.Inspection
{
    public class InterviewModel
    {
        public int Id { get; set; }
        public int ContractorId { get; set; }
        public string CompanyName { get; set; }
        public decimal Hours { get; set; }
    }
}