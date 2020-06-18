using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Inspection
{
    public class InspectionMapModel
    {
        public int InspectionID { get; set; }
        public int Point { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public decimal Miles { get; set; }
   }
}
