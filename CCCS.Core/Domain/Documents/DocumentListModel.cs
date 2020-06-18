using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Models
{
    public class DocumentListModel
    {
        public int ProjectID { get; set; }
        public int ContractorID { get; set; }
        public string DocumentName { get; set; }
        public int NumberRequested { get; set; }
        public int NumberReceived { get; set; }
        public int DaysOverdue { get; set; }
        public string Title { get; set; }
    }
}
