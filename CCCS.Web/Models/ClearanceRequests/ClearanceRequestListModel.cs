using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.ClearanceRequests
{
    public class ClearanceRequestListModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<KeyValuePair<string, int>> Counts { get; set; }
        public int TotalClearanceRequests { get; set; }
    }
}
