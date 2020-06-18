using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Projects
{
    [Table("vwProjects")]
    public class ViewProject
    {
        [Key]
        public int Id { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DateClosed { get; set; }
        public string Phase { get; set; }
        public ProjectType? ProjectType { get; set; }
        public Boolean FederalFunds { get; set; }
        public string Unit { get; set; }
        public string DCO { get; set; }
        public int PrimeContractorID { get; set; }
        public string CompanyName { get; set; }
        public string DepartmentID { get; set; }
        public Decimal? HoursAvailable { get; set; }
        public Decimal? HoursRemaining { get; set; }
        public int? NumberSubcontractors { get; set; }
        public decimal? ContractAmount { get; set; }
        public string DepartmentName { get; set; }

    }
}
