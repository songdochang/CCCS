using System;

namespace CCCS.Core.Domain.Contractors
{
    public class Contract : BaseEntity
    {
        public int ProjectId { get; set; }
        public int ContractorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int SubTo { get; set; }
        public int SubLevel { get; set; }
        public Decimal? ContractAmount { get; set; }
        public DateTime DateRegistered { get; set; }
    }
}
