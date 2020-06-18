using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Inspection
{
    public class SiteVisitTemp
    {
        public int ProjectID { get; set; }
        public int ContractorID { get;  set; }
        public string PS { get; set;}
        public string CompanyName { get; set; }
        public DateTime? DateOfVisit { get; set; }
        public string DCO { get; set; }
        public int NumberInterviews { get; set; }
   }

    public class SiteVisitListModel
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public string Text { get; set; }
        public List<KeyValuePair<string, int>> Visits { get; set; }
        public List<KeyValuePair<string, int>> Interviews { get; set; }
        public int TotalVisits { get; set; }
        public int TotalInterviews { get; set; }
    }
}
