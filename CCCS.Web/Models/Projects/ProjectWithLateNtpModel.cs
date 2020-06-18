using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Core.Domain.Common;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Projects;
using CCCS.Data;
using CCCS.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CCCS.Web.Models.Projects
{
    public class ProjectWithLateNtpModel
    {
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        [Display(Name = "Date Received")]
        public DateTime? DateReceived { get; set; }
        public string Phase { get; set; }
        public string Unit { get; set; }
        public bool FederalFunds { get; set; }
        public Decimal? HoursAvailable { get; set;  }
        public Decimal? HoursRemaining { get; set; }
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        public string DCO { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public ProjectType ProjectType { get; set; }
        public Decimal? ContractAmount { get; set; }
        public int PrimeContractorId { get; set; }
        public string PrimeContractorName { get; set; }
        public int? NumberSubs { get; set; }
        public DateTime? DateClosed { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public ProjectContact Contact { get; set; }
        public ClearanceRequest ClearanceRequest { get; set; }
        public DateTime ClearanceRequestStatusDate { get; set; }
        public string ClearanceRequestStatus { get; set; }
        public List<Comment> Comments { get; set; }
        public string DateLastSiteVisit { get; set; }
        public DateTime? DateLastUpdate { get; set; }
        public DateTime? DateLastDocumentReceived { get; set; }
        public int ProgressPercent { get; set; }
        public string Analyst { get; set; }
        public string AnalystPhoneNumber { get; set; }
        public string ProjectManager { get; set; }
        public string ProjectManagerPhoneNumber { get; set; }
        public int NumberMissingDocuments { get; set; }
        public int NumberDaysReceivedStart { get; set; }


        private ContractContext db = new ContractContext();

        public List<ProjectWithLateNtpModel> GetProjectsWithLateNtp()
        {
            var list = from p in db.ViewProjects
                       join cr in db.ClearanceRequests on p.Id equals cr.ProjectId into pcr
                       from y in pcr.DefaultIfEmpty()
                       join t in db.ProjectContacts on p.Id equals t.ProjectId into yt
                       from z in yt.DefaultIfEmpty()
                       select new ProjectWithLateNtpModel
                       {
                           ProjectID = p.Id,
                           JOC = p.JOC,
                           ProjectName = p.ProjectName,
                           Address = p.Address,
                           City = p.City,
                           Zip = p.Zip,
                           DateReceived = p.DateReceived,
                           StartDate = p.StartDate,
                           EndDate = p.EndDate,
                           DateClosed = p.DateClosed,
                           Phase = p.Phase,
                           ProjectType = (p.ProjectType == null) ? ProjectType.NonCapital : (ProjectType)p.ProjectType,
                           FederalFunds = p.FederalFunds,
                           Unit = p.Unit,
                           DCO = p.DCO,
                           PrimeContractorId = p.PrimeContractorID,
                           PrimeContractorName = p.CompanyName,
                           DepartmentId = p.DepartmentID,
                           HoursAvailable = p.HoursAvailable,
                           HoursRemaining = p.HoursRemaining,
                           NumberSubs = p.NumberSubcontractors,
                           ContractAmount = p.ContractAmount,
                           DepartmentName = p.DepartmentName,
                           ClearanceRequest = y,
                           Contact = z
                       };

            var model = list.Where(x => x.DateReceived > x.StartDate).ToList(); 

            foreach(var m in model)
            {
                m.NumberDaysReceivedStart = (m.StartDate == null || m.DateReceived == null) ? 0 : ((DateTime)m.StartDate - (DateTime)m.DateReceived).Days;
            }

            return model;
        }

    }
}
