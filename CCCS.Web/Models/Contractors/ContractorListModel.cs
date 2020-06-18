using System;

namespace CCCS.Web.Models.Contractors
{
    public class ContractorListModel
    {
        public int ContractID { get; set; }
        public int ContractorID { get; set; }
        public string CompanyName { get; set; }
        public string TaxId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Decimal? ContractAmount { get; set; }
        public int SubTo { get; set; }
        public int SubLevel { get; set; }
        public string SubToCompanyName { get; set; }
    }
}
