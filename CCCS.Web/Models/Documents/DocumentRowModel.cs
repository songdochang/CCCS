using CCCS.Core.Domain.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Documents
{
    public class DocumentRowModel
    {
        public int ProjectID { get; set; }
        public int ContractorID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public string CompanyName { get; set; }
        public int SubTo { get; set; }
        public int SubLevel { get; set; }
        public string SubToCompanyName { get; set; }
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<DocumentRow> DocumentRows { get; set; }
    }
}
