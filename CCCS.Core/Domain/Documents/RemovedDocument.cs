using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Documents
{
    public class RemovedDocument
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public DateTime DateRemoved { get; set; }
        public string ProcessedBy { get; set; }
    }
}
