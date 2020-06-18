using CCCS.Core.Domain.Projects;
using System.Data.Entity.ModelConfiguration;

namespace CCCS.Data.Mapping.Projects
{
    public partial class ProjectMap : EntityTypeConfiguration<Project>
    {
        public ProjectMap()
        {
            this.ToTable("Projects");
        }
    }
}