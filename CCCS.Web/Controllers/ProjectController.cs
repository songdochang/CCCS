using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using CCCS.Web.Models;
using System.Web.UI.WebControls;
using PagedList;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Entity.Validation;
using CCCS.Infrastructure;
using DocumentFormat.OpenXml.Packaging;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Projects;
using CCCS.Core.Domain.Documents;
using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Core.Domain.Common;
using CCCS.Web.Models.Contractors;
using CCCS.Web.Models.Documents;
using CCCS.Models.Common;
using CCCS.Web.Models.Inspection;
using CCCS.Web.Models.Worksheets;
using CCCS.Web.Models.Notices;
using CCCS.Web.Models.Projects;
using System.Web.Hosting;
using CCCS.Core.Data;

namespace CCCS.Controllers
{
    [Authorize]
    public class ProjectController : BaseController
    {
        const int PAGE_SIZE = 15;
        const int MAX_RECORDS = 500;

        private readonly IRepository<Contractor> _contractorRepository;
        private readonly IRepository<DocumentFile> _documentFileRepository;
        private readonly IRepository<Project> _projectRepository;

        public ProjectController() { }

        public ProjectController(
            IRepository<Contractor> contractorRepository,
            IRepository<DocumentFile> documentFileRepository,
            IRepository<Project> projectRepository)
        {
            this._contractorRepository = contractorRepository;
            this._documentFileRepository = documentFileRepository;
            this._projectRepository = projectRepository;
        }

        public ActionResult Index2(string sortOrder, string co, string fed, string projectType, string listStyle,
            string joc, int? page, string prime = "", string sub = "", string minAmount = "", string maxAmount = "")
        {
            List<ProjectModel> model;

            ProjectModel pm = new ProjectModel();
            string key = String.Format(PROJECT_LIST_KEY, "OPEN");
            if (!CacheHelper.Get<List<ProjectModel>>(key, out model))
            {
                model = pm.GetProjectModel(null, false);
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }

            if (String.IsNullOrEmpty(co) && User.IsInRole("DCO"))
                co = UserProfile.UserInitial;

            decimal min = (String.IsNullOrEmpty(minAmount)) ? 0 : decimal.Parse(minAmount);
            decimal max = (String.IsNullOrEmpty(maxAmount)) ? (decimal.MaxValue - 1.0m) : decimal.Parse(maxAmount);
            model = FilterModel(model, co, fed, projectType, listStyle, joc, page, false, prime, sub, min, max);

            model = SortModel(model, sortOrder, page);

            GetViewBags();

            ViewBag.Total = model.Count;
            ViewBag.ListStyle = listStyle;

            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            minAmount = (String.IsNullOrEmpty(minAmount)) ? "0" : minAmount.Replace(",", "");
            maxAmount = (String.IsNullOrEmpty(maxAmount)) ? "0" : maxAmount.Replace(",", "");
            ViewBag.Amount = String.Concat(minAmount, ";", maxAmount);

            int pageNumber = (page ?? 1);
            ViewBag.PageNumber = pageNumber;

            var pagedModel = model.ToPagedList(pageNumber, PAGE_SIZE);
            pagedModel = CommonHelper.GetExtraInfo(pagedModel);

            return View(pagedModel);
        }

        public ActionResult Index3(string sortOrder, string co, string fed, string projectType, string listStyle,
            string joc, int? page, string prime = "", string sub = "", string minAmount = "", string maxAmount = "")
        {
            List<ProjectModel> model;

            ProjectModel pm = new ProjectModel();
            string key = String.Format(PROJECT_LIST_KEY, "CLOSED");
            if (!CacheHelper.Get<List<ProjectModel>>(key, out model))
            {
                model = pm.GetProjectModel(null, true);
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }

            if (String.IsNullOrEmpty(co) && User.IsInRole("DCO"))
                co = UserProfile.UserInitial;

            decimal min = (String.IsNullOrEmpty(minAmount)) ? 0 : decimal.Parse(minAmount);
            decimal max = (String.IsNullOrEmpty(maxAmount)) ? (decimal.MaxValue - 1.0m) : decimal.Parse(maxAmount);
            model = FilterModel(model, co, fed, projectType, listStyle, joc, page, true, prime, sub, min, max);

            if (sortOrder == null)
                sortOrder = "DateClosed_desc";

            model = SortModel(model, sortOrder, page);

            GetViewBags();
            ViewBag.Total = model.Count;
            ViewBag.ListStyle = listStyle;

            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            minAmount = (String.IsNullOrEmpty(minAmount)) ? "0" : minAmount.Replace(",", "");
            maxAmount = (String.IsNullOrEmpty(maxAmount)) ? "0" : maxAmount.Replace(",", "");
            ViewBag.Amount = String.Concat(minAmount, ";", maxAmount);

            int pageNumber = (page ?? 1);
            //TODO: filter sets pageNumber to 1
            ViewBag.PageNumber = pageNumber;

            var pagedModel = model.ToPagedList(pageNumber, PAGE_SIZE);
            pagedModel = CommonHelper.GetExtraInfo(pagedModel);

            return View(pagedModel);
        }

        private List<ProjectModel> FilterModel(List<ProjectModel> model, string co, string fed, string projectType, string listStyle,
            string joc, int? page, bool closed, string prime = "", string sub = "", decimal minAmount = 0.0m, decimal maxAmount = decimal.MaxValue)
        {
            if (!string.IsNullOrEmpty(co))
            {
                model = model.Where(x => x.DCO == co).ToList();
            }
            ViewBag.CO = co;

            if (!string.IsNullOrEmpty(projectType))
            {
                ProjectType pt = (ProjectType)int.Parse(projectType);

                model = model.Where(x => x.ProjectType == pt).ToList();
            }
            ViewBag.ProjectType = projectType;

            if (!string.IsNullOrEmpty(joc) && joc.Contains("true"))
            {
                model = model.Where(x => x.JOC.Contains("JOC")).ToList();
            }
            ViewBag.JOC = joc;

            if (!string.IsNullOrEmpty(fed))
            {
                if (fed == "Fed")
                    model = model.Where(x => x.FederalFunds == true).ToList();
                else
                    model = model.Where(x => x.FederalFunds == false).ToList();
            }
            ViewBag.Fed = fed;

            if (!string.IsNullOrEmpty(prime))
            {
                string p = prime.ToLower();
                model = (from m in model
                         join c in db.Contractors on m.PrimeContractorId equals c.Id
                         where c.CompanyName.ToLower().Contains(p)
                         select m).ToList();
            }
            ViewBag.Prime = prime;

            if (!string.IsNullOrEmpty(sub))
            {
                string s = sub.ToLower();
                model = (from m in model
                         join ct in db.Contracts on m.ProjectID equals ct.ProjectId
                         join c in db.Contractors on ct.ContractorId equals c.Id
                         where c.CompanyName.ToLower().Contains(s) && ct.SubTo != 0
                         select m).ToList();
            }
            ViewBag.Sub = sub;

            model = model.Where(x => x.ContractAmount >= (minAmount - 0.5m) && x.ContractAmount < (maxAmount + 0.5m)).ToList();

            return model;
        }

        // Filter 'Ending Soon'
        private List<ProjectModel> FilterModel(List<ProjectModel> model, string notVisited,
            decimal minAmount, decimal maxAmount, string co, int? page)
        {
            model = model.Where(x => x.ContractAmount >= (minAmount - 0.5m) && x.ContractAmount < (maxAmount + 0.5m)).ToList();

            if (!String.IsNullOrEmpty(co))
                model = model.Where(x => x.DCO == co).ToList();

            if (!string.IsNullOrEmpty(notVisited) && notVisited.Contains("true"))
            {
                model = model.Where(x => x.DateLastSiteVisit == null).ToList();
            }

            return model;
        }

        [HttpPost]
        public ActionResult Index2(FormCollection form, int page)
        {
            bool reset = (form["submit"] == "reset");

            string co = form["co"];
            string fed = form["fed"];
            string prime = form["prime"];
            string sub = form["sub"];
            string projectType = form["ProjectType"];
            string listStyle = form["ListStyle"];
            string joc = form["joc"];
            string minAmount = form["minAmount"];
            string maxAmount = form["maxAmount"];

            if (reset)
            {
                co = "";
                fed = "";
                prime = "";
                sub = "";
                projectType = "";
                listStyle = "";
                joc = "";
                minAmount = "";
                maxAmount = "";
                page = 1;
            }

            return RedirectToAction("Index2", new
            {
                co = co,
                fed = fed,
                projectType = projectType,
                listStyle = listStyle,
                page = page,
                prime = prime,
                sub = sub,
                minAmount = minAmount,
                maxAmount = maxAmount
            });
        }

        [HttpPost]
        public ActionResult Index3(FormCollection form, int page)
        {
            bool reset = (form["submit"] == "reset");

            string co = form["co"];
            string fed = form["fed"];
            string prime = form["prime"];
            string sub = form["sub"];
            string projectType = form["ProjectType"];
            string listStyle = form["ListStyle"];
            string joc = form["joc"];
            string minAmount = form["minAmount"];
            string maxAmount = form["maxAmount"];

            if (reset)
            {
                co = "";
                fed = "";
                prime = "";
                sub = "";
                projectType = "";
                listStyle = "";
                joc = "";
                minAmount = "";
                maxAmount = "";
                page = 1;
            }

            return RedirectToAction("Index3", new
            {
                co = co,
                fed = fed,
                projectType = projectType,
                listStyle = listStyle,
                page = page,
                prime = prime,
                sub = sub,
                minAmount = minAmount,
                maxAmount = maxAmount
            });
        }

        public ActionResult Index1(string sortOrder, string co, string listStyle, string notVisited,
            string minAmount, string maxAmount, int? page)
        {
            List<ProjectModel> model;

            ProjectModel pm = new ProjectModel();
            string key = String.Format(PROJECT_LIST_KEY, "ENDING");
            if (!CacheHelper.Get<List<ProjectModel>>(key, out model))
            {
                model = pm.GetEndingSoonListModel();
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }

            if (String.IsNullOrEmpty(notVisited))
                notVisited = "false";

            if (String.IsNullOrEmpty(co) && User.IsInRole("DCO"))
                co = UserProfile.UserInitial;

            decimal min = (String.IsNullOrEmpty(minAmount)) ? 0 : decimal.Parse(minAmount);
            decimal max = (String.IsNullOrEmpty(maxAmount)) ? (decimal.MaxValue - 1.0m) : decimal.Parse(maxAmount);
            model = FilterModel(model, notVisited, min, max, co, page);

            model = SortModel(model, sortOrder, page);

            ViewBag.COs = GetCOs();
            ViewBag.CO = co;

            ViewBag.Amounts = GetAmounts();
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            minAmount = (String.IsNullOrEmpty(minAmount)) ? "0" : minAmount.Replace(",", "");
            maxAmount = (String.IsNullOrEmpty(maxAmount)) ? "0" : maxAmount.Replace(",", "");
            ViewBag.Amount = String.Concat(minAmount, ";", maxAmount);

            ViewBag.Total = model.Count;

            ViewBag.NotVisited = (notVisited.Contains("true")) ? "true" : "false";
            ViewBag.ListStyle = listStyle;

            int pageNumber = (page ?? 1);
            ViewBag.PageNumber = pageNumber;

            var pagedModel = model.ToPagedList(pageNumber, PAGE_SIZE);
            pagedModel = CommonHelper.GetExtraInfo(pagedModel);

            return View(pagedModel);
        }

        [HttpPost]
        public ActionResult Index1(FormCollection form, int page)
        {
            bool reset = (form["submit"] == "reset");

            string co = form["co"];
            string listStyle = form["ListStyle"];
            string notVisited = form["notVisitedCheck"];
            string minAmount = form["minAmount"];
            string maxAmount = form["maxAmount"];

            if (reset)
            {
                co = "";
                listStyle = "";
                notVisited = "";
                minAmount = "";
                maxAmount = "";
                page = 1;
            }

            return RedirectToAction("Index1", new
            {
                co = co,
                listStyle = listStyle,
                notVisited = notVisited,
                minAmount = minAmount,
                maxAmount = maxAmount,
                page = page
            });
        }

