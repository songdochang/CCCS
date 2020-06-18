using CCCS.Core.Domain.Log;
using System.Data.Entity;

namespace CCCS.Data
{
    public class LogContext : DbContext
    {
        public LogContext() : base("LogContext")
        {
        }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);
        }

    }
}
