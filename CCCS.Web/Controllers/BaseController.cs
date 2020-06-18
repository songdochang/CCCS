using CCCS.Core.Domain.Users;
using CCCS.Data;
using CCCS.Infrastructure;
using CCCS.Web.Models;
using CCCS.Web.Models.ClearanceRequests;
using CCCS.Web.Models.Contractors;
using CCCS.Web.Models.Home;
using CCCS.Web.Models.Projects;
using CrystalDecisions.CrystalReports.Engine;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace CCCS.Controllers
{
    public class BaseController : Controller
    {
        protected static string PROJECT_LIST_KEY = "CCCS_PROJECT_LIST_{0}";
        protected static string CONTRACTOR_LIST_KEY = "CCCS_CONTRACTOR_LIST_{0}";
        protected static string CONTRACTOR_OVERVIEW_KEY = "CCCS_CONTRACTOR_OVERVIEW";
        protected static string PASTDUE_LIST_KEY = "CCCS_PASTDUE_LIST_{0}_{1}";
        protected static string PROJECT_WITH_LATE_NTP_KEY = "CCCS_PROJECT_WITH_LATE_NTP";

        protected ContractContext db = new ContractContext();

        public List<ListItem> GetCOs(bool select = false)
        {
            var list = new List<ListItem>();

            if (select)
            {
                list.Add(new ListItem { Text = "- Select CO -", Value = "" });
            }
            else
            {
                list.Add(new ListItem { Text = "- All COs -", Value = "" });
            }

            ApplicationRole role = RoleManager.FindByName("DCO");
            string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();
            IEnumerable<ApplicationUser> members = UserManager.Users.Where(x => memberIDs.Any(y => y == x.Id));
            foreach (var m in members)
            {
                UserProfile up = db.UserProfiles.FirstOrDefault(x => x.UserID == m.Id);

                if (up.IsActive)
                    list.Add(new ListItem { Text = m.UserName, Value = up.UserInitial });
            }

            return list;
        }

        public List<ListItem> GetAmounts()
        {
            var list = new List<ListItem>();

            list.Add(new ListItem { Text = "- All amount -", Value = "0;0" });
            list.Add(new ListItem { Text = "Under $1,000", Value = "0;1000" });
            list.Add(new ListItem { Text = "$1,000 ~ $5,000", Value = "1000;5000" });
            list.Add(new ListItem { Text = "$5,000 ~ $50,000", Value = "5000;50000" });
            list.Add(new ListItem { Text = "$50,000 ~ $200,000", Value = "50000;200000" });
            list.Add(new ListItem { Text = "$200,000 ~ $1M", Value = "200000;1000000" });
            list.Add(new ListItem { Text = "Over $1M", Value = "1000000;0" });

            return list;
        }

        protected List<SelectListItem> GetUsers(string dco)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            if (string.IsNullOrEmpty(dco))
            {
                var dcos = GetCOs(false);

                foreach (var d in dcos)
                {
                    if (!String.IsNullOrEmpty(d.Value))
                    {
                        UserProfile up = db.UserProfiles.FirstOrDefault(x => x.UserInitial == d.Value);
                        list.Add(new SelectListItem { Text = d.Text + " (" + up.UserInitial + ")", Value = up.UserInitial });
                    }
                }
            }
            else
            {
                UserProfile up = db.UserProfiles.FirstOrDefault(x => x.UserInitial == dco);
                list.Add(new SelectListItem { Text = User.Identity.Name + " (" + up.UserInitial + ")", Value = up.UserInitial });
            }

            ViewBag.COs = list;
            return list;

        }


        protected List<SelectListItem> GetMonths()
        {
            List<SelectListItem> list = new List<SelectListItem>();

            DateTime start = DateTime.Today;
            for (int i = 0; i > -12; i--)
            {
                DateTime dt = start.AddMonths(i);
                string month = dt.ToString("MM/yyyy");

                list.Add(new SelectListItem { Text = month, Value = month });
            }

            return list;
        }

        public List<ListItem> GetDepartments()
        {
            var list = new List<ListItem>();

            list.Add(new ListItem { Text = "- All Departments -", Value = "" });

            var departments = db.Departments.ToList();
            foreach (var d in departments)
            {
                list.Add(new ListItem { Text = d.DepartmentName, Value = d.DepartmentId });
            }

            return list;
        }

        #region Properties
        public ApplicationUserManager UserManager
        {
            get
            {
                return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        public ApplicationRoleManager RoleManager
        {
            get
            {
                return HttpContext.GetOwinContext().GetUserManager<ApplicationRoleManager>();
            }
        }

        public UserProfile UserProfile
        {
            get
            {
                var user = UserManager.Users.FirstOrDefault(x => x.UserName == User.Identity.Name);

                if (user != null)
                    return db.UserProfiles.FirstOrDefault(x => x.UserID == user.Id);
                else
                    return db.UserProfiles.FirstOrDefault(x => x.FullName == User.Identity.Name);
            }
        }

        #endregion

        #region Methods

        protected static object _lock = new Object();

        public void GetProjectListModel()
        {
            string key = String.Format(PROJECT_LIST_KEY, "MASTER");
            if (!CacheHelper.Exists(key))
            {
                ProjectModel pm = new ProjectModel();
                List<ProjectModel> model = pm.GetProjectModelList();
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }
        }

        public void GetEndingSoonListAsync()
        {
            string key = String.Format(PROJECT_LIST_KEY, "ENDING");
            if (!CacheHelper.Exists(key))
            {
                lock (_lock)
                {
                    Task.Factory.StartNew(delegate
                    {
                        ProjectModel pm = new ProjectModel();
                        List<ProjectModel> model = pm.GetEndingSoonListModel();
                        CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
                    });
                }
            }
        }

        public void GetProjectListAsync(int? contractorId, bool closed = false)
        {
            if (contractorId == null)
                contractorId = 0;

            string key = String.Format(PROJECT_LIST_KEY, (closed) ? "CLOSED" : "OPEN");
            if (!CacheHelper.Exists(key))
            {
                lock (_lock)
                {
                    Task.Factory.StartNew(delegate
                    {
                        ProjectModel pm = new ProjectModel();
                        List<ProjectModel> model = pm.GetProjectModel(contractorId, closed);
                        CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
                    });
                }
            }
        }

        public void GetContractorListAsync()
        {
            if (!CacheHelper.Exists(CONTRACTOR_LIST_KEY))
            {
                lock (_lock)
                {
                    Task.Factory.StartNew(delegate
                    {
                        var model = GetContractorListModel();
                        CacheHelper.Add<List<ContractorModel>>(model, CONTRACTOR_LIST_KEY, 60.0);
                    });
                }
            }
        }

        public List<ContractorModel> GetContractorListModel()
        {
            List<ContractorModel> model = (from c in db.Contractors
                                           select new ContractorModel
                                           {
                                               ContractorID = c.Id,
                                               CompanyName = c.CompanyName,
                                               DCO = c.DCO,
                                               AlternateDCO = c.AlternateDCO,
                                               TaxId = c.TaxId
                                           }).ToList();

            model = GetExtraContractorInfo(model);

            return model;
        }

        protected List<ContractorModel> GetExtraContractorInfo(List<ContractorModel> model)
        {
            foreach (var m in model)
            {
                var contacts = db.ContractorContacts.Where(x => x.ContractorId == m.ContractorID).ToList();
                if (contacts.Count > 0)
                {
                    var contact = contacts.First();
                    m.ContactName = contact.Name;
                    m.ContactEmail = contact.Email;
                    m.ContactPhone = contact.PhoneNumber;
                    m.ContactExtension = contact.Extension;
                }

                var recentProjects = (from ct in db.Contracts
                                      join p in db.Projects on ct.ProjectId equals p.Id
                                      where ct.ContractorId == m.ContractorID
                                      orderby p.EndDate descending
                                      select new
                                      {
                                          ProjectID = p.Id,
                                          JOC = p.JOC,
                                          ProjectName = p.ProjectName,
                                          SubTo = ct.SubTo,
                                          StartDate = p.StartDate,
                                          EndDate = p.EndDate
                                      }).ToList();
                if (recentProjects.Count > 0)
                {
                    var p = recentProjects.First();

                    m.StartDate = p.StartDate;
                    m.EndDate = p.EndDate;
                    m.RecentProjectID = p.ProjectID;
                    m.RecentJOC = p.JOC;
                    m.RecentProjectName = p.ProjectName;
                    m.RecentPS = (p.SubTo == 0) ? "P" : "S";
                }

                m.NumberPastDueDocuments = db.NonCompliances.Where(x => x.ContractorID == m.ContractorID && x.DateReceived == null).Count();
            }

            return model;
        }

        public List<ContractorSummaryRowModel> GetContractorOverviewModel()
        {
            string key = CONTRACTOR_OVERVIEW_KEY;
            List<ContractorSummaryRowModel> model = new List<ContractorSummaryRowModel>();
            if (CacheHelper.Exists(key))
            {
                CacheHelper.Get<List<ContractorSummaryRowModel>>(key, out model);
                return model;
            }

            var pcs = from c in db.Contractors
                      join p in db.Projects on c.Id equals p.PrimeContractorID
                      select new { ContractorID = c.Id, CompanyName = c.CompanyName };

            foreach (var p in pcs.Distinct().ToList())
            {
                int level = 0;
                int id = p.ContractorID;

                ContractorSummaryRowModel row = new ContractorSummaryRowModel
                {
                    Level = level,
                    ContractorID = id,
                    CompanyName = p.CompanyName,
                    NumberOpenProjects = GetNumberOpenProjectsByContractor(id),
                    AmountOpenProjects = GetAmountOpenProjects(id),
                    NumberFederalProjects = GetNumberFederalProjects(id),
                    AmountFederalProjects = GetAmountFederalProjects(id),
                    NumberSiteVisits = GetNumberSiteVisits(id),
                    LastSiteVisit = GetLastSiteVisit(id),
                    NumberNotInCompliance = GetNumberNotCompliantByContractor(id)
                };

                row.NumberInCompliance = row.NumberOpenProjects - row.NumberNotInCompliance;

                List<ContractorSummaryRowModel> list = new List<ContractorSummaryRowModel>();
                row.Subcontractors = GetSubSummary(list, id, level);
                row.NumberSubcontractors = row.Subcontractors.Count;

                model.Add(row);
            }

            return model;
        }


        public void GetContractorOverviewModelAsync()
        {
            string key = CONTRACTOR_OVERVIEW_KEY;
            if (!CacheHelper.Exists(key))
            {
                lock (_lock)
                {
                    Task.Factory.StartNew(delegate
                    {
                        var model = GetContractorOverviewModel();
                        CacheHelper.Add<List<ContractorSummaryRowModel>>(model, key, 60.0);
                    });
                }
            }
        }

        #endregion

        protected int GetNumberOpenProjects(int primeContractorId = 0, string dco = "")
        {
            var projects = db.Projects.Where(x => x.DateClosed == null).ToList();

            if (primeContractorId > 0)
                projects = projects.Where(x => x.PrimeContractorID == primeContractorId).ToList();

            if (!string.IsNullOrEmpty(dco))
                projects = projects.Where(x => x.DCO == dco).ToList();

            return projects.Count;
        }

        protected string GetAmountOpenProjects(int primeContractorId = 0, string dco = "")
        {
            string amount = "N/A";

            var projects = db.Projects.Where(x => x.DateClosed == null).ToList();

            if (primeContractorId > 0)
                projects = projects.Where(x => x.PrimeContractorID == primeContractorId).ToList();

            if (!string.IsNullOrEmpty(dco))
                projects = projects.Where(x => x.DCO == dco).ToList();

            amount = ((decimal)projects.Sum(x => x.ContractAmount)).ToString("$#,###.00");

            return amount;
        }

        protected int GetNumberFederalProjects(int primeContractorId = 0, string dco = "")
        {
            var projects = db.Projects.Where(x => x.DateClosed == null && x.FederalFunds == true).ToList();

            if (primeContractorId > 0)
                projects = projects.Where(x => x.PrimeContractorID == primeContractorId).ToList();

            if (!string.IsNullOrEmpty(dco))
                projects = projects.Where(x => x.DCO == dco).ToList();

            return projects.Count;
        }

        protected string GetAmountFederalProjects(int primeContractorId = 0, string dco = "")
        {
            var projects = db.Projects.Where(x => x.DateClosed == null && x.FederalFunds == true).ToList();

            if (primeContractorId > 0)
                projects = projects.Where(x => x.PrimeContractorID == primeContractorId).ToList();

            if (!string.IsNullOrEmpty(dco))
                projects = projects.Where(x => x.DCO == dco).ToList();

            var sum = projects.Sum(x => x.ContractAmount);
            return (sum == null) ? "N/A" : ((decimal)projects.Sum(x => x.ContractAmount)).ToString("$#,###.00");
        }

        protected int GetNumberSiteVisits(int contractorId = 0, string dco = "")
        {
            var inspections = (from p in db.Projects
                               join i in db.Inspections on p.Id equals i.ProjectId
                               where p.DateClosed == null && i.DateOfVisit != null
                               select new { DCO = i.DCO, DateOfVisit = i.DateOfVisit, ContractorId = i.ContractorId }).ToList();

            if (!string.IsNullOrEmpty(dco))
                inspections = inspections.Where(x => x.DCO == dco).ToList();

            if (contractorId > 0)
                inspections = inspections.Where(x => x.ContractorId == contractorId).ToList();

            return inspections.Count();
        }

        protected string GetLastSiteVisit(int contractorId = 0, string dco = "")
        {
            string dt = "N/A";

            var inspections = (from p in db.Projects
                               join i in db.Inspections on p.Id equals i.ProjectId
                               where p.DateClosed == null && i.DateOfVisit != null
                               select new { DCO = p.DCO, DateOfVisit = i.DateOfVisit, ContractorId = i.ContractorId }).ToList();

            if (!string.IsNullOrEmpty(dco))
                inspections = inspections.Where(x => x.DCO == dco).ToList();

            if (contractorId > 0)
                inspections = inspections.Where(x => x.ContractorId == contractorId).ToList();

            if (inspections.Count == 0)
            {
                return dt;
            }
            else
            {
                var dv = inspections.OrderByDescending(x => x.DateOfVisit).First().DateOfVisit;
                return ((DateTime)dv).ToShortDateString();
            }
        }

        protected string GetLastSiteVisitByProject(int projectId)
        {
            string dt = "-";

            var inspection = (from p in db.Projects
                               join i in db.Inspections on p.Id equals i.ProjectId
                               where p.Id == projectId
                               orderby i.DateOfVisit descending
                               select i.DateOfVisit).FirstOrDefault();

            if (inspection != null)
            {
                dt = inspection.ToString();
            }

            return dt;
        }

        protected int GetNumberReceivedProjects(string dco)
        {
            DateTime dt = DateTime.Parse("1/1/" + DateTime.Today.Year);
            var projects = db.Projects.Where(x => x.DateReceived >= dt).ToList();

            if (!string.IsNullOrEmpty(dco))
                projects = projects.Where(x => x.DCO == dco).ToList();

            return projects.Count();
        }

        protected string GetAmountReceivedProjects(string dco)
        {
            DateTime dt = DateTime.Parse("1/1/" + DateTime.Today.Year);
            string amount = "N/A";
            var projects = db.Projects.Where(x => x.DateReceived >= dt).ToList();

            if (!string.IsNullOrEmpty(dco))
                projects = projects.Where(x => x.DCO == dco).ToList();

            if (projects.Count > 0)
            {
                amount = ((decimal)projects.Sum(x => x.ContractAmount)).ToString("$#,###.00");
            }

            return amount;
        }


        protected int GetMessageCount()
        {
            string ui = UserProfile.UserInitial;

            var messages = from m in db.Messages
                           group m by new { m.ThreadId } into grp
                           let LastId = grp.Max(x => x.Id)
                           from g in grp
                           join mt in db.MessageThreads on g.ThreadId equals mt.Id
                           where g.Id == LastId && mt.DateClosed == null
                           where g.Recipient == ui
                           select g;

            return messages.Count();
        }

        protected int GetNumberNotCompliant(List<ContractorSummaryRowModel> list)
        {
            int[] ids = db.NonCompliances.Select(x => x.ContractorID).Distinct().ToArray();
            var nc = list.Where(x => ids.Any(y => y == x.ContractorID));

            return nc.Count();
        }

        protected int GetNumberNotCompliantByContractor(int contractorId)
        {
            var nc = db.NonCompliances.Where(x => x.ContractorID == contractorId).Select(x => x.ProjectID).Distinct().Count();

            return nc;
        }

        protected int GetNumberSubs(List<ContractorSummaryRowModel> list, bool distinct = false)
        {
            int[] ids = list.Select(x => x.ContractorID).ToArray();
            int nsubs = 0;
            if (distinct)
            {
                nsubs = (from c in db.Contracts
                         join p in db.Projects on c.ProjectId equals p.Id
                         where ids.Any(y => y == c.SubTo) && p.DateClosed == null
                         select c.ContractorId).Distinct().Count();
            }
            else
            {
                nsubs = (from c in db.Contracts
                         join p in db.Projects on c.ProjectId equals p.Id
                         where ids.Any(y => y == c.SubTo) && p.DateClosed == null
                         select c.ContractorId).Count();
            }

            return nsubs;
        }

        protected int GetNumberSubsByContractor(int contractorId, bool distinct = false)
        {
            int cnt = 0;

            if (distinct)
            {
                cnt = (from c in db.Contracts
                       join p in db.Projects on c.ProjectId equals p.Id
                       where c.SubTo == contractorId && p.DateClosed == null
                       select c.ContractorId).Distinct().Count();
            }
            else
            {
                cnt = (from c in db.Contracts
                       join p in db.Projects on c.ProjectId equals p.Id
                       where c.SubTo == contractorId && p.DateClosed == null
                       select c.ContractorId).Count();
            }

            return cnt;
        }

        protected int GetNumberOpenProjectsByContractor(int contractorId)
        {
            var nop = (from c in db.Contracts
                       join p in db.Projects on c.ProjectId equals p.Id
                       where c.ContractorId == contractorId && p.DateClosed == null
                       select p.Id).Count();

            return nop;
        }

        protected List<ContractorSummaryRowModel> GetSubSummary(List<ContractorSummaryRowModel> list, int contractorId, int level)
        {
            var contractors = from c in db.Contractors
                              join ct in db.Contracts on c.Id equals ct.ContractorId
                              where ct.SubTo == contractorId && ct.SubLevel == level
                              select new { ContractorID = c.Id, CompanyName = c.CompanyName };

            level = level + 1;

            foreach (var p in contractors.ToList())
            {
                int id = p.ContractorID;

                ContractorSummaryRowModel row = new ContractorSummaryRowModel
                {
                    Level = level,
                    ContractorID = id,
                    CompanyName = p.CompanyName,
                    NumberOpenProjects = GetNumberOpenProjectsByContractor(id),
                    AmountOpenProjects = GetAmountOpenProjects(id),
                    NumberFederalProjects = GetNumberFederalProjects(id),
                    AmountFederalProjects = GetAmountFederalProjects(id),
                    NumberSiteVisits = GetNumberSiteVisits(id),
                    LastSiteVisit = GetLastSiteVisit(id),
                    NumberNotInCompliance = GetNumberNotCompliantByContractor(id)
                };

                row.NumberInCompliance = row.NumberOpenProjects - row.NumberNotInCompliance;
                row.NumberSubcontractors = GetNumberSubsByContractor(id);
                row.NumberSubcontractorsDistinct = GetNumberSubsByContractor(id, true);

                list.Add(row);

                list.AddRange(GetSubSummary(new List<ContractorSummaryRowModel>(), id, level));
            }

            return list;
        }

        public List<ProjectModel> SortModel(List<ProjectModel> model, string sortColumn, string sortDirection)
        {
            sortColumn = (string.IsNullOrEmpty(sortColumn)) ? "ProjectID" : sortColumn;
            sortDirection = (string.IsNullOrEmpty(sortDirection) || sortDirection == "desc") ? sortDirection = "asc" : sortDirection = "desc";

            if (sortColumn == "ProjectID")
            {
                model = (sortDirection == "asc") ? model.OrderBy(p => p.JOC).ToList() : model.OrderByDescending(p => p.JOC).ToList();
            }
            else if (sortColumn == "ProjectName")
            {
                model = (sortDirection == "asc") ? model.OrderBy(p => p.ProjectName).ToList() : model.OrderByDescending(p => p.ProjectName).ToList();
            }

            //model = (from m in model
            //        orderby m.GetType().Name == sortColumn
            //        select m).ToList();

            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;

            return model;
        }

        public List<ProjectModel> SortModel(List<ProjectModel> model, string sortOrder, int? page)
        {
            if (page == null)
            {
                ViewBag.ProjectIdSortParm = (string.IsNullOrEmpty(sortOrder) || sortOrder == "ProjectID") ? "ProjectID_desc" : "ProjectID";
                ViewBag.ProjectNameSortParm = (sortOrder == "ProjectName") ? "ProjectName_desc" : "ProjectName";
                ViewBag.PrimeContractorSortParm = (sortOrder == "PrimeContractor") ? "PrimeContractor_desc" : "PrimeContractor";
                ViewBag.DateReceivedSortParm = (sortOrder == "DateReceived") ? "DateReceived_desc" : "DateReceived";
                ViewBag.CitySortParm = (sortOrder == "City") ? "City_desc" : "City";
                ViewBag.DateClosedSortParm = (sortOrder == "DateClosed") ? "DateClosed_desc" : "DateClosed";
            }

            switch (sortOrder)
            {
                case "ProjectName":
                    model = model.OrderBy(p => p.ProjectName).ToList();
                    break;
                case "ProjectName_desc":
                    model = model.OrderByDescending(p => p.ProjectName).ToList();
                    break;
                case "ProjectID":
                    model = model.OrderBy(p => p.JOC).ToList();
                    break;
                case "ProjectID_desc":
                    model = model.OrderByDescending(p => p.JOC).ToList();
                    break;
                case "PrimeContractor":
                    model = model.OrderBy(p => p.PrimeContractorName).ToList();
                    break;
                case "PrimeContractor_desc":
                    model = model.OrderByDescending(p => p.PrimeContractorName).ToList();
                    break;
                case "DateReceived":
                    model = model.OrderBy(p => p.DateReceived).ToList();
                    break;
                case "DateReceived_desc":
                    model = model.OrderByDescending(p => p.DateReceived).ToList();
                    break;
                case "City":
                    model = model.OrderBy(p => p.City).ToList();
                    break;
                case "City_desc":
                    model = model.OrderByDescending(p => p.City).ToList();
                    break;
                case "DateClosed":
                    model = model.OrderBy(p => p.DateClosed).ToList();
                    break;
                case "DateClosed_desc":
                    model = model.OrderByDescending(p => p.DateClosed).ToList();
                    break;
                default:
                    model = model.OrderBy(p => p.JOC).ToList();
                    break;
            }

            ViewBag.SortOrder = (string.IsNullOrEmpty(sortOrder)) ? "ProjectID" : sortOrder;

            return model;
        }

        public List<ClearanceRequestModel> SortModel(List<ClearanceRequestModel> model, string sortOrder, int? page)
        {
            if (page == null)
            {
                ViewBag.DateRequestedSortParm = (sortOrder == "DateRequested") ? "DateRequested_desc" : "DateRequested";
                ViewBag.ProjectNameSortParm = (sortOrder == "ProjectName") ? "ProjectName_desc" : "ProjectName";
                ViewBag.ProjectIdSortParm = (sortOrder == "ProjectId") ? "ProjectId_desc" : "ProjectId";
                ViewBag.DateRequestedSortParm = (sortOrder == "DateRequested") ? "DateRequested_desc" : "DateRequested";
                ViewBag.DateRejectedSortParm = (sortOrder == "DateRejected") ? "DateRejected_desc" : "DateRejected";
                ViewBag.DateClosedSortParm = (String.IsNullOrEmpty(sortOrder) || sortOrder == "DateClosed") ? "DateClosed_desc" : "DateClosed";
            }

            switch (sortOrder)
            {
                case "ProjectName":
                    model = model.OrderBy(p => p.ProjectName).ToList();
                    break;
                case "ProjectName_desc":
                    model = model.OrderByDescending(p => p.ProjectName).ToList();
                    break;
                case "ProjectId":
                    model = model.OrderBy(p => p.JOC).ToList();
                    break;
                case "ProjectId_desc":
                    model = model.OrderByDescending(p => p.JOC).ToList();
                    break;
                case "DateRequested":
                    model = model.OrderBy(p => p.DateRequested).ToList();
                    break;
                case "DateRequested_desc":
                    model = model.OrderByDescending(p => p.DateRequested).ToList();
                    break;
                case "DateRejected":
                    model = model.OrderBy(p => p.DateRejected).ToList();
                    break;
                case "DateRejected_desc":
                    model = model.OrderByDescending(p => p.DateRejected).ToList();
                    break;
                case "DateClosed":
                    model = model.OrderBy(p => p.DateClosed).ToList();
                    break;
                case "DateClosed_desc":
                    model = model.OrderByDescending(p => p.DateClosed).ToList();
                    break;
                default:
                    model = model.OrderByDescending(p => p.DateClosed).ToList();
                    break;
            }

            ViewBag.SortOrder = (String.IsNullOrEmpty(sortOrder)) ? "DateClosed_desc" : sortOrder;

            return model;
        }

        public string RenderRazorViewToString(string viewName, object model, ViewDataDictionary viewData)
        {
            ViewData = viewData;
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);

                return sw.GetStringBuilder().ToString();
            }
        }

        public ActionResult GetContractors(int? id)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Insert(0, new SelectListItem { Text = "- Select Contractor -", Value = "" });

            if (id != null)
            {
                var query = from c in db.Contractors
                            join ct in db.Contracts on c.Id equals ct.ContractorId
                            join p in db.Projects on ct.ProjectId equals p.Id
                            where p.Id == id
                            orderby c.CompanyName
                            select new { c.Id, c.CompanyName }
                            ;
                list = query.Select(x => new SelectListItem { Text = x.CompanyName, Value = x.Id.ToString() }).ToList();
            }

            ViewBag.Contractors = list;

            return Json(list);
        }

        public void SetLoginInfo(ReportDocument rpt)
        {
            string[] args = ConfigurationManager.ConnectionStrings["ContractContext"].ConnectionString.Split(';');

            string uid = "", pwd = "", svr = "", database = "";
            foreach (var a in args)
            {
                if (a.StartsWith("uid"))
                {
                    uid = a.Replace("uid=", "");
                }
                else if (a.StartsWith("pwd"))
                {
                    pwd = a.Replace("pwd=", "");
                }
                else if (a.StartsWith("Data Source") || a.StartsWith("server"))
                {
                    svr = a.Replace("Data Source=", "").Replace("server=", "");
                }
                else if (a.StartsWith("Initial Catalog") || a.StartsWith("database"))
                {
                    database = a.Replace("Initial Catalog=", "").Replace("database=", "");
                    database = database.Replace("_test", "");
                }
            }

            rpt.SetDatabaseLogon(uid, pwd, svr, database);
        }

    }
}