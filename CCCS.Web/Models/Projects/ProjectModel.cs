using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Core.Domain.Common;
using CCCS.Core.Domain.Projects;
using CCCS.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CCCS.Web.Models.Projects
{
    public class ProjectModel
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
        public DateTime? DateLastSiteVisit { get; set; }
        public DateTime? DateLastUpdate { get; set; }
        public DateTime? DateLastDocumentReceived { get; set; }
        public int ProgressPercent { get; set; }
        public string Analyst { get; set; }
        public string AnalystPhoneNumber { get; set; }
        public string ProjectManager { get; set; }
        public string ProjectManagerPhoneNumber { get; set; }
        public int NumberMissingDocuments { get; set; }
        public List<string> FiscalYears { get; set; }


        private ContractContext db = new ContractContext();

        public List<ProjectModel> GetProjectModelList()
        {
            var list = from p in db.Projects
                       join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                       from q in pc.DefaultIfEmpty()
                       join d in db.Departments on p.DepartmentID equals d.DepartmentId into pd
                       from x in pd.DefaultIfEmpty()
                       join cr in db.ClearanceRequests on p.Id equals cr.ProjectId into pcr
                       from y in pcr.DefaultIfEmpty()
                       join t in db.ProjectContacts on p.Id equals t.ProjectId into yt
                       from z in yt.DefaultIfEmpty()
                       select new ProjectModel
                       {
                           ProjectID = p.Id,
                           JOC = p.JOC,
                           DateReceived = p.DateReceived,
                           StartDate = p.StartDate,
                           EndDate = p.EndDate,
                           DateClosed = p.DateClosed,
                           ProjectName = p.ProjectName,
                           Address = p.Address,
                           City = p.City,
                           Zip = p.Zip,
                           Phase = p.Phase,
                           ProjectType = (p.ProjectType == null) ? ProjectType.NonCapital : (ProjectType)p.ProjectType,
                           FederalFunds = p.FederalFunds,
                           Unit = p.Unit,
                           DCO = p.DCO,
                           HoursAvailable = p.HoursAvailable,
                           HoursRemaining = p.HoursRemaining,

                           DepartmentId = p.DepartmentID,
                           NumberSubs = p.NumberSubcontractors,
                           PrimeContractorId = p.PrimeContractorID,
                           PrimeContractorName = q.CompanyName,
                           ContractAmount = p.ContractAmount,
                           DepartmentName = x.DepartmentName,
                           ClearanceRequest = y,
                           Contact = z
                       };

            List<ProjectModel> model = list.ToList();

            return list.ToList();
        }

        public List<ProjectModel> GetProjectModel(int? contractorId, bool closed = false)
        {
            var list = from p in db.Projects
                       join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                       from q in pc.DefaultIfEmpty()
                       join d in db.Departments on p.DepartmentID equals d.DepartmentId into pd
                       from x in pd.DefaultIfEmpty()
                       join cr in db.ClearanceRequests on p.Id equals cr.ProjectId into pcr
                       from y in pcr.DefaultIfEmpty()
                       join t in db.ProjectContacts on p.Id equals t.ProjectId into yt
                       from z in yt.DefaultIfEmpty()
                       select new ProjectModel
                       {
                           ProjectID = p.Id,
                           JOC = p.JOC,
                           DateReceived = p.DateReceived,
                           StartDate = p.StartDate,
                           EndDate = p.EndDate,
                           DateClosed = p.DateClosed,
                           ProjectName = p.ProjectName,
                           Address = p.Address,
                           City = p.City,
                           Zip = p.Zip,
                           Phase = p.Phase,
                           ProjectType = (p.ProjectType == null) ? ProjectType.NonCapital : (ProjectType)p.ProjectType,
                           FederalFunds = p.FederalFunds,
                           Unit = p.Unit,
                           DCO = p.DCO,
                           HoursAvailable = p.HoursAvailable,
                           HoursRemaining = p.HoursRemaining,

                           DepartmentId = p.DepartmentID,
                           NumberSubs = p.NumberSubcontractors,
                           PrimeContractorId = p.PrimeContractorID,
                           PrimeContractorName = q.CompanyName,
                           ContractAmount = p.ContractAmount,
                           DepartmentName = x.DepartmentName,
                           ClearanceRequest = y,
                           Contact = z
                       };

            List<ProjectModel> model = (closed) ? list.Where(x => x.DateClosed != null).ToList() : list.Where(x => x.DateClosed == null).ToList();

            if (contractorId != null && contractorId > 0)
            {
                int[] projectIds = db.Contracts.Where(x => x.ContractorId == contractorId).Select(x => x.ProjectId).ToArray();
                model = model.Where(x => projectIds.Any(y => y == x.ProjectID)).ToList();
            }

            return model;
        }

        public ProjectModel GetModel(int projectId)
        {
            var model = (from p in db.Projects
                         join c in db.ClearanceRequests on p.Id equals c.ProjectId into pc
                         from y in pc.DefaultIfEmpty()
                         where p.Id == projectId
                         select new ProjectModel
                         {
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             DateReceived = p.DateReceived,
                             StartDate = p.StartDate,
                             EndDate = p.EndDate,
                             DateClosed = p.DateClosed,
                             DCO = p.DCO,
                             Phase = p.Phase,
                             FederalFunds = p.FederalFunds,
                             Unit = p.Unit,
                             HoursAvailable = p.HoursAvailable,
                             HoursRemaining = p.HoursRemaining,
                             DepartmentId = p.DepartmentID,
                             Address = p.Address,
                             City = p.City,
                             Zip = p.Zip,
                             PrimeContractorId = p.PrimeContractorID,
                             NumberSubs = p.NumberSubcontractors,
                             ContractAmount = p.ContractAmount,
                             ClearanceRequest = y
                         }).ToList();

            return model.FirstOrDefault();
        }

        public List<ProjectModel> Search(string searchString)
        {
            searchString = searchString.ToLower();

            var model = (from p in db.Projects
                         join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                         from q in pc.DefaultIfEmpty()
                         join d in db.Departments on p.DepartmentID equals d.DepartmentId into pd
                         from s in pd.DefaultIfEmpty()
                         where p.JOC.ToLower().Contains(searchString)
                             || p.ProjectName.ToLower().Contains(searchString)
                             || p.Address.ToLower().Contains(searchString)
                             || q.CompanyName.ToLower().Contains(searchString)
                         orderby p.JOC
                         select new ProjectModel
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
                             DCO = p.DCO,
                             Phase = p.Phase,
                             ProjectType = (p.ProjectType == null)? ProjectType.NonCapital: (ProjectType) p.ProjectType,
                             FederalFunds = p.FederalFunds,
                             Unit = p.Unit,
                             HoursAvailable = p.HoursAvailable,
                             HoursRemaining = p.HoursRemaining,
                             DepartmentId = p.DepartmentID,
                             PrimeContractorId = p.PrimeContractorID,
                             PrimeContractorName = q.CompanyName,
                             NumberSubs = p.NumberSubcontractors,
                             ContractAmount = p.ContractAmount,
                             DepartmentName = s.DepartmentName
                         }).ToList();

            return model;
        }

        public List<ProjectModel> GetEndingSoonListModel()
        {
            DateTime dt1 = DateTime.Today.AddDays(10);
            DateTime dt2 = DateTime.Today.AddMonths(3);
            DateTime dt = DateTime.Today;

            var list = from p in db.Projects
                       join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                       from q in pc.DefaultIfEmpty()
                       join d in db.Departments on p.DepartmentID equals d.DepartmentId into pd
                       from x in pd.DefaultIfEmpty()
                       join cr in db.ClearanceRequests on p.Id equals cr.ProjectId into pcr
                       from y in pcr.DefaultIfEmpty()
                       join t in db.ProjectContacts on p.Id equals t.ProjectId into yt
                       from z in yt.DefaultIfEmpty()
                       select new ProjectModel
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
                           ProjectType = (p.ProjectType == null)? ProjectType.NonCapital: (ProjectType) p.ProjectType,
                           FederalFunds = p.FederalFunds,
                           Unit = p.Unit,
                           DCO = p.DCO,
                           PrimeContractorId = p.PrimeContractorID,
                           PrimeContractorName = q.CompanyName,
                           DepartmentId = p.DepartmentID,
                           HoursAvailable = p.HoursAvailable,
                           HoursRemaining = p.HoursRemaining,
                           NumberSubs = p.NumberSubcontractors,
                           ContractAmount = p.ContractAmount,
                           DepartmentName = x.DepartmentName,
                           ClearanceRequest = y,
                           Contact = z
                       };

            var model = list.Where(x => x.EndDate > dt1 && x.EndDate < dt2 && x.StartDate <= dt && x.DateClosed == null).ToList();

            return model;
        }

        public List<ProjectModel> GetProjectModelByContractor(int contractorId, bool closed)
        {
            int[] projectIDs = db.Contracts.Where(x => x.ContractorId == contractorId).Select(x => x.ProjectId).ToArray();

            var list = from p in db.Projects
                       join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                       from q in pc.DefaultIfEmpty()
                       join d in db.Departments on p.DepartmentID equals d.DepartmentId into pd
                       from x in pd.DefaultIfEmpty()
                       join cr in db.ClearanceRequests on p.Id equals cr.ProjectId into pcr
                       from y in pcr.DefaultIfEmpty()
                       join t in db.ProjectContacts on p.Id equals t.ProjectId into yt
                       from z in yt.DefaultIfEmpty()
                       where projectIDs.Any(y => y == p.Id)
                       select new ProjectModel
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
                           PrimeContractorName = q.CompanyName,
                           DepartmentId = p.DepartmentID,
                           HoursAvailable = p.HoursAvailable,
                           HoursRemaining = p.HoursRemaining,
                           NumberSubs = p.NumberSubcontractors,
                           ContractAmount = p.ContractAmount,
                           DepartmentName = x.DepartmentName,
                           ClearanceRequest = y,
                           Contact = z
                       };

            List<ProjectModel> model = (closed) ? list.Where(x => x.DateClosed != null).ToList() : list.Where(x => x.DateClosed == null).ToList();

            return model;
        }

    }
}
