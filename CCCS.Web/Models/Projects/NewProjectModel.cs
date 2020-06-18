using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Projects
{
    public class NewProjectModel
    {
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public DateTime? DateReceived { get; set; }
        public string PrimeContractorName { get; set; }
        public int ContractorID { get; set; }
        public string DCO { get; set; }
    }
}