        public ActionResult Index(FormCollection form, string search, string sortColumn, string sortDirection, string co)
        {
            List<ProjectModel> model;
            string searchString = (form["searchString"] == null) ? search : form["searchString"].Trim();

            GetViewBags();
            ViewBag.SearchString = searchString;

            if (string.IsNullOrEmpty(searchString) || searchString.Length < 3)
            {
                TempData["Message"] = "Error: Search string must be 3 characters or more.";
                ViewBag.CO = form["co"];

                return View();
            }

            ProjectModel pm = new ProjectModel();
            model = pm.Search(searchString);

            co = (form["co"] == null) ? co : form["co"];
            if (!String.IsNullOrEmpty(co))
            {
                model = model.Where(x => x.DCO == co).ToList();
            }

            model = SortModel(model, sortColumn, sortDirection);

            ViewBag.CO = co;
            ViewBag.Total = model.Count;

            if (model.Count > MAX_RECORDS)
            {
                TempData["Message"] = "Error: Search resulted in too many records.  Please enter more restrictive search string.";
                return View();
            }
            else
            {
                return View(model);
            }
        }

        public ActionResult Index4(string fiscalYear, int? page)
        {
            var list = GetFiscalYears();
            ViewBag.FiscalYears = list;

            int pageNumber = (page ?? 1);

            fiscalYear = (String.IsNullOrEmpty(fiscalYear)) ? list[0].Text : fiscalYear;

            List<ServiceRequest> model = new List<ServiceRequest>();
            List<ServiceRequest> serviceRequests = db.ServiceRequests.Where(x => x.FiscalYear == fiscalYear).ToList();

            string str = Request.Params["searchString"];
            if (!String.IsNullOrEmpty(str))
            {
                foreach (var sr in serviceRequests)
                {
                    string path = Server.MapPath("~/Files/ServiceRequest/" + sr.FilePath);
                    if (sr.FileName.Contains(str) || SearchInWord(path, str))
                    {
                        model.Add(sr);
                    }
                }

                ViewBag.SearchString = str;
            }
            else
            {
                model = serviceRequests;
            }

            return View(model.ToPagedList(pageNumber, 500));
        }

        public ViewResult Index5(int? page)
        {
            List<ProjectModel> model;

            string key = String.Format(PROJECT_LIST_KEY, "ALL");
            if (!CacheHelper.Get<List<ProjectModel>>(key, out model))
            {
                ProjectModel pm = new ProjectModel();
                model = pm.GetProjectModelList().OrderByDescending(x => x.DateReceived).ToList();
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }

            int pageNumber = (page ?? 1);
            ViewBag.PageNumber = pageNumber;

            var pagedModel = model.ToPagedList(pageNumber, 100);
            //pagedModel = CommonHelper.GetExtraInfo(pagedModel);

            return View(pagedModel);
        }

        private List<SelectListItem> GetFiscalYears()
        {
            var list = new List<SelectListItem>();

            string fy = "";
            int year = DateTime.Today.Year + 1;

            do
            {
                fy = (year - 1).ToString() + "-" + year.ToString();
                list.Add(new SelectListItem { Text = fy, Value = fy });

                year = year - 1;
            } while (year > 2011);

            return list;
        }

        private bool SearchInWord(string path, string searchKeyWord)
        {
            try
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(path, true))
                {
                    var text = wordDoc.MainDocumentPart.Document.InnerText;
                    if (text.Contains(searchKeyWord))
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // GET: Project/Details1/5
        public ActionResult Details1(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var project = _projectRepository.GetById(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            ProjectModel model = new ProjectModel
            {
                ProjectID = (int)id,
                JOC = project.JOC,
                PrimeContractorId = project.PrimeContractorID,
                DateReceived = project.DateReceived,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Address = project.Address,
                City = project.City,
                Zip = project.Zip,
                Unit = project.Unit,
                Phase = project.Phase
            };

            var contractor = _contractorRepository.GetById(project.PrimeContractorID);
            if (contractor != null)
            {
                model.PrimeContractorName = contractor.CompanyName;
            }

            var department = db.Departments.Find(project.DepartmentID);
            if (department != null)
            {
                model.DepartmentName = department.DepartmentName;
            }

            var contact = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == model.ProjectID);
            if (contact != null)
            {
                model.Analyst = contact.Analyst;
                model.AnalystPhoneNumber = contact.AnalystPhoneNumber;
                model.ProjectManager = contact.ProjectManager;
                model.ProjectManagerPhoneNumber = contact.ProjectManagerPhoneNumber;
            }

            var cm = db.Comments.Where(x => x.EntityId == model.ProjectID).ToList();
            var ws = db.Worksheets.Where(x => x.ProjectId == model.ProjectID)
                .Where(x => !String.IsNullOrEmpty(x.Comment))
                .Select(x => new { EntityId = x.Id, CommentText = x.Comment, CommentedBy = x.DCO, DateRegistered = x.WorkDate }).ToList()
                .Select(x => new Comment { EntityId = x.EntityId, CommentText = x.CommentText, Category = "Worksheet", CommentedBy = x.CommentedBy, DateRegistered = x.DateRegistered });

            var comments = cm.Union(ws);
            if (comments != null)
            {
                model.Comments = comments.OrderBy(x => x.DateRegistered).ToList();
            }

            //TODO
            model.FiscalYears = new List<string>();

            ViewBag.User = UserProfile.UserInitial;

            return View(model);
        }

        public ActionResult Details2(int? id, string message = "")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int projectId = (int)id;
            Project project = db.Projects.FirstOrDefault(x => x.Id == projectId);
            ViewBag.ProjectID = projectId;
            ViewBag.ProjectName = project.ProjectName;
            ViewBag.JOC = project.JOC;
            ViewBag.NumberSubcontractors = project.NumberSubcontractors;

            Document model = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == "Service Request");
            if (model == null)
            {
                model = new Document();
            }

            ViewBag.Message = message;

            return View(model);
        }

        public ActionResult Details3(int id)
        {
            List<DocumentRowModel> model = GetDocumentList(id);

            ViewBag.ProjectID = id;

            return View(model);
        }

        public List<DocumentRowModel> GetDocumentList(int id)
        {
            var query = from c in db.Contractors
                        join ct in db.Contracts on c.Id equals ct.ContractorId
                        where ct.ProjectId == id
                        select new ContractorListModel
                        {
                            ContractID = ct.Id,
                            ContractorID = ct.ContractorId,
                            StartDate = ct.StartDate,
                            EndDate = ct.EndDate,
                            SubTo = ct.SubTo,
                            SubLevel = ct.SubLevel,
                            CompanyName = c.CompanyName
                        };

            var subList = query.ToList();
            List<ContractorListModel> list = new List<ContractorListModel>();
            list = GetOrderedSubcontractors(subList, subList, list, 0);

            string companyWithWrongDates = "";
            List<DocumentRowModel> model = new List<DocumentRowModel>();
            foreach (var l in list)
            {
                DocumentRowModel drm = new DocumentRowModel
                {
                    ProjectID = (int)id,
                    ContractorID = l.ContractorID,
                    CompanyName = l.CompanyName,
                    SubLevel = l.SubLevel,
                    SubTo = l.SubTo,
                    SubToCompanyName = l.SubToCompanyName,
                    DocumentRows = new List<DocumentRow>()
                };

                var subToCompany = db.Contractors.FirstOrDefault(x => x.Id == l.SubTo);
                if (subToCompany != null)
                {
                    drm.SubToCompanyName = subToCompany.CompanyName;
                }

                DateTime dt1 = (l.StartDate == null) ? DateTime.Today : (DateTime)l.StartDate;
                DateTime dt2 = (l.EndDate == null) ? DateTime.Parse("12/31/" + DateTime.Today.Year) : (DateTime)l.EndDate;

                if (dt1 > dt2)
                {
                    companyWithWrongDates += "<li>" + drm.CompanyName + "</li>";
                }

                drm.StartYear = dt1.Year;
                drm.StartDate = dt1;
                drm.EndYear = dt2.Year;
                drm.EndDate = dt2;

                var list1 = db.DocumentFiles.Where(x => x.ContractorID == l.ContractorID
                        && x.DocumentType != "ListSub" && x.DocumentType != "NtceEEO");
                var list2 = db.DocumentFiles.Where(x => x.ContractorID == l.ContractorID && x.ProjectID == id
                        && (x.DocumentType == "ListSub" || x.DocumentType == "NtceEEO"));

                //Make sure the project files to appear, which were submitted before the project start date
                var first = list2.OrderBy(x => x.Year).FirstOrDefault();
                if (first != null && first.Year < drm.StartYear)
                    drm.StartYear = first.Year;

                var files = list1.Union(list2).OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.DateUploaded).ToList();

                for (int i = drm.StartYear; i <= drm.EndYear; i++)
                {
                    DocumentRow row = new DocumentRow { Year = i, ContractorID = l.ContractorID };
                    foreach (var f in files)
                    {
                        if (f.Year == i)
                        {
                            // Check any project of the contractor was cleared for the year
                            var projectIds = db.DocumentFiles
                                .Where(x => x.Year == i && x.ContractorID == l.ContractorID && x.ProjectID > 0)
                                .Select(x => x.ProjectID).ToArray();

                            var everClosed = db.Projects
                                .Where(x => projectIds.Any(y => y == x.Id) && x.DateClosed != null).FirstOrDefault();

                            foreach (var p in row.GetType().GetProperties())
                            {
                                if (f.DocumentType.Contains("EUR") && p.Name.Contains("EUR"))
                                {
                                    int month = int.Parse(p.Name.Replace("EUR", ""));
                                    if (f.Month == month)
                                    {
                                        string status = "";
                                        if (f.DateApproved == null && f.DateRejected == null)
                                        {
                                            status = "Received";
                                        }
                                        else if (f.DateApproved != null)
                                        {
                                            status = "Approved";
                                        }
                                        else if (f.DateRejected != null)
                                        {
                                            status = "Rejected";
                                        }

                                        if (everClosed != null)
                                        {
                                            // Make sure latest document is not rejected
                                            var latest = db.DocumentFiles
                                                .Where(x => x.DocumentType == f.DocumentType && x.Year == i && x.Month == f.Month && x.ContractorID == l.ContractorID)
                                                .OrderByDescending(x => x.DateUploaded).FirstOrDefault();

                                            if (latest.DateApproved != null && latest.DateRejected == null)
                                                status = "Cleared";
                                            else if (latest.DateApproved == null && latest.DateRejected == null)
                                                status = "Received";
                                            else if (latest.DateRejected != null)
                                                status = "Rejected";
                                        }

                                        DocumentCell cell = new DocumentCell { Name = p.Name, Status = status };
                                        row.GetType().GetProperty(p.Name).SetValue(row, cell);
                                    }
                                }
                                else if (f.DocumentType.Contains("WIBA") && p.Name.Contains("WIBA"))
                                {
                                    int month = int.Parse(p.Name.Replace("WIBA", ""));
                                    if (f.Month == month)
                                    {
                                        string status = "";
                                        if (f.DateApproved == null && f.DateRejected == null)
                                        {
                                            status = "Received";
                                        }
                                        else if (f.DateApproved != null)
                                        {
                                            status = "Approved";
                                        }
                                        else if (f.DateRejected != null)
                                        {
                                            status = "Rejected";
                                        }

                                        DocumentCell cell = new DocumentCell { Name = p.Name, Status = status };
                                        row.GetType().GetProperty(p.Name).SetValue(row, cell);
                                    }
                                }
                                else if (f.DocumentType == p.Name)
                                {
                                    string status = "";
                                    if (f.DateApproved == null && f.DateRejected == null)
                                    {
                                        status = "Received";
                                    }
                                    else if (f.DateApproved != null)
                                    {
                                        status = "Approved";
                                    }
                                    else if (f.DateRejected != null)
                                    {
                                        status = "Rejected";
                                    }

                                    if (!(f.DocumentType.Contains("ListSub") && p.Name.Contains("ListSub"))
                                        && !(f.DocumentType.Contains("NtceEEO") && p.Name.Contains("NtceEEO")))
                                    {
                                        if (everClosed != null)
                                        {
                                            // Make sure latest document is not rejected
                                            var latest = db.DocumentFiles
                                                .Where(x => x.DocumentType == f.DocumentType && x.Year == i && x.ContractorID == l.ContractorID)
                                                .OrderByDescending(x => x.DateUploaded).FirstOrDefault();

                                            if (latest.DateApproved != null && latest.DateRejected == null)
                                                status = "Cleared";
                                            else if (latest.DateApproved == null && latest.DateRejected == null)
                                                status = "Received";
                                            else if (latest.DateRejected != null)
                                                status = "Rejected";
                                        }
                                    }

                                    DocumentCell cell = new DocumentCell { Name = p.Name, Status = status };
                                    row.GetType().GetProperty(p.Name).SetValue(row, cell);
                                }
                            }
                        }
                    }

                    drm.DocumentRows.Add(row);
                }

                model.Add(drm);
            }

