using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Core.Domain.Common;
using CCCS.Core.Domain.Configuration;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Documents;
using CCCS.Core.Domain.Inspection;
using CCCS.Core.Domain.Log;
using CCCS.Core.Domain.Notices;
using CCCS.Core.Domain.Projects;
using CCCS.Core.Domain.Reference;
using CCCS.Core.Domain.Users;
using CCCS.Core.Domain.Worksheets;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Web;

namespace CCCS.Data
{
    public class ContractContext : DbContext
    {
        public ContractContext()
        {
            var objContext = ((IObjectContextAdapter)this).ObjectContext;
            objContext.SavingChanges += (context_SavingChanges);
        }

        public DbSet<AccountNumber> AccountNumbers { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ActivityCode> ActivityCodes { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Cache> Caches { get; set; }
        public DbSet<ClearanceRequest> ClearanceRequests { get; set; }
        public DbSet<ClearanceRequestForm> ClearanceRequestForms { get; set; }
        public DbSet<ClearanceRequestException> ClearanceRequestExceptions { get; set; }
        public DbSet<ClearanceRequestLog> ClearanceRequestLogs { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentCategory> CommentCategories { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Contractor> Contractors { get; set; }
        public DbSet<ContractorContact> ContractorContacts { get; set; }
        public DbSet<ContractorNotification> ContractorNotifications { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentFile> DocumentFiles { get; set; }
        public DbSet<DocumentRow> DocumentRows { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<EmailRecipient> EmailRecipients { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<InspectionLog> InspectionLogs { get; set; }
        public DbSet<InspectionInterview> InspectionInterviews { get; set; }
        public DbSet<InspectionPhoto> InspectionPhotos { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageThread> MessageThreads { get; set; }
        public DbSet<NonCompliance> NonCompliances { get; set; }
        public DbSet<ExcludedNonCompliance> ExcludedNonCompliances { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectContact> ProjectContacts { get; set; }
        public DbSet<ProjectFiscalYear> ProjectFiscalYears { get; set; }
        public DbSet<PublicProfile> PublicProfiles { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewSubcontractor> ReviewSubcontractors { get; set; }
        public DbSet<RemovedDocument> RemovedDocuments { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<SiteInspection> SiteInspections { get; set; }
        public DbSet<SiteVisitCompletion> SiteVisitCompletions { get; set; }
        public DbSet<SiteVisitException> SiteVisitExceptions { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<ViolationCorrection> ViolationCorrections { get; set; }
        public DbSet<Worksheet> Worksheets { get; set; }
        public DbSet<ViewProject> ViewProjects { get; set; }
        public DbSet<BillingRate> BillingRates { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);
        }

        private void context_SavingChanges(object sender, EventArgs e)
        {
            var userName = HttpContext.Current.User.Identity.Name;

            ObjectContext context = sender as ObjectContext;
            if (context != null)
            {
                // Validate the state of each entity in the context
                // before SaveChanges can succeed.
                foreach (ObjectStateEntry entry in
                    context.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Deleted))
                {
                    if (!entry.IsRelationship && (entry.Entity.GetType() != typeof(ActivityLog)))
                    {
                        string entityType = ObjectContext.GetObjectType(entry.Entity.GetType()).FullName;
                        string entityName = ObjectContext.GetObjectType(entry.Entity.GetType()).BaseType.Name;

                        ActivityLog log = new ActivityLog
                        {
                            ActivityDate = DateTime.Now,
                            Action = entry.State.ToString(),
                            EntityType = entityType,
                            EntityName = entityName,
                            Entity = JsonConvert.SerializeObject(entry.Entity, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }),
                            ExecutedBy = userName
                        };

                        if (entry.Entity.GetType().GetProperty("ProjectID") != null)
                        {
                            var projectId = entry.Entity.GetType().GetProperty("ProjectID").GetValue(entry.Entity);

                            if (projectId != null)
                            {
                                log.ProjectId = (int)projectId;
                            }
                        }

                        LogContext logContext = new LogContext();
                        logContext.ActivityLogs.Add(log);
                        logContext.SaveChanges();
                    }
                }
            }
        }
    }
}
