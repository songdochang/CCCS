using CCCS.Core.Domain.Projects;
using System.Data.Entity.ModelConfiguration;

namespace CCCS.Data.Mapping.Projects
{
    public partial class ProjectFiscalYearMap : EntityTypeConfiguration<ProjectFiscalYear>
    {
        public ProjectFiscalYearMap()
        {
            this.ToTable("ProjectFiscalYears");
        }
    }
}