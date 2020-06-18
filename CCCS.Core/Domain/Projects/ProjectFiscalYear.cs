
namespace CCCS.Core.Domain.Projects
{
    public class ProjectFiscalYear: BaseEntity
    {
        public int ProjectId { get; set; }
        public string JOC { get; set; }
        public string FiscalYear { get; set; }
    }
}
