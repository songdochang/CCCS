using CCCS.Core.Domain.Projects;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CCCS.Core.Domain.ClearanceRequests
{
    public class Review
    {
        [Key]
        [ForeignKey("Project")]
        public int ProjectID { get; set; }
        public int CheckItem1 { get; set; }
        public int CheckItem2 { get; set; }
        public int CheckItem3 { get; set; }
        public int CheckItem4 { get; set; }
        public int CheckItem5 { get; set; }
        public int CheckItem6 { get; set; }
        public int CheckItem7 { get; set; }
        public int CheckItem8 { get; set; }
        public string Comment { get; set; }
        public DateTime? DateLastUpdated { get; set; }

        public virtual Project Project { get; set; }
    }

}
