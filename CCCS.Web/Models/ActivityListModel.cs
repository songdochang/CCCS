using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models
{
    public class ActivityListModel
    {
        public int ID { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Action { get; set; }
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public string DCO { get; set; }
        public string ExecutedBy { get; set; }

    }
}