            if (!String.IsNullOrEmpty(companyWithWrongDates))
            {
                string msg = "Error: Please check the 'Start Date' and 'End Date' of following companies:";
                msg += "<ol>" + companyWithWrongDates + "</ol>";
                TempData["Message"] = msg;
            }

            return model;
        }

        [HttpPost]
        public ActionResult Details3(int id, string submit)
        {
            int contractorId = int.Parse(Request.Form["ContractorId"]);
            string type = Request.Form["Column"];
            int year = int.Parse(Request.Form["Year"]);
            int month = (string.IsNullOrEmpty(Request.Form["Month"])) ? 0 : int.Parse(Request.Form["Month"]);

            string message = "";
            int fileId;

            if (submit == "Reset")
            {
                var docFiles = _documentFileRepository.Table.Where(x => x.ContractorID == contractorId
                        && x.Year == year && x.Month == month && x.DocumentType == type).ToList();
                if (type == "ListSub" || type == "NtceEEO")
                {
                    docFiles = docFiles.Where(x => x.ProjectID == id).ToList();
                }

                foreach (var df in docFiles)
                {
                    _documentFileRepository.Delete(df);
                }

                return RedirectToAction("Details3");
            }
            else if (int.TryParse(submit, out fileId))
            {
                var docFile = _documentFileRepository.GetById(fileId);
                _documentFileRepository.Delete(docFile);

                return RedirectToAction("Details3");
            }

            if (Request.Files.Count == 0)
            {
                TempData["Message"] = "Error: Please select a file to upload.";

                return RedirectToAction("Details3");
            }

            var upload = Request.Files[0];
            if (upload == null || upload.ContentLength == 0)
            {
                TempData["Message"] = "Error: Please select a valid file to upload.";
                return RedirectToAction("Details3");
            }

            string dateReceived = (string.IsNullOrEmpty(Request.Form["DateReceived"])) ? DateTime.Today.ToShortDateString() : Request.Form["DateReceived"];

            var existingDoc = _documentFileRepository.Table.Where(x => x.DocumentType == type && x.ContractorID == contractorId
                && x.Year == year && x.DateApproved == null && x.DateRejected == null).ToList();
            if (type == "EUR" || type == "WIBA")
            {
                existingDoc = existingDoc.Where(x => x.Month == month).ToList();
            }
            else if (type == "ListSub" || type == "NtceEEO")
            {
                existingDoc = existingDoc.Where(x => x.ProjectID == id).ToList();
            }

            foreach (var ed in existingDoc)
            {
                _documentFileRepository.Delete(ed);
            }

            DocumentFile newFile = new DocumentFile
            {
                DateReceived = dateReceived,
                ContractorID = contractorId,
                Year = year,
                DocumentType = type,
                UploadedBy = UserProfile.UserInitial
            };
            if (type == "ListSub" || type == "NtceEEO")
            {
                newFile.ProjectID = id;
            }
            else if (type == "EUR" || type == "WIBA")
            {
                newFile.Month = month;
            }

            _documentFileRepository.Insert(newFile);

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(upload.FileName);
                fileName = fileName.RemoveSpecialCharacters();
                string extension = Path.GetExtension(upload.FileName);
                fileName = string.Concat(id.ToString(), "_", contractorId, "_", fileName, extension);
                string relativePath = string.Concat("~/Files/", GetPath(type));
                var pathName = string.Concat(Server.MapPath("~/Files/"), GetPath(type));
                var path = Path.Combine(pathName, fileName);
                upload.SaveAs(path);

                DocumentFile file = _documentFileRepository.GetById(newFile.Id);

                file.FileName = fileName;
                file.FilePath = relativePath + fileName;
                file.DateUploaded = DateTime.Now;

                _documentFileRepository.Update(file);

                message = "File was uploaded successfully.";
            }
            catch (Exception ex)
            {
                message = "Error: " + ex.Message;
            }

            return RedirectToAction("Details3");
        }

        private void UpdatePastDueDocument(int projectId, int contractorId, int year, int month, string type, DateTime dateApproved)
        {
            var docs = db.NonCompliances.Where(x => x.ProjectID == projectId && x.ContractorID == contractorId && x.DocumentType == type);
            if (type == "NtceEEO")
            {
                var doc = docs.FirstOrDefault();

                if (doc != null)
                {
                    doc.DateReceived = dateApproved;
                    db.Entry(doc).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            else if (type == "EUR")
            {
                var doc = docs.FirstOrDefault(x => x.Year == year && x.Month == month);

                if (doc != null)
                {
                    doc.DateReceived = dateApproved;
                    db.Entry(doc).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            else
            {
                var doc = docs.FirstOrDefault(x => x.Year == year);

                if (doc != null)
                {
                    doc.DateReceived = dateApproved;
                    db.Entry(doc).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        private string GetPath(string type)
        {
            string path = "/";

            if (type.Contains("EUR"))
            {
                path = "EUR/";
            }
            else
            {
                switch (type)
                {
                    case "PrevPerf":
                        path = "Previous_Performance/";
                        break;
                    case "NonSeg":
                        path = "Non_Segregated_Certification/";
                        break;
                    case "GFE":
                        path = "GFE/";
                        break;
                    case "ListSub":
                        path = "List_of_Subcontractors/";
                        break;
                    case "NtceEEO":
                        path = "Notice_of_EEO/";
                        break;
                    case "WIBA":
                        path = "WIBA/";
                        break;
                }
            }

            return path;
        }

        public ActionResult Details4(int? id, string message = "")
        {
            var list = from i in db.Inspections
                       join c in db.Contractors on i.ContractorId equals c.Id
                       join p in db.Projects on i.ProjectId equals p.Id
                       where p.Id == id
                       select new InspectionListModel
                       {
                           InspectionID = i.Id,
                           ProjectID = i.ProjectId,
                           JOC = p.JOC,
                           ContractorID = i.ContractorId,
                           CompanyName = c.CompanyName,
                           DateApproved = i.DateApproved,
                           DateCancelled = i.DateCancelled,
                           DateOfVisit = i.DateOfVisit,
                           DCO = i.DCO,
                           Address = i.Address,
                           City = i.City,
                           State = i.State,
                           Zip = i.Zip,
                           NumberInterviews = i.NumberInterviews,
                           Violations = i.Violations,
                           PhotosTaken = i.PhotosTaken,
                           MilesOneWay = i.MilesOneWay,
                           MilesToEastern = i.MilesToEastern,
                           RoundTripMiles = i.RoundTripMiles,
                           RoundTripHours = i.RoundTripHours
                       };

            List<InspectionListModel> model = list.OrderBy(x => x.DateOfVisit).ToList();

            foreach (var m in model)
            {
                var ct = db.Contracts.FirstOrDefault(x => x.ProjectId == m.ProjectID && x.ContractorId == m.ContractorID);
                if (ct != null)
                {
                    m.PS = (ct.SubTo == 0) ? "P" : "S";
                }
            }

            ViewBag.ProjectID = id;
            ViewBag.CO = UserProfile.UserInitial;

            return View(model);
        }

        public ActionResult Details5(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var model = (from c in db.ClearanceRequests
                         join l in db.ClearanceRequestLogs on c.Id equals l.ClearanceRequestId
                         where c.ProjectId == id && !c.CurrentStatus.StartsWith("Deleted")
                         select l).ToList();

            ViewBag.ProjectID = id;

            ViewBag.EnableDocument = false;
            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id && !x.CurrentStatus.StartsWith("Deleted"));

            if (request != null)
            {
                ViewBag.CurrentStatus = request.CurrentStatus;

                if (User.IsInRole("DCO"))
                {
                    ViewBag.Approved = (request.CurrentStatus.Contains("Approved"));
                }

                if (!String.IsNullOrEmpty(request.RequestedBy) && !request.RequestedBy.Contains("System"))
                    ViewBag.EnableDocument = true;

                string filePath = Server.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".PDF";
                if (System.IO.File.Exists(filePath))
                {
                    string url = Url.Content("/Files/Clearance_Request/Forms/ClearanceRequest_" + id.ToString() + ".pdf");
                    ViewBag.FileUrl = url;
                }
            }

            if (User.IsInRole("DCO"))
                ViewBag.CO = UserProfile.UserInitial;

            return View(model);
        }

        [HttpPost]
        public ActionResult Details5(FormCollection form, Review review)
        {
            int id = int.Parse(form["projectID"]);
            string action = form["confirm"];
            string comment = form["Comment"];

            if (action == "Confirm")
            {
                DateTime dateClosed = DateTime.Today;
                DateTime.TryParse(form["DateClosed"], out dateClosed);
                return AdministrativeClosing(id, dateClosed, comment);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    string answer = form["answer"];
                    string dt = form["DateClosed"];
                    DateTime now = (String.IsNullOrEmpty(dt)) ? DateTime.Now : DateTime.Parse(dt);

                    var reqForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);
                    if (action == "Send to Department")
                    {
                        Project project = db.Projects.Find(id);

                        project.DateClosed = now;
                        db.Entry(project).State = EntityState.Modified;

                        if (reqForm == null)
                        {
                            reqForm = CommonHelper.NewCrForm(id);
                            reqForm.SmDate = now;
                        }
                        else
                        {
                            reqForm.SmDate = now;
                            db.Entry(reqForm).State = EntityState.Modified;
                        }
                    }
                    else if (action.Contains("to Manager"))
                    {
                        if (reqForm == null)
                        {
                            reqForm = CommonHelper.NewCrForm(id);
                            if (answer == "Yes")
                                reqForm.IsCleared = 1;
                            else if (answer == "No")
                                reqForm.IsCleared = 2;
                            reqForm.Comments = comment;
                        }
                        else
                        {
                            if (answer == "Yes")
                                reqForm.IsCleared = 1;
                            else if (answer == "No")
                                reqForm.IsCleared = 2;
                            reqForm.Comments = comment;
                            db.Entry(reqForm).State = EntityState.Modified;
                        }
                    }

                    db.SaveChanges();

                    SendClearanceRequest(id, action, comment);

                    return RedirectToAction("Details5", new { id = id });
                }
            }
            catch (Exception ex)
            {

            }

            ViewBag.Hours = CommonHelper.GetWorksheetHours(id);

            return View(review);
        }

        private ActionResult AdministrativeClosing(int id, DateTime dateClosed, string comment)
        {
            DateTime now = DateTime.Now;
            Project project = db.Projects.Find(id);

            project.DateClosed = dateClosed;
            db.Entry(project).State = EntityState.Modified;
            db.SaveChanges();

            string status = "Closed by Administrator";
            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);
            int clearanceRequestId;
            if (request != null)
            {
                clearanceRequestId = request.Id;
                request.DateModified = now;
                request.CurrentStatus = status;

                var reqForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);
                if (reqForm == null)
                {
                    reqForm = CommonHelper.NewCrForm(id);
                    reqForm.IsCleared = 1;
                    reqForm.Comments = CommonHelper.GetCrComment(id);
                }
                else
                {
                    reqForm.IsCleared = 1;
                    reqForm.Comments = CommonHelper.GetCrComment(id);
                    db.Entry(reqForm).State = EntityState.Modified;
                }
                db.SaveChanges();
            }
            else
            {
                ClearanceRequest cr = new ClearanceRequest
                {
                    ProjectId = id,
                    ProcessedBy = "Administrator",
                    DateRequested = now,
                    CurrentStatus = status
                };

                db.ClearanceRequests.Add(cr);
                db.SaveChanges();

                clearanceRequestId = cr.Id;
            }

            ClearanceRequestLog log = new ClearanceRequestLog
            {
                ClearanceRequestId = clearanceRequestId,
                Activity = status,
                Date = now
            };

            if (!string.IsNullOrEmpty(comment))
            {
                log.Comment = comment;
            }
            db.ClearanceRequestLogs.Add(log);
            db.SaveChanges();

            TempData["Message"] = "Clearance Request was closed successfully.";

            return RedirectToAction("Details5", new { id = id });
        }

        public ActionResult Details6(int id)
        {
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                review = new Review
                {
                    Project = db.Projects.Find(id),
                    ProjectID = (int)id,
                    CheckItem1 = -1,
                    CheckItem2 = -1,
                    CheckItem3 = -1,
                    CheckItem4 = -1,
                    CheckItem5 = -1,
                    CheckItem6 = -1,
                    CheckItem7 = -1,
                    CheckItem8 = -1
                };

                db.Reviews.Add(review);
                db.SaveChanges();
            }

            ViewBag.Hours = CommonHelper.GetWorksheetHours(id);
            ViewBag.Approved = CommonHelper.IsProjectApproved(id);
            var contractor = db.Contractors.FirstOrDefault(x => x.Id == review.Project.PrimeContractorID);
            if (contractor != null)
                ViewBag.CompanyName = contractor.CompanyName;

            if (review.Project.NumberSubcontractors == null)
                review.Project.NumberSubcontractors = 0;

            return View(review);
        }

        [HttpPost]
        public ActionResult Details6(FormCollection form, Review review)
        {
            int id = review.ProjectID;

            try
            {
                if (ModelState.IsValid)
                {
                    review.DateLastUpdated = DateTime.Now;
                    db.Entry(review).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["Message"] = "Analysis was saved successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error while saving analysis.";
            }

            review.Project = db.Projects.Find(id);
            ViewBag.Hours = CommonHelper.GetWorksheetHours(id);

            ViewBag.Approved = CommonHelper.IsProjectApproved(id);

            return View(review);
        }

        public ActionResult ViewAnalysisClose(int id)
        {
            string filePath = CommonHelper.CreateAnalysisClose(id);

            return Redirect(filePath);
        }

        public void SendClearanceRequest(int id, string action = "", string comment = "")
        {
            DateTime now = DateTime.Now;
            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);
            request.DateModified = now;

            ClearanceRequestLog log = new ClearanceRequestLog
            {
                ClearanceRequestId = request.Id,
                Date = now
            };
            if (!string.IsNullOrEmpty(comment))
            {
                log.Comment = comment;
            }

            if (User.IsInRole("Administrator") || User.IsInRole("Manager"))
            {
                if (action == "Reject")
                {
                    string status = "Rejected by Manager";
                    request.CurrentStatus = status;
                    log.Activity = status;
                    TempData["Message"] = "Clearance Request was by manager and sent to CO.";
                }
                else
                {
                    string status = "Sent to Department";
                    request.CurrentStatus = status;
                    request.ProcessedBy = UserProfile.UserInitial;

                    log.Activity = status;
                    TempData["Message"] = "Clearance Request was sent to department successfully.";
                }
            }
            else if (User.IsInRole("DCO"))
            {
                string status = "Sent to Manager";
                request.CurrentStatus = status;
                log.Activity = status;

                var project = db.Projects.Find(id);
                project.DateClosed = now;
                db.Entry(project).State = EntityState.Modified;

                TempData["Message"] = "Clearance Request was sent to manager successfully.";
            }

            db.Entry(request).State = EntityState.Modified;
            db.ClearanceRequestLogs.Add(log);

            db.SaveChanges();

            string filePath = HostingEnvironment.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".PDF";
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                filePath = CommonHelper.CreateCR(id);
                CommonHelper.Upload(filePath);
            }
        }

        public ActionResult Details7(int id)
        {
            var model = db.Projects.FirstOrDefault(x => x.Id == id);

            ViewBag.ProjectID = id;

            return View(model);
        }

        public ActionResult Details8(int id)
        {
            var model = db.Projects.FirstOrDefault(x => x.Id == id);

            var query = from c1 in db.Contractors
                        join ct in db.Contracts on c1.Id equals ct.ContractorId
                        join c2 in db.Contractors on ct.SubTo equals c2.Id into temp
                        from t in temp.DefaultIfEmpty()
                        where ct.ProjectId == id
                        select new ContractorListModel
                        {
                            ContractID = ct.Id,
                            ContractorID = ct.ContractorId,
                            SubTo = ct.SubTo,
                            SubLevel = ct.SubLevel,
                            SubToCompanyName = t.CompanyName,
                            CompanyName = c1.CompanyName,
                            StartDate = ct.StartDate,
                            EndDate = ct.EndDate,
                            ContractAmount = ct.ContractAmount
                        };

            var subTo = new List<SelectListItem>();
            string companyWithWrongDates = "";
            foreach (var c in query.OrderBy(x => x.CompanyName))
            {
                subTo.Add(new SelectListItem { Text = c.CompanyName, Value = c.ContractorID.ToString() });
                if (c.StartDate > c.EndDate)
                {
                    companyWithWrongDates += "<li>" + c.CompanyName + "</li>";
                }
            }
            if (!String.IsNullOrEmpty(companyWithWrongDates))
            {
                string msg = "Error: Please check the 'Start Date' and 'End Date' of following companies:";
                msg += "<ol>" + companyWithWrongDates + "</ol>";
                TempData["Message"] = msg;
            }

            ViewBag.SubToList = subTo;
            ViewBag.Prime = model.PrimeContractorID;

            var subList = query.Where(x => x.SubTo != 0).ToList();
            List<ContractorListModel> orderedList = new List<ContractorListModel>();
            ViewBag.Subcontractors = GetOrderedSubcontractors(subList, subList, orderedList, 1);

            //TODO: StartDate & EndDate are required
            try
            {
                model.NumberSubcontractors = subList.Count;
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                string str = ex.Message;
            }

            ViewBag.ProjectID = id;

            return View(model);
        }

        public ActionResult Details9(int id)
        {
            var model = (from mt in db.MessageThreads
                         join m in db.Messages on mt.Id equals m.ThreadId
                         where mt.ProjectId == id
                         orderby m.DateSent
                         select new MessageListModel
                         {
                             ThreadId = mt.Id,
                             Subject = mt.Subject,
                             ProjectId = mt.ProjectId,
                             JOC = mt.JOC,
                             DateSent = m.DateSent,
                             Sender = m.Sender,
                             Recipient = m.Recipient,
                             Text = m.Text
                         }).ToList();

            ViewBag.ProjectID = id;

            return View(model);
        }


        private List<ContractorListModel> GetOrderedSubcontractors(List<ContractorListModel> list,
            List<ContractorListModel> subList, List<ContractorListModel> orderedList, int level)
        {
            var levelList = subList.Where(x => x.SubLevel == level).OrderBy(x => x.CompanyName).ToList(); ;
            foreach (var c in levelList)
            {
                orderedList.Add(c);

                subList = list.Where(x => x.SubLevel == c.SubLevel + 1 && x.SubTo == c.ContractorID).ToList();

                if (subList.Count > 0)
                {
                    orderedList = GetOrderedSubcontractors(list, subList, orderedList, c.SubLevel + 1);
                }
            }

            return orderedList;
        }

        // GET: Project/Create
        public ActionResult Create(int? id, string returnUrl = "")
        {
            if (id != null)
            {
                ViewBag.ContractorId = id;
            }

            GetViewBags();
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.PrimeContractors = GetPrimeContractors();

            var model = new Project();

            return View(model);
        }

        // POST: Project/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection form, Project project, string returnUrl = "")
        {
            try
            {
                var p = db.Projects.FirstOrDefault(x => x.JOC == project.JOC);
                if (p != null)
                {
                    TempData["Message"] = "Error: Entered project ID already exists.";

                    return RedirectToAction("Create", "Project", new { returnUrl = returnUrl });
                }

                if (ModelState.IsValid)
                {
                    if (!string.IsNullOrEmpty(form["primeContractor"]))
                        project.PrimeContractorID = int.Parse(form["primeContractor"]);

                    if (!string.IsNullOrEmpty(form["ContractAmount"]))
                        project.ContractAmount = decimal.Parse(form["ContractAmount"]);

                    project.LastUpdateDate = DateTime.Now;

                    List<string> warnings = GetProjectWarnings(project);
                    if (warnings.Any())
                    {
                        string msg = "Error: <ul>";
                        foreach (string w in warnings)
                        {
                            msg += "<li>" + w + "</li>";
                        }
                        msg += "</ul>";
                        TempData["Message"] = msg;
                        GetViewBags();
                        ViewBag.ReturnUrl = returnUrl;

                        return View(project);
                    }
                    else
                    {
                        project.HoursRemaining = project.HoursAvailable;
                        project.ProjectType = (project.Phase == "B") ? ProjectType.Capital : ProjectType.NonCapital;

                        db.Projects.Add(project);
                        db.SaveChanges();
                    }

                    Contract contract = new Contract
                    {
                        ProjectId = project.Id,
                        ContractorId = project.PrimeContractorID,
                        SubTo = 0,
                        DateRegistered = DateTime.Now
                    };

                    if (project.StartDate != null)
                    {
                        contract.StartDate = (DateTime)project.StartDate;
                    }

                    if (project.EndDate != null)
                    {
                        contract.EndDate = (DateTime)project.EndDate;
                    }

                    db.Contracts.Add(contract);
                    db.SaveChanges();

                    return RedirectToAction("Details1", new { id = project.Id });
                }
            }
            catch (Exception ex)
            {

            }

            ViewBag.PrimeContractors = GetPrimeContractors();
            ViewBag.Phases = GetPhases();

            return View(project);
        }

        // GET: Project/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            GetViewBags();

            return View(project);
        }

        // POST: Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FormCollection form, string returnUrl = "")
        {
            int projectId = int.Parse(form["Id"]);
            Project project = db.Projects.FirstOrDefault(x => x.Id == projectId);

            if (ModelState.IsValid)
            {
                if (!User.IsInRole("DCO"))
                {
                    return Edit1(project, form, returnUrl);
                }
                else
                {
                    return Edit2(project, form, returnUrl);
                }
            }

            GetViewBags();

            return View(project);
        }

        private ActionResult Edit1(Project project, FormCollection form, string returnUrl)
        {
            DateTime dateReceived, startDate, endDate;
            if (DateTime.TryParse(form["DateReceived"], out dateReceived))
                project.DateReceived = dateReceived;
            else
                project.DateReceived = null;

            if (DateTime.TryParse(form["StartDate"], out startDate))
                project.StartDate = startDate;
            else
                project.StartDate = null;

            if (DateTime.TryParse(form["EndDate"], out endDate))
                project.EndDate = endDate;
            else
                project.EndDate = null;

            Decimal amount;
            if (Decimal.TryParse(form["ContractAmount"], out amount))
                project.ContractAmount = amount;
            else
                project.ContractAmount = null;

            // If prime contractor changes, update the contract too.
            if (!string.IsNullOrEmpty(form["PrimeContractorID"]))
            {
                int primeId = int.Parse(form["PrimeContractorID"]);

                if (primeId == project.PrimeContractorID)
                {
                    project.PrimeContractorID = primeId;

                    var primeContract = db.Contracts.FirstOrDefault(x => x.ProjectId == project.Id && x.SubTo == 0);

                    primeContract.ContractAmount = project.ContractAmount;
                    primeContract.StartDate = project.StartDate;
                    primeContract.EndDate = project.EndDate;

                    db.Entry(primeContract).State = EntityState.Modified;
                }
                else
                {
                    project.PrimeContractorID = primeId;

                    var primeContract = db.Contracts.FirstOrDefault(x => x.ProjectId == project.Id && x.SubTo == 0);
                    if (primeContract != null)
                    {
                        db.Contracts.Remove(primeContract);
                    }

                    Contract newContract = new Contract
                    {
                        ProjectId = project.Id,
                        ContractorId = primeId,
                        SubTo = 0,
                        ContractAmount = project.ContractAmount,
                        StartDate = project.StartDate,
                        EndDate = project.EndDate,
                        DateRegistered = DateTime.Now
                    };
                    db.Contracts.Add(newContract);
                }

                db.SaveChanges();
            }

            project.JOC = form["JOC"];
            project.ProjectName = form["ProjectName"];

            if (String.IsNullOrEmpty(form["DateClosed"]))
            {
                project.DateClosed = null;
            }
            else
            {
                DateTime closedDate;
                if (DateTime.TryParse(form["DateClosed"], out closedDate))
                {
                    project.DateClosed = closedDate;
                }
            }

            project.FederalFunds = (form["FederalFunds"].Contains("true"));

            project.Address = form["Address"];
            project.City = form["City"];
            project.Zip = form["Zip"];

            project.Unit = form["Unit"];
            project.Phase = form["Phase"];
            project.ProjectType = (form["Phase"] == "B") ? ProjectType.Capital : ProjectType.NonCapital;

            decimal hoursAvailable;
            if (decimal.TryParse(form["HoursAvailable"], out hoursAvailable))
            {
                decimal hoursCharged = 0.0m;
                var hours = db.Worksheets.Where(x => x.ProjectId == project.Id);
                foreach (var h in hours)
                {
                    hoursCharged += h.Hours + h.Minutes / 60.0m;
                }

                project.HoursAvailable = hoursAvailable;
                project.HoursRemaining = hoursAvailable - hoursCharged;
            }

            project.DCO = form["DCO"];
            project.LastUpdateDate = DateTime.Now;
            project.DepartmentID = form["DepartmentID"];

            List<string> warnings = GetProjectWarnings(project);
            if (warnings.Any())
            {
                string msg = "Error: <ul>";
                foreach (string w in warnings)
                {
                    msg += "<li>" + w + "</li>";
                }
                msg += "</ul>";
                TempData["Message"] = msg;
                GetViewBags();

                return View(project);
            }
            else
            {
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
            }

            if (String.IsNullOrEmpty(returnUrl))
            {
                TempData["Message"] = "Data was saved successfully.";

                return RedirectToAction("Details1", new { id = project.Id });
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        private ActionResult Edit2(Project project, FormCollection form, string returnUrl)
        {
            DateTime startDate, endDate;

            if (DateTime.TryParse(form["StartDate"], out startDate))
                project.StartDate = startDate;
            else
                project.StartDate = null;

            if (DateTime.TryParse(form["EndDate"], out endDate))
                project.EndDate = endDate;
            else
                project.EndDate = null;

            project.DCO = form["DCO"];
            project.LastUpdateDate = DateTime.Now;

            List<string> warnings = GetProjectWarnings(project);
            if (warnings.Any())
            {
                string msg = "Error: <ul>";
                foreach (string w in warnings)
                {
                    msg += "<li>" + w + "</li>";
                }
                msg += "</ul>";
                TempData["Message"] = msg;
                GetViewBags();

                return View(project);
            }
            else
            {
                db.Entry(project).State = EntityState.Modified;

                var contract = db.Contracts.FirstOrDefault(x => x.ProjectId == project.Id);
                if (contract != null)
                {
                    contract.StartDate = project.StartDate;
                    contract.EndDate = project.EndDate;

                    db.Entry(contract).State = EntityState.Modified;
                }

                db.SaveChanges();
            }

            if (String.IsNullOrEmpty(returnUrl))
            {
                TempData["Message"] = "Data was saved successfully.";

                return RedirectToAction("Details1", new { id = project.Id });
            }
            else
            {
                return Redirect(returnUrl);
            }
        }


        private List<string> GetProjectWarnings(Project project)
        {
            var warnings = new List<string>();

            if (project.StartDate < DateTime.Today.AddYears(-10))
            {
                warnings.Add("Please check the start date.  It can't be more than 10 years ago.");
            }
            if (project.EndDate > DateTime.Today.AddYears(10))
            {
                warnings.Add("Please check the end date. It can't be more than 10 years from today.");
            }
            if (project.StartDate > project.EndDate)
            {
                warnings.Add("'Start Date' must be earlier than 'End Date'.");
            }

            return warnings;
        }

        // GET: Project/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Project/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Project project = db.Projects.Find(id);
            db.Projects.Remove(project);
            db.SaveChanges();

            return RedirectToAction("Index1");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Inspection(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        public ActionResult AddSubcontractor(int id)
        {
            Project model = db.Projects.Find(id);

            var query = from c1 in db.Contractors
                        join ct in db.Contracts on c1.Id equals ct.ContractorId
                        join c2 in db.Contractors on ct.SubTo equals c2.Id into temp
                        from t in temp.DefaultIfEmpty()
                        where ct.ProjectId == id
                        select new ContractorListModel
                        {
                            ContractID = ct.Id,
                            ContractorID = ct.ContractorId,
                            SubTo = ct.SubTo,
                            SubLevel = ct.SubLevel,
                            SubToCompanyName = t.CompanyName,
                            CompanyName = c1.CompanyName,
                            StartDate = ct.StartDate,
                            EndDate = ct.EndDate
                        };

            var subTo = new List<SelectListItem>();
            foreach (var c in query.OrderBy(x => x.CompanyName))
            {
                subTo.Add(new SelectListItem { Text = c.CompanyName, Value = c.ContractorID.ToString() });
            }

            ViewBag.SubToList = subTo;
            ViewBag.Prime = model.PrimeContractorID;

            var subList = query.Where(x => x.SubTo != 0).ToList();
            List<ContractorListModel> orderedList = new List<ContractorListModel>();
            ViewBag.Subcontractors = GetOrderedSubcontractors(subList, subList, orderedList, 1);

            var list = new List<ListItem>();
            list.Add(new ListItem { Text = "- Select a contractor -", Value = "" });

            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                var selected = subList.FirstOrDefault(x => x.ContractorID == c.Id);

                if (selected == null && c.Id.ToString() != model.PrimeContractorID.ToString())
                {
                    list.Add(new ListItem { Text = c.CompanyName, Value = c.Id.ToString() });
                }
            }
            ViewBag.Contractors = list;

            return View(model);
        }

        // Add Subcontractor from Project/Document 
        public ActionResult AddSubcontractor2(int id, int subTo, string returnUrl, int cid = 0)
        {
            Project model = db.Projects.Find(id);

            var subToList = new List<SelectListItem>();
            var contractor = db.Contractors.FirstOrDefault(x => x.Id == subTo);
            subToList.Add(new SelectListItem { Text = contractor.CompanyName, Value = contractor.Id.ToString() });

            ViewBag.SubToList = subToList;
            ViewBag.SubTo = subTo;

            var list = new List<ListItem>();
            list.Add(new ListItem { Text = "- Select a contractor -", Value = "" });

            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                list.Add(new ListItem { Text = c.CompanyName, Value = c.Id.ToString() });
            }
            ViewBag.Contractors = list;
            ViewBag.Contractor = cid;
            ViewBag.ReturnUrl = returnUrl;

            return View("AddSubcontractor", model);
        }


        [HttpPost]
        public ActionResult AddSubcontractor(FormCollection form, int id, string returnUrl = "")
        {
            returnUrl = returnUrl.ToLower();

            string contId = Request.Form["Contractors"];
            string subTo = Request.Form["SubTo"];

            if (!string.IsNullOrEmpty(contId))
            {
                if (contId == "new")
                {
                    string companyName = Request.Form["CompanyName"];

                    Contractor contractor = new Contractor
                    {
                        CompanyName = companyName,
                        DateRegistered = DateTime.Now
                    };

                    db.Contractors.Add(contractor);
                    db.SaveChanges();

                    contId = contractor.Id.ToString();
                }
                else if (form["confirm"] != "Confirm")
                {
                    int contractorId = int.Parse(contId);
                    var contract = db.Contracts.FirstOrDefault(x => x.ProjectId == id && x.ContractorId == contractorId);
                    if (contract != null)
                    {
                        var contractor = db.Contractors.Find(contractorId);
                        //TempData["Message"] = "Error: '" + contractor.CompanyName + "' is already a contractor for this project.";
                        string msg = contractor.CompanyName + "' is already a contractor for this project.  Are you sure to add this contractor again?";
                        msg += "<input type='submit' name='confirm' value='Confirm' class='btn btn-default mar-left-20' />";
                        TempData["Message"] = msg;

                        if (returnUrl.Contains("details3"))
                        {
                            return RedirectToAction("AddSubcontractor2", new { id = id, subTo = subTo, returnUrl = returnUrl, cid = contractorId });
                        }
                        else
                        {
                            return RedirectToAction("AddSubcontractor", new { id = contractorId });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(subTo))
                {
                    string startDate = Request.Form["StartDate"];
                    string endDate = Request.Form["EndDate"];
                    int subToId = int.Parse(subTo);
                    var sub = db.Contracts.FirstOrDefault(x => x.ProjectId == id && x.ContractorId == subToId);

                    Decimal amount = (string.IsNullOrEmpty(Request.Form["ContractAmount"])) ? 0.0m : Decimal.Parse(Request.Form["ContractAmount"]);

                    Contract contract = new Contract
                    {
                        ProjectId = id,
                        ContractorId = int.Parse(contId),
                        SubTo = subToId,
                        SubLevel = sub.SubLevel + 1,
                        ContractAmount = amount,
                        DateRegistered = DateTime.Now
                    };

                    DateTime dt;
                    if (DateTime.TryParse(startDate, out dt))
                        contract.StartDate = dt;
                    if (DateTime.TryParse(endDate, out dt))
                        contract.EndDate = dt;

                    db.Contracts.Add(contract);
                    db.SaveChanges();

                    UpdateNumberSubcontractors(id);
                }
            }

            if (String.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Details8", new { id });
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        public ActionResult DeleteContract(int id, int cId)
        {
            Contract contract = db.Contracts.Find(cId);
            db.Contracts.Remove(contract);
            db.SaveChanges();

            UpdateNumberSubcontractors(id);

            return RedirectToAction("Details1", new { id });
        }

        private void UpdateNumberSubcontractors(int id)
        {
            int numSub = db.Contracts.Where(x => x.ProjectId == id && x.SubTo != 0).Count();

            Project project = db.Projects.Find(id);
            project.NumberSubcontractors = numSub;
            db.Entry(project).State = EntityState.Modified;
            db.SaveChanges();
        }

        [HttpPost]
        public ActionResult AddComment(int id, string category = "")
        {
            Comment comment = new Comment
            {
                EntityId = (int)id,
                CommentText = Request.Form["Comment"],
                DateRegistered = DateTime.Now,
                Category = category,
                CommentedBy = UserProfile.UserInitial
            };
            db.Comments.Add(comment);
            db.SaveChanges();

            if (category == "CR")
            {
                return RedirectToAction("Details5", "Project", new { id = id });
            }

            return RedirectToAction("Details1", "Project", new { id = id });
        }

        [HttpPost]
        public ActionResult EditComment(int id, string category = "")
        {
            int commentId = int.Parse(Request.Form["CommentID"]);

            Comment comment = db.Comments.Find(commentId);
            comment.CommentText = Request.Form["EditComment"];
            db.Entry(comment).State = EntityState.Modified;
            db.SaveChanges();

            if (category == "CR")
            {
                return RedirectToAction("Details5", "Project", new { id = id });
            }

            return RedirectToAction("Details1", "Project", new { id = id });
        }

        public ActionResult DeleteComment(int id, int cId)
        {
            Comment comment = db.Comments.Find(cId);
            db.Comments.Remove(comment);
            db.SaveChanges();

            if (comment.Category == "CR")
            {
                return RedirectToAction("Details5", "Project", new { id = id });
            }

            return RedirectToAction("Details1", "Project", new { id = id });
        }

        #region "Partial Views"

        public PartialViewResult PageHeader(int id)
        {
            var model = db.Projects.Find(id);

            return PartialView("_PageHeader", model);
        }

        public PartialViewResult PrimeContractor(int id, int projectId)
        {
            Contractor contractor = db.Contractors.Find(id);

            ViewBag.ProjectID = projectId;
            ViewBag.ContractContacts = db.ContractorContacts.Where(x => x.ContractorId == id).ToList();

            return PartialView("_PrimeContractor", contractor);
        }

        public PartialViewResult ServiceRequest(int id)
        {
            Document model = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == "SR");

            ViewBag.ProjectID = id;

            return PartialView("_ServiceRequest", model);
        }

        public PartialViewResult GetDocument(int id, string type)
        {
            if (type == "Misc")
            {
                List<Document> model = new List<Document>();
                model = db.Documents.Where(x => x.ProjectID == id && x.DocumentName == type).ToList();

                ViewBag.ProjectID = id;
                return PartialView("_Misc", model);
            }
            else
            {
                var model = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == type);
                if (model == null)
                {
                    model = new Document { ProjectID = id };
                }

                if (type == "NTP")
                    return PartialView("_NoticeToProceed", model);
                else if (type == "WOA")
                    return PartialView("_WorkOrderAuthorization", model);
                else if (type == "BL")
                    return PartialView("_BoardLetters", model);
                else
                    return null;
            }

        }

        public PartialViewResult NoticeHistory(int id, string category = "")
        {
            var model = db.EmailLogs.Where(x => x.ProjectId == id).ToList();
            ViewBag.ProjectID = id;

            if (string.IsNullOrEmpty(category))
            {
                model = model.Where(x => x.NoticeType != "Clearance Request" && x.NoticeType != "Clearance Request Confirmation").ToList();
            }
            else
            {
                model = model.Where(x => x.NoticeType == category).ToList();
            }

            return PartialView("_NoticeHistory", model);
        }

        public PartialViewResult Preconstruction(int id)
        {
            Document model = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == "Preconstruction");
            ViewBag.Logs = db.EmailLogs.Where(x => x.ProjectId == id).ToList();
            ViewBag.ProjectID = id;

            return PartialView("_Preconstruction", model);
        }
        #endregion

        private void GetViewBags()
        {
            var types = new List<ListItem>();
            types.Add(new ListItem { Text = "Capital & Non-Capital", Value = "" });
            types.Add(new ListItem { Text = "Capital", Value = "0" });
            types.Add(new ListItem { Text = "Non-Capital", Value = "1" });
            ViewBag.ProjectTypes = types;

            ViewBag.COs = GetCOs();

            var statusList = new List<ListItem>();
            statusList.Add(new ListItem { Text = "- All Projects -", Value = "" });
            statusList.Add(new ListItem { Text = "No Request", Value = "0" });
            statusList.Add(new ListItem { Text = "Delievered to CO", Value = "1" });
            statusList.Add(new ListItem { Text = "Sent to Manager", Value = "2" });
            statusList.Add(new ListItem { Text = "Sent to Department", Value = "3" });
            ViewBag.StatusList = statusList;

            var departments = new List<ListItem>();
            departments.Add(new ListItem { Text = "- Select department -", Value = "" });
            foreach (var d in db.Departments.OrderBy(x => x.DepartmentName))
            {
                departments.Add(new ListItem { Text = d.DepartmentName, Value = d.DepartmentId });
            }
            ViewBag.Departments = departments;

            ViewBag.Phases = GetPhases();

            var contractors = new List<ListItem>();
            contractors.Add(new ListItem { Text = "- Select prime contractor -", Value = "" });
            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                contractors.Add(new ListItem { Text = c.CompanyName, Value = c.Id.ToString() });
            }
            ViewBag.PrimeContractors = contractors;

            ViewBag.Feds = GetFeds();

            ViewBag.Amounts = GetAmounts();
        }

        private List<SelectListItem> GetPrimeContractors()
        {
            var contractors = new List<SelectListItem>();
            contractors.Add(new SelectListItem { Text = "- Select prime contractor -", Value = "" });

            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                contractors.Add(new SelectListItem { Text = c.CompanyName, Value = c.Id.ToString() });
            }

            return contractors;
        }

        private List<SelectListItem> GetPhases()
        {
            var phases = new List<SelectListItem>();

            phases.Add(new SelectListItem { Text = "- Select phase -", Value = "" });
            phases.Add(new SelectListItem { Text = "A", Value = "A" });
            phases.Add(new SelectListItem { Text = "B", Value = "B" });
            phases.Add(new SelectListItem { Text = "C", Value = "C" });
            phases.Add(new SelectListItem { Text = "P", Value = "P" });

            return phases;
        }

        private List<SelectListItem> GetFeds()
        {
            var feds = new List<SelectListItem>();

            feds.Add(new SelectListItem { Text = "Fed & Non-Fed", Value = "" });
            feds.Add(new SelectListItem { Text = "Fed", Value = "Fed" });
            feds.Add(new SelectListItem { Text = "Non-Fed", Value = "NonFed" });

            return feds;
        }

        [HttpPost]
        public ActionResult EditSubcontractor(FormCollection form, int id)
        {
            int subTo = int.Parse(form["SubTo"]);
            int contractId = int.Parse(form["ContractId"]);

            var contract = db.Contracts.FirstOrDefault(x => x.Id == contractId);

            if (contract != null)
            {
                var parent = db.Contracts.FirstOrDefault(x => x.ContractorId == subTo && x.ProjectId == id);

                contract.SubTo = subTo;
                contract.SubLevel = parent.SubLevel + 1;

                if (!string.IsNullOrEmpty(form["StartDate"]))
                    contract.StartDate = DateTime.Parse(form["StartDate"]);

                if (!string.IsNullOrEmpty(form["EndDate"]))
                    contract.EndDate = DateTime.Parse(form["EndDate"]);

                if (!string.IsNullOrEmpty(form["ContractAmount"]))
                    contract.ContractAmount = Decimal.Parse(form["ContractAmount"]);

                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Details8", new { id = id });
        }

        public ActionResult DeleteSubcontractor(int pId, int cId)
        {
            var contractor = db.Contracts.FirstOrDefault(x => x.ProjectId == pId && x.ContractorId == cId);

            if (contractor != null)
            {
                RemoveSubs(pId, cId);

                db.Contracts.Remove(contractor);
                db.SaveChanges();
            }

            //deletes document files
            var documents = db.DocumentFiles.Where(x => x.ProjectID == pId && x.ContractorID == cId).ToList();
            foreach (var d in documents)
            {
                db.DocumentFiles.Remove(d);
                db.SaveChanges();
            }

            return RedirectToAction("Details8", new { id = pId });
        }

        public ActionResult DeleteClearanceRequestLog(int crId, int id, string returnUrl)
        {
            var cr = db.ClearanceRequests.FirstOrDefault(x => x.Id == crId);
            var crLog0 = db.ClearanceRequestLogs.FirstOrDefault(x => x.Id == id);
            var crLogs = db.ClearanceRequestLogs.Where(x => x.ClearanceRequestId == crId && x.Id < id).ToList();

            if (cr != null)
            {
                db.ClearanceRequestLogs.Remove(crLog0);

                if (crLogs.Count == 0)
                {
                    db.ClearanceRequests.Remove(cr);

                    var crForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == cr.ProjectId);
                    if (crForm != null)
                        db.ClearanceRequestForms.Remove(crForm);
                }
                else
                {
                    ClearanceRequestLog prevLog = crLogs.OrderByDescending(x => x.Date).First();
                    string status = prevLog.Activity;
                    DateTime now = DateTime.Now;

                    cr.CurrentStatus = status;
                    cr.DateModified = now;
                    cr.ProcessedBy = UserProfile.UserInitial;
                    db.Entry(cr).State = EntityState.Modified;

                    if (crLog0.Activity.Contains("to Department"))
                    {
                        var project = db.Projects.FirstOrDefault(x => x.Id == cr.ProjectId);
                        project.DateClosed = null;
                        db.Entry(project).State = EntityState.Modified;
                    }
                    else if (crLog0.Activity.Contains("by Manager"))
                    {
                        var messageThread = db.MessageThreads.FirstOrDefault(x => x.ProjectId == cr.ProjectId);

                        if (messageThread != null)
                        {
                            var messages = db.Messages.Where(x => x.ThreadId == messageThread.Id).ToList();
                            foreach (var m in messages)
                            {
                                db.Messages.Remove(m);
                            }

                            db.MessageThreads.Remove(messageThread);
                        }
                    }
                }

                if (crLog0.Activity.Contains("Closed by Administrator"))
                {
                    var project = db.Projects.FirstOrDefault(x => x.Id == cr.ProjectId);
                    project.DateClosed = null;
                    db.Entry(project).State = EntityState.Modified;
                }

                db.SaveChanges();
            }

            return Redirect(returnUrl);
        }

        private void RemoveSubs(int pId, int cId)
        {
            var subs = db.Contracts.Where(x => x.ProjectId == pId && x.SubTo == cId).ToList();

            foreach (var s in subs)
            {
                var sub = db.Contracts.FirstOrDefault(x => x.ProjectId == pId && x.Id == s.Id);
                db.Contracts.Remove(sub);
                db.SaveChanges();

                RemoveSubs(pId, s.Id);
            }
        }

        public PartialViewResult ContactInfo(int id)
        {
            var model = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == id);
            if (model == null)
            {
                model = new ProjectContact { ProjectId = id };
            }

            return PartialView("_ContactInfo", model);
        }

        public ActionResult EditContact(int id, string message = "")
        {
            var model = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == id);
            if (model == null)
            {
                model = new ProjectContact { ProjectId = id, DeptId = "ALL" };
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult EditContact(ProjectContact contact)
        {
            if (ModelState.IsValid)
            {
                var ct = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == contact.ProjectId);
                if (ct == null)
                {
                    db.ProjectContacts.Add(contact);
                }
                else
                {
                    ct.DeptContact = contact.DeptContact;
                    ct.DeptContactEmail = contact.DeptContactEmail;
                    ct.DeptContactPhoneNumber = contact.DeptContactPhoneNumber;
                    ct.DeptContactExtension = contact.DeptContactExtension;

                    ct.Analyst = contact.Analyst;
                    ct.AnalystEmail = contact.AnalystEmail;
                    ct.AnalystPhoneNumber = contact.AnalystPhoneNumber;
                    ct.AnalystExtension = contact.AnalystExtension;

                    ct.ProjectManager = contact.ProjectManager;
                    ct.ProjectManagerEmail = contact.ProjectManagerEmail;
                    ct.ProjectManagerPhoneNumber = contact.ProjectManagerPhoneNumber;
                    ct.ProjectManagerExtension = contact.ProjectManagerExtension;

                    ct.Contractor = contact.Contractor;
                    ct.ContractorEmail = contact.ContractorEmail;
                    ct.ContractorPhoneNumber = contact.ContractorPhoneNumber;
                    ct.ContractorExtension = contact.ContractorExtension;

                    db.Entry(ct).State = EntityState.Modified;
                }
                db.SaveChanges();
            }

            return RedirectToAction("Details1", new { id = contact.ProjectId });
        }

        public string GetFiles(int id, int cid, int year, int? month, string type)
        {
            var files = _documentFileRepository.Table.Where(x => x.ContractorID == cid && x.Year == year && x.DocumentType == type).ToList();
            if (type == "ListSub" || type == "NtceEEO")
            {
                files = files.Where(x => x.ProjectID == id).ToList();
            }
            else if (type == "EUR")
            {
                files = files.Where(x => x.Month == month).ToList();
            }

            string result = "<hr/><table class='table'>";
            result += "<thead><tr><th class='col-md-2'>Date Received</th><th class='col-md-2'></th>";
            result += "<th class='col-md-2'>Approved</th><th class='col-md-2'>Rejected</th><th></th><th></th><th></th></tr></thead>";

            string returnUrl = Request.UrlReferrer.ToString();
            foreach (var f in files)
            {
                result += "<tbody><tr><td>" + DateTime.Parse(f.DateReceived).ToShortDateString() + "</td>";
                if (string.IsNullOrEmpty(f.FilePath))
                {
                    result += "<td>No Electronic Copy</td>";
                }
                else
                {
                    result += "<td class='text-center'><a target='_blank' href='" + Url.Content(f.FilePath) + "'>View</a></td>";
                }

                string deleteCell = "<td class='text-right'><a onclick=\"return confirm('Are your sure to delete this document?');\" class='btn btn-warning btn-sm' ";
                deleteCell += " href ='" + Url.Content("~/Project/DeleteDocument/" + f.Id) + "?returnUrl=" + returnUrl + "'>Delete</a></td></tr>";
                if (f.DateApproved == null && f.DateRejected == null)
                {
                    result += "<td></td><td></td><td class='text-center'><a class='btn btn-success btn-sm' href='" + Url.Content("~/Project/ApproveDocument/" + f.Id) + "?returnUrl=" + returnUrl + "'>Approve</a></td>";
                    result += "<td class='text-center'><a class='btn btn-danger btn-sm' href='" + Url.Content("~/Project/RejectDocument/" + f.Id) + "'>Reject</a></td>";
                    result += deleteCell;
                }
                else if (f.DateApproved != null)
                {
                    if (f.DocumentType == "ListSub" || f.DocumentType == "NtceEEO")
                    {
                        if (!User.IsInRole("DCO"))
                        {
                            result += "<td></td><td class='text-center'><a class='btn btn-danger btn-sm' href='" + Url.Content("~/Project/RejectDocument/" + f.Id) + "'>Reject</a></td>";
                            result += deleteCell;
                        }
                        else
                        {
                            result += "<td colspan='4'></td></tr>";
                        }
                    }
                    else
                    {
                        bool canReject = true;
                        if (f.ProjectID == 0)
                        {
                            var closedProjects = (from df in _documentFileRepository.Table
                                                  join p in _projectRepository.Table on df.ProjectID equals p.Id
                                                  where df.Year == f.Year && df.ProjectID != 0 && df.ContractorID == f.ContractorID
                                                       && p.DateClosed != null
                                                  select new { id = p.Id, joc = p.JOC }).Distinct().ToList();

                            canReject = (closedProjects.Count() == 0);
                        }
                        result += "<td class='text-center'>" + ((DateTime)f.DateApproved).ToShortDateString() + "</td><td></td>";
                        if (canReject || !User.IsInRole("DCO"))
                        {
                            result += "<td></td><td class='text-center'><a class='btn btn-danger btn-sm' href='" + Url.Content("~/Project/RejectDocument/" + f.Id) + "'>Reject</a></td>";
                            result += deleteCell;
                        }
                        else
                        {
                            result += "<td colspan='4'></td></tr>";
                        }
                    }
                }
                else if (f.DateRejected != null)
                {
                    result += "<td></td><td class='text-center'>" + ((DateTime)f.DateRejected).ToShortDateString() + "</td><td></td><td></td>";
                    result += deleteCell;
                    result += "<tr><td colspan='7' style='border-bottom: solid 2px #666;'><strong>Reason:</strong>&nbsp;" + f.Comment + "</td></tr>";
                }

            }
            result += "</tbody></table>";

            return (files.Count > 0) ? result : "";
        }

        public string GetFiles1(int id)
        {
            DocumentFile df = db.DocumentFiles.Find(id);

            return GetFiles(df.ProjectID, df.ContractorID, df.Year, df.Month, df.DocumentType);
        }

        public ActionResult DeleteDocument(int id, string returnUrl)
        {
            string message = "File was deleted succesfully";
            var file = db.DocumentFiles.FirstOrDefault(x => x.Id == id);

            try
            {
                db.DocumentFiles.Remove(file);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                message = "Error: " + ex.Message;
            }

            TempData["message"] = message;

            return Redirect(returnUrl);
        }

        public JsonResult GetAccountNo(string q)
        {
            string[] data = db.AccountNumbers.Where(x => x.AccountNo.StartsWith(q)).Select(x => x.AccountNo).Distinct().ToArray();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ApproveDocument(int id, string returnUrl)
        {
            DateTime dateApproved = DateTime.Now;

            var doc = db.DocumentFiles.Find(id);
            doc.DateApproved = dateApproved;

            db.Entry(doc).State = EntityState.Modified;
            db.SaveChanges();

            UpdatePastDueDocument(doc.ProjectID, doc.ContractorID, doc.Year, doc.Month, doc.DocumentType, dateApproved);

            return Redirect(returnUrl);
        }

        public ActionResult RejectDocument(int id, string returnUrl = "")
        {
            var model = db.DocumentFiles.Find(id);

            ViewBag.ReturnUrl = (String.IsNullOrEmpty(returnUrl)) ? Request.UrlReferrer.ToString() : returnUrl;

            return View(model);
        }


        [HttpPost]
        public ActionResult RejectDocument(FormCollection form, string returnUrl = "")
        {
            int id = int.Parse(form["ID"]);
            DocumentFile doc = db.DocumentFiles.Find(id);
            if (ModelState.IsValid)
            {
                doc.DateApproved = null;
                doc.DateRejected = DateTime.Now;
                doc.Comment = form["Comment"];
                db.Entry(doc).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("EmailDocumentRejected", "Notice", new { id = id, returnUrl = returnUrl });
            }

            return RedirectToAction("RejectDocument", new { returnUrl = returnUrl });
        }

        public PartialViewResult SendClearanceRequestButton(int id)
        {
            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);

            if (User.IsInRole("Administrator") || User.IsInRole("Manager"))
            {
                if (request.CurrentStatus.Contains("to Manager"))
                {
                    ViewBag.ShowButton = true;
                    ViewBag.ProjectID = id;
                }
                else
                {
                    ViewBag.ShowButton = false;
                }
            }
            else if (User.IsInRole("DCO"))
            {
                if (request.CurrentStatus.Contains("to CO") || request.CurrentStatus.Contains("by Manager"))
                {
                    ViewBag.ShowButton = true;
                    ViewBag.ProjectID = id;
                }
                else
                {
                    ViewBag.ShowButton = false;
                }
            }
            else
            {
                ViewBag.ShowButton = false;
            }

            return PartialView("_SendClearanceRequestButton");
        }

        public ActionResult WorksheetDetails(int id, string code)
        {
            var model = (from w in db.Worksheets
                         join p in db.Projects on w.ProjectId equals p.Id
                         where w.IsBillable == true && w.ProjectId == id && w.ActivityCode == code
                         orderby w.WorkDate descending
                         select new WorksheetListModel
                         {
                             Worksheet = w,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             DepartmentID = p.DepartmentID,
                             AccountNo = p.Unit,
                             PrimaryContractorID = p.PrimeContractorID
                         }).ToList();

            foreach (var m in model)
            {
                m.TotalHours = GetHours(m.Worksheet.Id);

                Contractor c = GetContractor(m.ProjectID);
                if (c != null)
                {
                    m.CompanyName = c.CompanyName;
                }
            }

            ViewBag.ProjectID = id;

            return View(model);
        }

        private Contractor GetContractor(int? id)
        {
            Contractor contractor = new Contractor();

            if (id != null)
            {
                contractor = (from p in db.Projects
                              join c in db.Contractors on p.PrimeContractorID equals c.Id
                              where p.Id == id
                              select c).FirstOrDefault();
            }

            return contractor;
        }
        private decimal GetHours(int? id)
        {
            decimal hours = 0.0m;

            if (id != null)
            {
                var obj = (from w in db.Worksheets
                           where w.Id == id
                           select new { w.Hours, w.Minutes }).FirstOrDefault();

                hours = obj.Hours + obj.Minutes / 60m;
            }

            return hours;
        }

        public decimal GetEstimatedHours(string amount)
        {
            decimal am, hours = 0.0m;
            if (decimal.TryParse(amount, out am))
            {
                hours = (from br in db.BillingRates
                         where br.MinAmount <= am && br.MaxAmount >= am
                         select br.EstimatedHours).FirstOrDefault();
            }

            return hours;
        }

        public PartialViewResult ProjectStats(int id)
        {
            Project project = db.Projects.Find(id);
            ProjectStats model = new ProjectStats
            {
                DCO = project.DCO,
                ContractAmount = (project.ContractAmount == null) ? "-" : ((decimal)project.ContractAmount).ToString("$#,###.00"),
                FederalFunds = (project.FederalFunds) ? "Yes" : "No",
                AvailableHours = (project.HoursAvailable == null) ? "-" : ((decimal)project.HoursAvailable).ToString("0.00"),
                RemainingHours = (project.HoursRemaining == null) ? "-" : ((decimal)project.HoursRemaining).ToString("0.00"),
            };

            if (project.ProjectType == null)
            {
                model.ProjectType = "-";
            }
            else
            {
                model.ProjectType = (project.ProjectType == ProjectType.Capital) ? "Capital" : "Non-Capital";
            }

            var siteVisits = db.Inspections.Where(x => x.ProjectId == id && x.DateSiteVisitCompletion != null).ToList();
            int cnt = siteVisits.Count;
            model.NumberSiteVisit = cnt;

            if (cnt == 0)
            {
                model.LastSiteVisit = "-";
            }
            else
            {
                var lastVisit = siteVisits.OrderByDescending(x => x.DateOfVisit).First();
                model.LastSiteVisit = ((DateTime)lastVisit.DateOfVisit).ToShortDateString();
            }

            model.NumberSubs = (project.NumberSubcontractors == null) ? 0 : (int)project.NumberSubcontractors;

            return PartialView("_ProjectStats", model);
        }

        public JsonResult GetProjectContacts(string q, string type)
        {
            List<ContactInfo> list = new List<ContactInfo>();

            if (type == "Dept")
            {
                list = (from c in db.ProjectContacts
                        where c.DeptContact.StartsWith(q)
                        group c by c.DeptContact into grp
                        let lastId = grp.Max(x => x.Id)
                        from pc in grp
                        where pc.Id == lastId
                        select new ContactInfo
                        {
                            Name = pc.DeptContact,
                            Email = pc.DeptContactEmail,
                            PhoneNumber = pc.DeptContactPhoneNumber,
                            Extension = pc.DeptContactExtension
                        }).OrderBy(x => x.Name).ToList();
            }
            else if (type == "Analyst")
            {
                list = (from c in db.ProjectContacts
                        where c.DeptContact.StartsWith(q)
                        group c by c.Analyst into grp
                        let lastId = grp.Max(x => x.Id)
                        from pc in grp
                        where pc.Id == lastId
                        select new ContactInfo
                        {
                            Name = pc.Analyst,
                            Email = pc.AnalystEmail,
                            PhoneNumber = pc.AnalystPhoneNumber,
                            Extension = pc.AnalystExtension
                        }).OrderBy(x => x.Name).ToList();
            }
            else if (type == "PM")
            {
                list = (from c in db.ProjectContacts
                        where c.DeptContact.StartsWith(q)
                        group c by c.ProjectManager into grp
                        let lastId = grp.Max(x => x.Id)
                        from pc in grp
                        where pc.Id == lastId
                        select new ContactInfo
                        {
                            Name = pc.ProjectManager,
                            Email = pc.ProjectManagerEmail,
                            PhoneNumber = pc.ProjectManagerPhoneNumber,
                            Extension = pc.ProjectManagerExtension
                        }).OrderBy(x => x.Name).ToList();
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCities(string q)
        {
            string[] data = db.Projects.Where(x => x.City.StartsWith(q)).Select(x => x.City).Distinct().ToArray();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public string CheckDuplicate(string joc)
        {
            var project = db.Projects.FirstOrDefault(x => x.JOC == joc);

            return (project == null) ? "" : "Entered project ID '" + joc + "' already exists."; ;
        }

        public ActionResult MapsDetails(int id, string fiscalYear = "", string month = "", string returnUrl = "")
        {
            string accountNo = db.Projects.FirstOrDefault(x => x.Id == id).Unit;
            MapsDetailModel model = new MapsDetailModel
            {
                ProjectId = id,
                ReturnUrl = returnUrl
            };

            if (string.IsNullOrEmpty(accountNo) || accountNo.Length < 10)
            {
                string msg = "Error: Account information is not available.  Please check whether the 'Unit' is entered for this project.";
                TempData["message"] = msg;

                return View(model);
            }

            string mainAccount = accountNo.Substring(0, 5);
            string cnnStr = ConfigurationManager.ConnectionStrings["WebBASIS"].ConnectionString;

            int yyyy = DateTime.Today.Year;
            if (String.IsNullOrEmpty(fiscalYear))
            {
                fiscalYear = (DateTime.Today.Month < 7) ? (yyyy - 1) + "-" + yyyy : yyyy + "-" + (yyyy + 1);
            }
            if (String.IsNullOrEmpty(month))
            {
                month = DateTime.Today.AddMonths(-1).Month.ToString();
            }

            try
            {
                using (SqlConnection cnn = new SqlConnection(cnnStr))
                {
                    string sql = "[dbo].[sp_CUST04]";
                    SqlCommand cmd = new SqlCommand(sql, cnn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@fiscal_year", fiscalYear));
                    cmd.Parameters.Add(new SqlParameter("@main_acct", mainAccount));
                    cmd.Parameters.Add(new SqlParameter("@month_no", month));

                    cnn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    model.FiscalYear = fiscalYear;
                    model.Month = month;
                    model.MainAccount = mainAccount;

                    while (dr.Read())
                    {
                        model.MainAccountDescription = dr["main_acct_desc"].ToString();
                        model.MapsCode = dr["maps_code"].ToString();
                        model.MapsCodeDescription = dr["maps_desc"].ToString();

                        MapsDetailModel.MapsDetail detail = new MapsDetailModel.MapsDetail();

                        detail.Account = dr["account"].ToString();
                        detail.BillCode = dr["bill_code"].ToString();
                        detail.BillCodeDescription = dr["bill_code_desc"].ToString();
                        detail.OtherDescription = dr["other_desc"].ToString();
                        detail.BillUnit = dr["bill_unit"].ToString();
                        detail.YtdBillUnit = dr["ytd_bill_unit"].ToString();
                        detail.Amount = dr["bill_amt"].ToString();
                        detail.YtdAmount = dr["ytd_bill_amt"].ToString();

                        model.MapsDetails.Add(detail);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error while accessing WebBASIS data.";

                return Redirect(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            List<SelectListItem> fiscalYears = new List<SelectListItem>();
            for (int i = 0; i < 2; i++)
            {
                int yr = yyyy - i;
                string txt = (DateTime.Today.Month < 7) ? (yr - 1) + "-" + yr : yr + "-" + (yr + 1);
                fiscalYears.Add(new SelectListItem { Text = txt, Value = txt });
            }
            ViewBag.FiscalYears = fiscalYears;

            List<SelectListItem> months = new List<SelectListItem>();
            for (int i = 1; i <= 12; i++)
            {
                months.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });
            }
            ViewBag.Months = months;

            return View(model);
        }

        [HttpPost]
        public ActionResult MapsDetails(FormCollection form, int id, string returnUrl)
        {
            string fiscalYear = form["FiscalYear"];
            string month = form["Month"];

            return RedirectToAction("MapsDetails", new { id = id, fiscalYear = fiscalYear, month = month, returnUrl = returnUrl });
        }

        public String GetContractorInfo(int id)
        {
            string result = "";
            var contractor = db.Contractors.FirstOrDefault(x => x.Id == id);
            result += contractor.Address1 + ";";
            result += contractor.Address2 + ";";
            result += contractor.City + ";";
            result += contractor.State + ";";
            result += contractor.Zip + ";";
            var phone = db.ContractorContacts.FirstOrDefault(x => x.ContractorId == id);
            result += (phone != null && !String.IsNullOrEmpty(phone.PhoneNumber)) ? phone.PhoneNumber : ";";

            return result;
        }

        public ActionResult CreateClearanceRequest(int id)
        {
            var project = db.Projects.Find(id);
            if (String.IsNullOrEmpty(project.DCO))
            {
                TempData["Message"] = "Error: No DCO is assigned to this project.  Please try again after designating a DCO for this project.";
                return RedirectToAction("Details5", new { id = id });
            }

            var cr = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id && !x.CurrentStatus.StartsWith("Deleted"));
            if (cr == null)
            {
                string requestedBy = "System: " + UserProfile.UserInitial;
                ClearanceRequest req = new ClearanceRequest
                {
                    ProjectId = id,
                    DateRequested = DateTime.Now,
                    RequestedBy = requestedBy,
                    CurrentStatus = "Submitted and sent to CO"
                };
                db.ClearanceRequests.Add(req);

                ClearanceRequestLog log = new ClearanceRequestLog
                {
                    ClearanceRequestId = req.Id,
                    Date = req.DateRequested,
                    Activity = req.CurrentStatus
                };
                db.ClearanceRequestLogs.Add(log);

                var form = CommonHelper.NewCrForm(id, requestedBy);
                db.ClearanceRequestForms.Add(form);
                db.SaveChanges();

                string filePath = CommonHelper.CreateCR(id);
                CommonHelper.Upload(filePath);

                TempData["Message"] = "'Clearance Request' was submitted succesfully!";
            }
            else
            {
                TempData["Message"] = "Error: 'Clearance Request' cannot be made more than once for a project.";
            }

            return RedirectToAction("Details5", new { id = id });
        }

        public string GetProjectDetails(int id)
        {
            var p = db.Projects.Find(id);
            var cr = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);

            string html = "";

            if (cr != null)
            {
                string status = (String.IsNullOrEmpty(cr.CurrentStatus)) ? "N/A" : cr.CurrentStatus;

                html += "<dl class='dl-horizontal'>";
                html += "<dt>Current CR Status</dt>";
                html += "<dd>" + status + "</dd>";
                html += "</dl>";
            }

            html += "<dl class='dl-horizontal'>";
            html += "<dt>Project ID</dt>";
            html += "<dd>" + p.JOC + "</dd>";
            html += "<dt>Project Name</dt>";
            html += "<dd>" + p.ProjectName + "</dd>";

            html += "<dt>Phase</dt>";
            html += "<dd>" + p.Phase + "</dd>";
            html += "<dt>Unit</dt>";
            html += "<dd>" + p.Unit + "</dd>";
            html += "</dl>";

            if (p.StartDate != null && p.EndDate != null)
            {
                int percent;
                if (p.EndDate <= DateTime.Today)
                {
                    percent = 100;
                }
                else
                {
                    double max = ((DateTime)p.EndDate - (DateTime)p.StartDate).TotalDays;
                    double now = (DateTime.Today - (DateTime)p.StartDate).TotalDays;
                    percent = (int)Math.Round(now / max * 100);
                }

                string cls = "progress-bar progress-bar-success";
                if (percent >= 90 && percent < 100)
                {
                    cls = "progress-bar progress-bar-danger";
                }
                else if (percent >= 0 && percent < 90)
                {
                    cls = "progress-bar progress-bar-warning";
                }

                html += "<div class='progress' style='margin-bottom: 0;'>";
                html += "   <div class='" + cls + "' role='progressbar' aria-valuenow='" + percent + "' aria-valuemin='0' aria-valuemax='100' style='width: " + percent + "%;'>";
                html += percent + "<span>%</span>";
                html += "   </div>";
                html += "</div>";
            }

            html += "<dl class='dl-horizontal mar-top-10'>";
            html += "<dt>Date Received</dt>";
            html += (p.DateReceived == null) ? "<dd></dd>" : "<dd>" + ((DateTime)p.DateReceived).ToShortDateString() + "</dd>";
            html += "<dt>Start Date</dt>";
            html += (p.StartDate == null) ? "<dd></dd>" : "<dd>" + ((DateTime)p.StartDate).ToShortDateString() + "</dd>";
            html += "<dt>End Date</dt>";
            html += (p.EndDate == null) ? "<dd></dd>" : "<dd>" + ((DateTime)p.EndDate).ToShortDateString() + "</dd>";
            html += "<dt>Date Closed</dt>";
            html += (p.DateClosed == null) ? "<dd></dd>" : "<dd>" + ((DateTime)p.DateClosed).ToShortDateString() + "</dd>";
            html += "</dl>";

            html += "<dl class='dl-horizontal'>";
            html += "<dt>Contract Amount</dt>";
            if (p.ContractAmount != null)
                html += "<dd>" + ((Decimal)p.ContractAmount).ToString("c") + "</dd>";
            else
                html += "<dd></dd>";

            html += "<dt># Subs</dt>";
            html += "<dd>" + p.NumberSubcontractors + "</dd>";

            html += "<dt>Capital?</dt>";
            html += "<dd>" + p.ProjectType + "</dd>";
            html += "<dt>Federal</dt>";
            html += (p.FederalFunds) ? "<dd>Yes</dd>" : "<dd>No</dd>";
            html += "</dl>";

            html += "<dl class='dl-horizontal'>";
            html += "<dt>Address</dt>";
            html += "<dd>" + p.Address + "</dd>";
            html += "<dt>City</dt>";
            html += "<dd>" + p.City + "</dd>";
            html += "<dt>Zip</dt>";
            html += "<dd>" + p.Zip + "</dd>";
            html += "</dl>";

            var dept = db.Departments.FirstOrDefault(x => x.DepartmentId == p.DepartmentID);

            html += "<dl class='dl-horizontal'>";
            html += "<dt>Department</dt>";

            if (dept == null)
            {
                html += "<dd>N/A</dd>";
            }
            else
            {
                html += "<dd>" + dept.DepartmentName + "</dd>";
            }
            html += "</dl>";

            string availableHours = (p.HoursAvailable == null) ? "-" : ((decimal)p.HoursAvailable).ToString("0.00");
            string remainingHours = (p.HoursRemaining == null) ? "-" : ((decimal)p.HoursRemaining).ToString("0.00");

            html += "<dl class='dl-horizontal'>";
            html += "<dt>Available Hours</dt>";
            html += "<dd>" + availableHours + "</dd>";
            html += "<dt>Remaining Hours</dt>";
            html += "<dd>" + remainingHours + "</dd>";
            html += "</dl>";

            //int missing = CommonHelper.GetMissingDocumentCount(id);
            //html += "<dl class='dl-horizontal'>";
            //html += "<dt>Missing Documents</dt>";
            //if (missing == 0)
            //{
            //    html += "<dd>0</dd>";
            //}
            //else
            //{
            //    html += "<dd><span class=\"badge badge-red\">" + missing + "</span></dd>";
            //}
            //html += "</dl>";

            return html;
        }

        public string GetProjectList(int id, string co)
        {
            List<Project> list1;
            string key = String.Format(PROJECT_LIST_KEY, "PRIME");
            if (!CacheHelper.Get<List<Project>>(key, out list1))
            {
                list1 = db.Projects.Where(x => x.DateClosed == null).ToList();
                foreach (var l in list1)
                {
                    var worksheet = db.Worksheets.Where(x => x.ProjectId == l.Id).OrderByDescending(x => x.WorkDate).FirstOrDefault();

                    if (worksheet != null && l.DCO != worksheet.DCO)
                    {
                        l.DCO = worksheet.DCO;
                    }
                }
                CacheHelper.Add<List<Project>>(list1, key, 60.0);
            }
            list1 = list1.Where(x => x.PrimeContractorID == id && x.DCO == co).OrderBy(x => x.JOC).ToList();

            string html = "";

            html += "<div class='row'>";

            int cnt = 1;
            foreach (var p in list1)
            {
                html += "<div class='col-md-1 text-right'>" + cnt + ".</div><div class='col-md-11'>" + p.JOC + "</div>";
                cnt++;
            }

            html += "</div>";

            return html;
        }

        public JsonResult GetSubcontractor(int id, int seq)
        {
            var contractors = from c in db.Contractors
                              join ct in db.Contracts on c.Id equals ct.ContractorId
                              join p in db.Projects on ct.ProjectId equals p.Id
                              where ct.SubLevel > 0 && p.Id == id
                              orderby c.CompanyName
                              select new
                              {
                                  ContractorId = c.Id,
                                  CompanyName = c.CompanyName,
                                  ContractAmount = ct.ContractAmount
                              };

            int total = contractors.Count();
            if (seq > total)
                seq = 1;
            else if (seq == 0)
                seq = total;
            var subcontractor = contractors.Skip(seq - 1).FirstOrDefault();

            string contractAmount = (subcontractor.ContractAmount == null) ? "N/A" : ((Decimal)subcontractor.ContractAmount).ToString("$#,##0");
            var review = db.ReviewSubcontractors.FirstOrDefault(x => x.ProjectId == id && x.ContractorId == subcontractor.ContractorId);

            if (review != null)
            {
                return Json(new
                {
                    contractorId = subcontractor.ContractorId,
                    sub = String.Format("{0} ({1} of {2}) - {3}", subcontractor.CompanyName, seq, total, contractAmount),
                    seq = seq,
                    q5 = review.CheckItem5,
                    q6 = review.CheckItem6,
                    q7 = review.CheckItem7,
                    q8 = review.CheckItem8
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    contractorId = subcontractor.ContractorId,
                    sub = String.Format("{0} ({1} of {2}) - {3}", subcontractor.CompanyName, seq, total, contractAmount),
                    seq = seq
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public void SaveSection2(int id, int contractorId, string question, int answer)
        {
            var review = db.ReviewSubcontractors.FirstOrDefault(x => x.ProjectId == id && x.ContractorId == contractorId);

            if (review == null)
            {
                review = new ReviewSubcontractor
                {
                    ProjectId = id,
                    ContractorId = contractorId,
                    CheckItem5 = -1,
                    CheckItem6 = -1,
                    CheckItem7 = -1,
                    CheckItem8 = -1,
                    DateLastUpdated = DateTime.Now
                };

                switch (question)
                {
                    case "q5":
                        review.CheckItem5 = answer;
                        break;
                    case "q6":
                        review.CheckItem6 = answer;
                        break;
                    case "q7":
                        review.CheckItem7 = answer;
                        break;
                    case "q8":
                        review.CheckItem8 = answer;
                        break;
                }

                db.ReviewSubcontractors.Add(review);
            }
            else
            {
                review.DateLastUpdated = DateTime.Now;

                switch (question)
                {
                    case "q5":
                        review.CheckItem5 = answer;
                        break;
                    case "q6":
                        review.CheckItem6 = answer;
                        break;
                    case "q7":
                        review.CheckItem7 = answer;
                        break;
                    case "q8":
                        review.CheckItem8 = answer;
                        break;
                }

                db.Entry(review).State = EntityState.Modified;
            }

            db.SaveChanges();
        }

        public void UncheckSection2(int id, int contractorId, string question)
        {
            var review = db.ReviewSubcontractors.FirstOrDefault(x => x.ProjectId == id && x.ContractorId == contractorId);

            if (review != null)
            {
                review.DateLastUpdated = DateTime.Now;

                switch (question)
                {
                    case "q5":
                        review.CheckItem5 = -1;
                        break;
                    case "q6":
                        review.CheckItem6 = -1;
                        break;
                    case "q7":
                        review.CheckItem7 = -1;
                        break;
                    case "q8":
                        review.CheckItem8 = -1;
                        break;
                }

                db.Entry(review).State = EntityState.Modified;
            }

            db.SaveChanges();
        }
    }
}