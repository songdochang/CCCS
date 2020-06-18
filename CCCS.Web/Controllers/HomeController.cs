using CCCS.Core.Data;
using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Core.Domain.Common;
using CCCS.Core.Domain.Documents;
using CCCS.Core.Domain.Inspection;
using CCCS.Core.Domain.Notices;
using CCCS.Core.Domain.Projects;
using CCCS.Core.Domain.Users;
using CCCS.Infrastructure;
using CCCS.Web.Models;
using CCCS.Web.Models.ClearanceRequests;
using CCCS.Web.Models.Documents;
using CCCS.Web.Models.Home;
using CCCS.Web.Models.Inspection;
using CCCS.Web.Models.Notices;
using CCCS.Web.Models.Projects;
using CCCS.Web.Models.Users;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace CCCS.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly IRepository<Cache> _cacheRepository;
        private readonly IRepository<PublicProfile> _publicProfileRepository;
        private readonly IRepository<SiteVisitException> _siteVisitExceptionRepository;

        public HomeController(IRepository<Cache> cacheRepository,
            IRepository<PublicProfile> publicProfileRepository,
            IRepository<SiteVisitException> siteVisitRepository)
        {
            _cacheRepository = cacheRepository;
            _publicProfileRepository = publicProfileRepository;
            _siteVisitExceptionRepository = siteVisitRepository;
        }

        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Administrator") || User.IsInRole("Manager"))
                {
                    base.GetContractorOverviewModelAsync();

                    return RedirectToAction("Manager1");
                }
                else if (User.IsInRole("Clerical") || User.IsInRole("Clerical2"))
                {
                    base.GetContractorOverviewModelAsync();

                    return RedirectToAction("Clerical");
                }
                else
                {
                    return RedirectToAction("DCO");
                }
            }
            else
            {
                return View();
            }
        }

        public ActionResult DCO()
        {
            string dco = UserProfile.UserInitial;
            ViewBag.CO = dco;

            var model = new DcoSummaryModel
            {
                NumberOpenProjects = GetNumberOpenProjects(0, dco),
                AmountOpenProjects = GetAmountOpenProjects(0, dco),
                NumberFederalProjects = GetNumberFederalProjects(0, dco),
                AmountFederalProjects = GetAmountFederalProjects(0, dco),
                NumberSiteVisits = GetNumberSiteVisits(0, dco),
                LastSiteVisit = GetLastSiteVisit(0, dco)
            };

            return View("DCO", model);
        }

        public ActionResult Clerical()
        {
            List<SummaryRowModel> model = new List<SummaryRowModel>();
            var dcos = GetCOs(false);

            foreach (var d in dcos)
            {
                if (!String.IsNullOrEmpty(d.Value))
                {
                    var dco = d.Value;

                    SummaryRowModel row = new SummaryRowModel
                    {
                        DCO = dco,
                        NumberOpenProjects = GetNumberOpenProjects(0, dco),
                        AmountOpenProjects = GetAmountOpenProjects(0, dco),
                        NumberFederalProjects = GetNumberFederalProjects(0, dco),
                        AmountFederalProjects = GetAmountFederalProjects(0, dco),
                        NumberSiteVisits = GetNumberSiteVisits(0, dco),
                        LastSiteVisit = GetLastSiteVisit(0, dco)
                    };

                    model.Add(row);
                }
            }

            ViewBag.NumberOpenProjects = GetNumberOpenProjects(0, "");
            ViewBag.AmountOpenProjects = GetAmountOpenProjects(0, "");
            ViewBag.NumberFederalProjects = GetNumberFederalProjects(0, "");
            ViewBag.AmountFederalProjects = GetAmountFederalProjects(0, "");
            ViewBag.NumberSiteVisits = GetNumberSiteVisits(0, "");
            ViewBag.LastSiteVisit = GetLastSiteVisit(0, "");

            return View(model);
        }

        public ActionResult Manager1()
        {
            var helper = new CommonHelper(_cacheRepository);
            ViewBag.Exceptions = helper.GetCrExceptionsCount();

            return View();
        }

        public ActionResult Manager2(DateTime? dateFrom = null, DateTime? dateTo = null, int since = 0)
        {
            List<SummaryRowModel> model = new List<SummaryRowModel>();
            var dcos = GetCOs(false);

            foreach (var d in dcos)
            {
                var dco = d.Value;

                if (!String.IsNullOrEmpty(dco))
                {
                    SummaryRowModel row = new SummaryRowModel
                    {
                        DCO = dco,
                        NumberOpenProjects = GetNumberOpenProjects(0, dco),
                        AmountOpenProjects = GetAmountOpenProjects(0, dco),
                        NumberFederalProjects = GetNumberFederalProjects(0, dco),
                        AmountFederalProjects = GetAmountFederalProjects(0, dco),
                        NumberSiteVisits = GetNumberSiteVisits(0, dco),
                        LastSiteVisit = GetLastSiteVisit(0, dco),
                        NumberReceivedProjects = GetNumberReceivedProjects(dco),
                        AmountReceivedProjects = GetAmountReceivedProjects(dco),
                    };

                    model.Add(row);
                }
            }

            ViewBag.NumberOpenProjects = GetNumberOpenProjects(0, "");
            ViewBag.AmountOpenProjects = GetAmountOpenProjects(0, "");
            ViewBag.NumberFederalProjects = GetNumberFederalProjects(0, "");
            ViewBag.AmountFederalProjects = GetAmountFederalProjects(0, "");
            ViewBag.NumberSiteVisits = GetNumberSiteVisits(0, "");
            ViewBag.LastSiteVisit = GetLastSiteVisit(0, "");
            ViewBag.NumberReceivedProjects = GetNumberReceivedProjects("");
            ViewBag.AmountReceivedProjects = GetAmountReceivedProjects("");

            ViewBag.Since = since;
            ViewBag.Date1 = DateTime.Today.AddMonths(-1);
            ViewBag.Date2 = DateTime.Today;

            return View(model);
        }

        [HttpPost]
        public ActionResult Manager2(FormCollection form)
        {
            int since = int.Parse(form["since"]);
            string dateFrom = form["dateFrom"];
            string dateTo = form["dateTo"];

            return RedirectToAction("Manager2", new { dateFrom = dateFrom, dateTo = dateTo, since = since });
        }

        public ActionResult Manager3()
        {
            DateTime dt1 = DateTime.Today.AddMonths(-12);
            DateTime dt2 = DateTime.Today;

            List<ContractorSummaryRowModel> model = GetContractorOverviewModel();

            ViewBag.NumberContractors = model.Count;
            ViewBag.NumberOpenProjects = GetNumberOpenProjects();
            ViewBag.AmountOpenProjects = GetAmountOpenProjects();
            ViewBag.NumberFederalProjects = GetNumberFederalProjects();
            ViewBag.AmountFederalProjects = GetAmountFederalProjects();
            ViewBag.NumberSiteVisits = GetNumberSiteVisits();
            ViewBag.LastSiteVisit = GetLastSiteVisit();

            int nc = GetNumberNotCompliant(model);
            ViewBag.NumberNotCompliant = nc;
            ViewBag.NumberInCompliance = model.Count - nc;
            ViewBag.NumberSubs = GetNumberSubs(model);
            ViewBag.NumberSubsDistinct = GetNumberSubs(model, true);

            return View(model);
        }

        public PartialViewResult ClearanceRequest(string dco)
        {
            var model = ClearanceRequestModel.GetModel(false);

            if (User.IsInRole("DCO") || User.IsInRole("Clerical") || User.IsInRole("Clerical2"))
            {
                model = model.Where(x => (x.CurrentStatus.Contains("to CO") || x.CurrentStatus.Contains("Manager"))).ToList();

                foreach (var m in model)
                {
                    m.NumberMissingDocuments = CommonHelper.GetMissingDocumentCount(m.ProjectID);
                }
            }
            else
            {
                model = model.Where(x => x.CurrentStatus.Contains("to Manager")).ToList();

                foreach (var m in model)
                {
                    var crForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == m.ProjectID);

                    if (crForm != null && crForm.DateClearedByDCO != null)
                    {
                        m.IsCleared = crForm.IsCleared;
                        m.Comment = crForm.Comments;
                    }

                    m.NumberMissingDocuments = CommonHelper.GetMissingDocumentCount(m.ProjectID);
                }
            }
            model = model.OrderBy(x => x.DateRequested).ToList();

            ViewBag.CO = dco;

            return PartialView("_ClearanceRequest", model);
        }

        public PartialViewResult ClearanceRequestException(string dco)
        {
            DateTime dt = DateTime.Today.AddDays(-7);

            // get the candidates
            var list = (from p in db.Projects
                        join cr in db.ClearanceRequests on p.Id equals cr.ProjectId
                        where cr.DateRequested < dt && p.DateClosed == null
                             && (cr.CurrentStatus.Contains("to CO") || cr.CurrentStatus.Contains("by Manager"))
                        orderby cr.DateRequested
                        select new ClearanceRequestExceptionListModel
                        {
                            ClearanceRequestID = cr.Id,
                            ProjectID = p.Id,
                            JOC = p.JOC,
                            ProjectName = p.ProjectName,
                            DateRequested = cr.DateRequested,
                            DCO = p.DCO
                        }).ToList();

            // make sure documents are complete
            var model = new List<ClearanceRequestExceptionListModel>();
            foreach (var item in list)
            {
                int missingDocs = CommonHelper.GetMissingDocumentCount(item.ProjectID);
                if (missingDocs == 0)
                {
                    DateTime lastDocDate = CommonHelper.GetLastDocumentDate(item.ProjectID);

                    if (lastDocDate <= dt)
                        model.Add(item);
                }
            }

            DateTime dt1 = DateTime.Today.AddMonths(-6);
            int[] exceptionIds = db.ClearanceRequestExceptions.Where(x => x.DateCommented >= dt1).Select(x => x.ClearanceRequestId).ToArray();
            model = model.Where(x => !exceptionIds.Any(y => y == x.ClearanceRequestID)).ToList();

            foreach (var m in model)
            {
                m.PastDueDays = (DateTime.Today - m.DateRequested).Days - 7;
            }

            ViewBag.CO = dco;

            return PartialView("_ClearanceRequestException", model);
        }

        public PartialViewResult SiteVisitApproval()
        {
            var model = from i in db.Inspections
                        join p in db.Projects on i.ProjectId equals p.Id
                        join c in db.Contractors on i.ContractorId equals c.Id
                        where new[] { "Site Visit Request sent to Manager", "VC sent to Manager", "SVC sent to Manager" }.Contains(i.Status)
                            && i.DateCancelled == null
                        orderby i.DateRequested descending
                        select new InspectionListModel
                        {
                            ProjectID = p.Id,
                            DCO = i.DCO,
                            InspectionID = i.Id,
                            JOC = p.JOC,
                            ProjectName = p.ProjectName,
                            CompanyName = c.CompanyName,
                            DateRequested = i.DateRequested,
                            DateOfVisit = i.DateOfVisit,
                            DateApproved = i.DateApproved,
                            Address = i.Address,
                            City = i.City,
                            DateCancelled = i.DateCancelled,
                            DateSiteVisitCompletion = i.DateSiteVisitCompletion,
                            DateViolationCorrection = i.DateViolationCorrection,
                            Status = i.Status
                        };

            return PartialView("_SiteVisitApproval", model);
        }

        public PartialViewResult SiteVisits(string dco)
        {
            var model = from i in db.Inspections
                        join p in db.Projects on i.ProjectId equals p.Id
                        join c in db.Contractors on i.ContractorId equals c.Id
                        where i.DateCancelled == null && !new[] { "SVC email notification", "VC sent to Department" }.Contains(i.Status)
                        select new InspectionListModel
                        {
                            DCO = i.DCO,
                            InspectionID = i.Id,
                            ProjectID = p.Id,
                            JOC = p.JOC,
                            ProjectName = p.ProjectName,
                            CompanyName = c.CompanyName,
                            DateRequested = i.DateRequested,
                            DateApproved = i.DateApproved,
                            DateContractorNotification = i.DateContractorNotification,
                            DateOfVisit = i.DateOfVisit,
                            DateSiteInspectionUploaded = i.DateSiteInspectionUploaded,
                            DateSiteVisitCompletion = i.DateSiteVisitCompletion,
                            DateViolationCorrection = i.DateViolationCorrection,
                            Address = i.Address,
                            City = i.City,
                            RoundTripHours = i.RoundTripHours,
                            DateCancelled = i.DateCancelled,
                            Status = i.Status
                        };

            ViewBag.CO = dco;

            return PartialView("_SiteVisits", model);
        }

        public PartialViewResult SiteVisitException(string dco)
        {
            DateTime start = DateTime.Today;

            switch (start.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    start = start.AddDays(-7);
                    break;
                case DayOfWeek.Tuesday:
                    start = start.AddDays(-8);
                    break;
                case DayOfWeek.Wednesday:
                    start = start.AddDays(-9);
                    break;
                case DayOfWeek.Thursday:
                    start = start.AddDays(-10);
                    break;
                case DayOfWeek.Friday:
                    start = start.AddDays(-11);
                    break;
            }
            DateTime end = start.AddDays(4);

            if (start.DayOfWeek != DayOfWeek.Saturday && start.DayOfWeek != DayOfWeek.Sunday)
            {
                if (!db.Inspections.Any(x => x.DateOfVisit >= start && x.DateOfVisit <= end && x.DCO == dco))
                {
                    string week = start.ToShortDateString();
                    var ex = _siteVisitExceptionRepository.Table.FirstOrDefault(x => x.Week == week && x.DCO == dco);

                    if (ex == null)
                    {
                        _siteVisitExceptionRepository.Insert(new SiteVisitException
                        {
                            Week = week,
                            DateRange = start.ToShortDateString() + " ~ " + end.ToShortDateString(),
                            DCO = dco
                        });
                    }
                }
            }

            var model = _siteVisitExceptionRepository.Table.Where(x => x.DCO == dco && string.IsNullOrEmpty(x.CommentText)).OrderBy(x => x.Id).ToList();
            ViewBag.CO = dco;

            return PartialView("_SiteVisitException", model);
        }


        public PartialViewResult DocumentReceived(string dco)
        {
            ViewBag.CO = dco;

            return PartialView("_DocumentReceived");
        }

        public ActionResult DocumentReceivedTable(string dco)
        {
            var list1 = (from f in db.DocumentFiles
                         join p in db.Projects on f.ProjectID equals p.Id
                         join c in db.Contractors on f.ContractorID equals c.Id
                         where f.DateReceived != null && f.DateApproved == null && f.DateRejected == null
                             && p.DateClosed == null 
                             && (f.DocumentType == "ListSub" || f.DocumentType == "NtceEEO")
                         select new DocumentReceivedModel
                         {
                             DocumentID = f.Id,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             CompanyName = c.CompanyName,
                             ContractorID = c.Id,
                             Date = f.DateReceived,
                             DocumentType = f.DocumentType,
                             Year = f.Year,
                             Month = f.Month,
                             DCO = p.DCO
                         }).ToList();

            var activeContractors = (from c in db.Contractors
                                     join ct in db.Contracts on c.Id equals ct.ContractorId
                                     join p in db.Projects on ct.ProjectId equals p.Id
                                     where p.DateClosed == null
                                     select c.Id).Distinct().ToArray();

            var list2 = (from f in db.DocumentFiles
                         join c in db.Contractors on f.ContractorID equals c.Id
                         where f.DateReceived != null && f.DateApproved == null && f.DateRejected == null
                             && (f.DocumentType != "ListSub" && f.DocumentType != "NtceEEO")
                             && activeContractors.Any(y=> y == c.Id)
                         select new DocumentReceivedModel
                         {
                             DocumentID = f.Id,
                             ProjectID = 0,
                             JOC = null,
                             ProjectName = null,
                             CompanyName = c.CompanyName,
                             ContractorID = c.Id,
                             Date = f.DateReceived,
                             DocumentType = f.DocumentType,
                             Year = f.Year,
                             Month = f.Month,
                             DCO = c.DCO
                         }).ToList();

            foreach (var l in list2)
            {
                if (String.IsNullOrEmpty(l.DCO))
                {
                    var project = (from p in db.Projects
                                   join ct in db.Contracts on p.Id equals ct.ProjectId
                                   where !string.IsNullOrEmpty(p.DCO) && ct.ContractorId == l.ContractorID
                                   orderby p.EndDate descending
                                   select p).FirstOrDefault();

                    if (project != null && !String.IsNullOrEmpty(project.DCO))
                    {
                        l.DCO = project.DCO;
                    }
                }
            }

            var allList = list1.Union(list2).ToList();
            var model = allList;

            if (!String.IsNullOrEmpty(dco))
            {
                model = model.Where(x => x.DCO == dco).ToList();
            }

            model = model.OrderBy(x => x.Date).ThenBy(x => x.CompanyName).ToList();

            var viewData = new ViewDataDictionary { { "showRemove", false } };
            string html = RenderRazorViewToString("_DocumentReceivedTable", model, viewData);

            return Json(new
            {
                success = true,
                details = html,
                dco = dco,
                count = model.Count
            }, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult DocumentReceivedClerical()
        {
            var list1 = (from f in db.DocumentFiles
                         join p in db.Projects on f.ProjectID equals p.Id
                         join c in db.Contractors on f.ContractorID equals c.Id
                         join x in db.RemovedDocuments on f.Id equals x.DocumentId into fx
                         from y in fx.DefaultIfEmpty()
                         where f.DateReceived != null && f.DateApproved == null && f.DateRejected == null
                             && f.DocumentType == "ListSub" && y.ProcessedBy == null
                         select new DocumentReceivedModel
                         {
                             DocumentID = f.Id,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             CompanyName = c.CompanyName,
                             ContractorID = c.Id,
                             Date = f.DateReceived,
                             DocumentType = f.DocumentType,
                             Year = f.Year,
                             Month = f.Month,
                             DCO = p.DCO,
                             FileName = f.FileName
                         }).ToList();

            var model = list1.OrderBy(x => x.Date).ThenBy(x => x.CompanyName).ToList();

            return PartialView("_DocumentReceivedClerical", model);
        }

        public ActionResult RemoveDocument(int id)
        {
            try
            {
                RemovedDocument doc = new RemovedDocument
                {
                    DocumentId = id,
                    DateRemoved = DateTime.Now,
                    ProcessedBy = UserProfile.UserInitial
                };
                db.RemovedDocuments.Add(doc);
                db.SaveChanges();
            }
            catch (Exception ex)
            {

            }

            if (User.IsInRole("Manager"))
                return RedirectToAction("Manager");
            else if (User.IsInRole("DCO"))
                return RedirectToAction("DCO");
            else
                return RedirectToAction("Clerical");
        }

        public PartialViewResult DocumentRejected()
        {
            var files = (from f in db.DocumentFiles
                         group f by new { f.DocumentType, f.Year, f.Month } into grp
                         let LastId = grp.Max(x => x.Id)
                         from f in grp
                         where f.Id == LastId
                         where f.DateRejected != null
                         select f).ToList();

            List<DocumentReceivedModel> model = new List<DocumentReceivedModel>();
            foreach (var f in files)
            {
                DocumentReceivedModel m = new DocumentReceivedModel
                {
                    ProjectID = f.ProjectID,
                    ContractorID = f.ContractorID,
                    DocumentType = f.DocumentType,
                    Date = f.DateReceived,
                    DateRejected = f.DateRejected,
                    Year = f.Year,
                    Month = f.Month
                };

                if (f.ProjectID > 0)
                {
                    var project = db.Projects.Find(f.ProjectID);
                    m.JOC = project.JOC;
                    m.ProjectName = project.ProjectName;
                }
                m.CompanyName = db.Contractors.Find(f.ContractorID).CompanyName;

                model.Add(m);
            }

            return PartialView("_DocumentRejected", model);
        }

        public PartialViewResult RegistrationApproval(string dco)
        {
            var list = _publicProfileRepository.Table.Where(x => x.DateModified == null).ToList();

            List<RegistrationApprovalModel> model = new List<RegistrationApprovalModel>();
            foreach (var l in list)
            {
                RegistrationApprovalModel m = new RegistrationApprovalModel
                {
                    UserID = l.UserID,
                    Name = l.Name,
                    Department = l.Department,
                    Title = l.Title,
                    DateRegistered = l.DateRegistered,
                    Email = UserManager.Users.FirstOrDefault(x => x.Id == l.UserID).Email,
                    PhoneNumber = UserManager.Users.FirstOrDefault(x => x.Id == l.UserID).PhoneNumber,
                    EmployeeNumber = l.EmployeeNumber
                };

                model.Add(m);
            }

            return PartialView("_RegistrationApproval", model.ToList());
        }

        public ViewResult ShowResult(bool isSuccess, string message, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        // GET: Home/Manager4
        public ActionResult Manager4()
        {
            var list = GetCOs(false);
            List<UserProfile> profiles = new List<UserProfile>();

            foreach (var l in list)
            {
                if (!String.IsNullOrEmpty(l.Value))
                {
                    UserProfile up = db.UserProfiles.FirstOrDefault(x => x.UserInitial == l.Value);
                    profiles.Add(up);
                }
            }

            return View(profiles);
        }

        public PartialViewResult PrimePartial(string dco)
        {
            List<string> DCOs = GetCOs().Where(x => !String.IsNullOrEmpty(x.Value)).Select(x => x.Value).ToList();

            List<Project> list1 = db.Projects.Where(x => x.DateClosed == null).ToList();

            var model = (from p in list1
                         join c in db.Contractors on p.PrimeContractorID equals c.Id
                         where (String.IsNullOrEmpty(c.AlternateDCO) && c.DCO == dco) || (!String.IsNullOrEmpty(c.AlternateDCO) && c.AlternateDCO == dco)
                         group p by p.PrimeContractorID into grp
                         join c in db.Contractors on grp.Key equals c.Id
                         select new PrimeDistribution
                         {
                             PrimeContractorID = c.Id,
                             PrimeContractor = c.CompanyName,
                             TotalNumberProjects = grp.Count(),
                             NumberCapProjects = grp.Count(x => x.ProjectType == ProjectType.Capital),
                             NumberFederalFunds = grp.Count(x => x.FederalFunds == true && x.DateClosed == null)
                         }).ToList();

            int mo = DateTime.Today.Month;
            int yyyy = (mo >= 7) ? DateTime.Today.Year : DateTime.Today.Year - 1;
            DateTime dt1 = DateTime.Parse("07/01/" + yyyy);
            DateTime dt2 = dt1.AddYears(1).AddDays(-1);
            foreach (var m in model)
            {
                var projects = list1.Where(x => x.PrimeContractorID == m.PrimeContractorID);

                var sum = projects.Where(x => x.ContractAmount != null).Sum(x => x.ContractAmount);
                m.TotalContractAmount = (sum == null) ? 0.0m : (decimal)sum;
                m.NumberSubs = (projects.Sum(x => x.NumberSubcontractors) == null) ? 0 : (int)projects.Sum(x => x.NumberSubcontractors).Value;

                var visits = db.Inspections.Where(x => x.DateOfVisit >= dt1 && x.DateOfVisit <= dt2
                                                    && x.DateSiteVisitCompletion != null && x.ContractorId == m.PrimeContractorID);
                m.FySiteVisits = visits.Count();

                int[] ids = projects.Select(x => x.Id).ToArray();
                Dictionary<string, int> np = new Dictionary<string, int>();
                foreach (var d in DCOs)
                {
                    var cnt = list1.Where(x => x.DCO == d && ids.Any(y => x.Id == y)).Select(x => x.Id).Distinct().Count();
                    np.Add(d, cnt);
                }
                m.NumberProjectsByDCO = np;
            }

            model = model.Where(x => x.TotalNumberProjects > 0).OrderBy(x => x.PrimeContractor).ToList();

            ViewBag.CO = dco;
            ViewBag.COs = DCOs;

            return PartialView("_PrimePartial", model);
        }

        public ActionResult Messages()
        {
            List<UserProfile> profiles = new List<UserProfile>();

            if (User.IsInRole("Manager"))
            {
                var list = GetCOs(false);

                foreach (var l in list)
                {
                    if (!String.IsNullOrEmpty(l.Value))
                    {
                        UserProfile up = db.UserProfiles.FirstOrDefault(x => x.UserInitial == l.Value);
                        profiles.Add(up);
                    }
                }
            }
            else if (User.IsInRole("DCO"))
            {
                UserProfile up = db.UserProfiles.FirstOrDefault(x => x.UserInitial == UserProfile.UserInitial);
                profiles.Add(up);
            }

            return View(profiles);
        }

        [HttpPost]
        public ActionResult Messages(FormCollection form)
        {
            string submit = form["submit"];
            int projectId = int.Parse(form["ProjectId"]);
            string message = form["Message"];

            var messageThread = db.MessageThreads.FirstOrDefault(x => x.ProjectId == projectId);
            if (submit == "End Conversation")
            {
                messageThread.DateClosed = DateTime.Now;
                db.Entry(messageThread).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Messages");
            }

            string sender = UserProfile.UserInitial;
            string recipient = "";
            int threadId;
            if (messageThread != null)
            {
                threadId = messageThread.Id;

                // Response to rejected CR is added to CR log.
                if (messageThread.Subject.Contains("CR was rejected"))
                {
                    var clearanceRequest = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == projectId);

                    ClearanceRequestLog log = new ClearanceRequestLog
                    {
                        ClearanceRequestId = clearanceRequest.Id,
                        Date = DateTime.Now,
                        Activity = "Response to Manager",
                        Comment = message
                    };
                    db.ClearanceRequestLogs.Add(log);
                }
            }
            else
            {
                var project = db.Projects.Find(projectId);
                MessageThread newThread = new MessageThread
                {
                    ProjectId = projectId,
                    JOC = project.JOC,
                    DateCreated = DateTime.Now
                };

                db.MessageThreads.Add(newThread);
                db.SaveChanges();

                threadId = newThread.Id;
            }

            var lastSent = db.Messages.Where(x => x.ThreadId == threadId && x.Sender != sender).OrderByDescending(x => x.DateSent).FirstOrDefault();
            if (lastSent != null)
            {
                recipient = lastSent.Sender;
            }
            else
            {
                recipient = db.Messages.Where(x => x.ThreadId == threadId).OrderByDescending(x => x.DateSent).FirstOrDefault().Recipient;
            }

            Message newMessage = new Message
            {
                ThreadId = threadId,
                Sender = sender,
                Recipient = recipient,
                Text = message,
                DateSent = DateTime.Now
            };

            db.Messages.Add(newMessage);
            db.SaveChanges();

            return RedirectToAction("Messages");
        }


        public PartialViewResult MessagePartial(string id)
        {
            string ui = UserProfile.UserInitial;

            var model = (from m in db.Messages
                         group m by new { m.ThreadId } into grp
                         let LastId = grp.Max(x => x.Id)
                         from g in grp
                         join mt in db.MessageThreads on g.ThreadId equals mt.Id
                         join p in db.Projects on mt.ProjectId equals p.Id
                         where g.Id == LastId && mt.DateClosed == null
                         where g.Sender == id || g.Recipient == id
                         select new MessageListModel
                         {
                             ThreadId = mt.Id,
                             Subject = mt.Subject,
                             ProjectId = mt.ProjectId,
                             JOC = mt.JOC,
                             DateSent = g.DateSent,
                             Sender = g.Sender,
                             IsSent = (g.Sender == ui),
                             Text = g.Text
                         }).ToList();

            ViewBag.UserInitial = id;

            bool received = false;
            foreach (var m in model)
            {
                if (!m.IsSent)
                {
                    received = true;
                    break;
                }
            }
            ViewBag.Color = (received) ? db.UserProfiles.FirstOrDefault(x => x.UserInitial == id).UserColor : "";

            return PartialView("_MessagePartial", model);
        }


        public PartialViewResult NewProject(string dco)
        {
            DateTime dt1 = DateTime.Today.AddDays(-15);
            DateTime dt2 = DateTime.Today.AddDays(1);

            var model = (from p in db.Projects
                         join c in db.Contractors on p.PrimeContractorID equals c.Id into temp
                         from t in temp.DefaultIfEmpty()
                         where p.DateReceived >= dt1 && p.DateReceived < dt2 && p.DateClosed == null
                         select new NewProjectModel
                         {
                             DCO = p.DCO,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             PrimeContractorName = t.CompanyName,
                             ContractorID = t.Id,
                             DateReceived = p.DateReceived
                         }).ToList();

            ViewBag.CO = dco;

            return PartialView("_NewProject", model);
        }

        public PartialViewResult MissingProjectDetails()
        {
            var list1 = (from p in db.Projects
                         where String.IsNullOrEmpty(p.DCO) && p.DateClosed == null
                         select new MissingProjectDetailsModel
                         {
                             DCO = p.DCO,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             ContractorID = p.PrimeContractorID,
                             Comment = "DCO not assigned"
                         }).ToList();

            var list2 = (from p in db.Projects
                         where String.IsNullOrEmpty(p.Unit) && p.DateClosed == null
                         select new MissingProjectDetailsModel
                         {
                             DCO = p.DCO,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             ContractorID = p.PrimeContractorID,
                             Comment = "Account information missing"
                         }).ToList();

            var model = list1.Union(list2);

            foreach (var m in model)
            {
                var contractor = db.Contractors.FirstOrDefault(x => x.Id == m.ContractorID);
                if (contractor != null)
                {
                    m.PrimeContractorName = contractor.CompanyName;
                }
            }

            int[] ids = db.Projects.Select(x => x.PrimeContractorID).Distinct().ToArray();
            var list3 = (from c in db.Contractors
                         where String.IsNullOrEmpty(c.DCO) && ids.Any(y => y == c.Id)
                         select new MissingProjectDetailsModel
                         {
                             ContractorID = c.Id,
                             PrimeContractorName = c.CompanyName,
                             Comment = "DCO not assigned to contractor"
                         }).ToList();

            model = model.Union(list3);

            var list4 = (from cr in db.ClearanceRequests
                         join p in db.Projects on cr.ProjectId equals p.Id into crp
                         from p in crp.DefaultIfEmpty()
                         where p.Id == 0
                         select new MissingProjectDetailsModel
                         {
                             ProjectName = "CR: " + cr.Id + ", Project: " + cr.ProjectId + "  Data error.  Please contact IT.",
                             Comment = "Project does NOT exist for CR"
                         }).ToList();

            model = model.Union(list4);

            var list6 = (from p in db.Projects
                         where p.ContractAmount == null && p.DateClosed == null
                         select new MissingProjectDetailsModel
                         {
                             DCO = p.DCO,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             ContractorID = p.PrimeContractorID,
                             Comment = "Missing 'Contract amount'"
                         }).ToList();

            model = model.Union(list6);

            // CR sent to department but project is not closed
            var list7 = (from p in db.Projects
                         join cr in db.ClearanceRequests on p.Id equals cr.ProjectId
                         where cr.CurrentStatus.Contains("Sent to Department") && p.DateClosed == null
                         select new MissingProjectDetailsModel
                         {
                             DCO = p.DCO,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             ContractorID = p.PrimeContractorID,
                             Comment = "CR submitted but project not closed"
                         }).ToList();

            model = model.Union(list7);

            var list5 = (from p in db.Projects
                         where (p.StartDate == null || p.EndDate == null) && p.DateClosed == null
                         select new MissingProjectDetailsModel
                         {
                             DCO = p.DCO,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             ContractorID = p.PrimeContractorID,
                             Comment = "Missing 'Start' and/or 'End' date"
                         }).ToList();

            model = model.Union(list5);

            return PartialView("_missingProjectDetails", model);
        }

        public string GetMessages(int id)
        {
            string result = "";
            var messages = from mt in db.MessageThreads
                           join m in db.Messages on mt.Id equals m.ThreadId
                           where mt.ProjectId == id
                           orderby m.DateSent
                           select m;

            foreach (var m in messages.ToList())
            {
                result += "<div class='msgDate'>" + m.DateSent + "</div>";
                result += "<div class='msg'><label>" + m.Sender + ":</label> " + m.Text + "</div>";
            }

            return result;
        }

        public PartialViewResult HomeTabs()
        {
            string url = Request.Url.ToString().ToLower();
            int index = 5;
            if (url.Contains("manager1"))
                index = 0;
            else if (url.Contains("manager2"))
                index = 1;
            else if (url.Contains("manager3"))
                index = 2;
            else if (url.Contains("manager4"))
                index = 3;
            else if (url.Contains("dco"))
                index = 4;
            else if (url.Contains("messages"))
                index = 5;

            ViewBag.Index = index;
            ViewBag.InBox = GetMessageCount();

            return PartialView("_HomeTabs");
        }

        public ActionResult PrintPrimeDistribution()
        {
            using (ReportDocument rpt = new ReportDocument())
            {
                try
                {
                    string rptPath = Server.MapPath("~/Reports/");
                    rpt.Load(rptPath + "PrimeDistribution.rpt");
                    SetLoginInfo(rpt);

                    if (rpt.HasRecords)
                    {
                        string fileName = rptPath + "Temp/PrimeDistribution.pdf";

                        rpt.ExportToDisk(ExportFormatType.PortableDocFormat, fileName);

                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Exists)
                        {
                            return File(fileName, "application/pdf");
                        }
                    }
                    else
                    {
                        TempData["Message"] = "Error: There is no record to report.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "Error: " + ex.Message;
                }
            }

            return Redirect(Request.UrlReferrer.ToString());
        }

        private void SetParameterValues(ReportDocument rpt, string dco)
        {
            if (!String.IsNullOrEmpty(dco))
                rpt.RecordSelectionFormula = "{rpt_DocumentReceived;1.DCO}='" + dco + "'";
        }

        public string PrintDocumentReceived(string dco)
        {
            string url = "";

            using (ReportDocument rpt = new ReportDocument())
            {
                try
                {
                    string rptPath = Server.MapPath("~/Reports/");
                    rpt.Load(rptPath + "DocumentReceived.rpt");

                    SetLoginInfo(rpt);
                    SetParameterValues(rpt, dco);

                    if (rpt.HasRecords)
                    {
                        string fileName = rptPath + "Temp/DocumentReceived.pdf";

                        rpt.ExportToDisk(ExportFormatType.PortableDocFormat, fileName);

                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Exists)
                        {
                            return Url.Content("~/Reports/Temp/DocumentReceived.pdf");
                        }
                    }
                    else
                    {
                        url = "Error: There is no record to report.";
                    }
                }
                catch (Exception ex)
                {
                    url = "Error: There was an error while creating report.";
                }
            }

            return url;
        }

    }
}