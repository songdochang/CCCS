using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Inspection
{
    public class InspectionLog
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }
        public DateTime Date { get; set; }
        public string Activity { get; set; }
        public string Comment { get; set; }
        public string ProcessedBy { get; set; }

        public Inspection Inspection { get; set; }
    }
}
