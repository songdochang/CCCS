using System.Collections.Generic;

namespace CCCS.Models.Common
{
    public class MapsDetailModel
    {
        public MapsDetailModel()
        {
            this.MapsDetails = new List<MapsDetail>();
        }

        public int ProjectId { get; set;  }
        public string FiscalYear { get; set; }
        public string Month { get; set; }
        public string MainAccount { get; set; }
        public string MainAccountDescription { get; set; }
        public string MapsCode { get; set; }
        public string MapsCodeDescription { get; set; }
        public string ReturnUrl { get; set; }

        public IList<MapsDetail> MapsDetails { get; set; }

        public class MapsDetail
        {
            public string Account { get; set; }
            public string BillCode { get; set; }
            public string BillCodeDescription { get; set; }
            public string OtherDescription { get; set; }
            public string Activity { get; set; }
            public string BillUnit { get; set; }
            public string YtdBillUnit { get; set; }
            public string Amount { get; set; }
            public string YtdAmount { get; set; }
        }
    }
}
