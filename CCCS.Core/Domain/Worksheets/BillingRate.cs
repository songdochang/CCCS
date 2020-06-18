
namespace CCCS.Core.Domain.Worksheets
{
    public class BillingRate: BaseEntity
    {
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal EstimatedAmount { get; set; }

    }
}
