
namespace CCCS.Core.Domain.Documents
{
    public class DocumentRow
    {
        public int Id { get; set; }
        public int ProjectID { get; set; }
        public int ContractorID { get; set; }
        public int Year { get; set; }
        public DocumentCell PrevPerf { get; set; }
        public DocumentCell NonSeg { get; set; }
        public DocumentCell GFE { get; set; }
        public DocumentCell WIBA1 { get; set; }
        public DocumentCell WIBA2 { get; set; }
        public DocumentCell EUR1 { get; set; }
        public DocumentCell EUR2 { get; set; }
        public DocumentCell EUR3 { get; set; }
        public DocumentCell EUR4 { get; set; }
        public DocumentCell EUR5 { get; set; }
        public DocumentCell EUR6 { get; set; }
        public DocumentCell EUR7 { get; set; }
        public DocumentCell EUR8 { get; set; }
        public DocumentCell EUR9 { get; set; }
        public DocumentCell EUR10 { get; set; }
        public DocumentCell EUR11 { get; set; }
        public DocumentCell EUR12 { get; set; }
        public DocumentCell ListSub { get; set; }
        public DocumentCell NtceEEO { get; set; }
    }
}