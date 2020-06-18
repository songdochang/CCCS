using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Inspection
{
    public class InspectionInterview
    {
        public int Id { get; set; }
        public int InspectionId { get; set; }
        public int ContractorId { get; set; }
        public decimal Hours { get; set; }
    }
}
