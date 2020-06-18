using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CCCS.Web.Models;
using System.Web.UI.WebControls;
using PagedList;
using System.IO;
using CCCS.Infrastructure;
using CrystalDecisions.CrystalReports.Engine;
using System.Configuration;
using CrystalDecisions.Shared;
using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Web.Models.ClearanceRequests;
using CCCS.Web.Models.Users;
using System.Web.Hosting;
using System.Diagnostics;
using Ghostscript.NET;
using Ghostscript.NET.Processor;
using CCCS.Core.Data;
using CCCS.Core.Domain.Common;

namespace CCCS.Controllers
{
    [Authorize]
    public class ClearanceRequestController : BaseController
    {
        const int PAGE_SIZE = 15;
        private readonly IRepository<Cache> _cacheRepository;

        public ClearanceRequestController(IRepository<Cache> cacheRepository)
        {
            _cacheRepository = cacheRepository;
        }

        public ClearanceRequestController()
        {
            var helper = new CommonHelper(_cacheRepository);
            ViewBag.Exceptions = helper.GetCrExceptionsCount();
        }

        // GET: ClearanceRequestModel
        public ActionResult Index1(string sortOrder, string dco, int? page)
        {
            List<ClearanceRequestModel> model = ClearanceRequestModel.GetModel(false);

            if (!string.IsNullOrEmpty(dco))
            {
                model = model.Where(x => x.DCO == dco).ToList();
            }
            ViewBag.CO = dco;
            ViewBag.Total = model.Count;

            model = SortModel(model, sortOrder, page);

            GetViewBags();

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Index1(FormCollection form)
        {
            string searchString = form["searchString"];
            string dco = form["dco"];
            string pdf = form["PDF"];

            if (!String.IsNullOrEmpty(searchString))
            {
                var model = ClearanceRequestModel.Search(searchString);

                GetViewBags();
                ViewBag.SearchString = searchString;

                return View(model.ToPagedList(1, PAGE_SIZE * 10));
            }
            else if (!String.IsNullOrEmpty(pdf))
            {
                return RedirectToAction("GetPdfPendingCR");
            }
            else
            {
                return RedirectToAction("Index1", new { dco = dco });
            }
        }

        public ActionResult GetPdfPendingCR()
        {
            using (ReportDocument rpt = new ReportDocument())
            {
                try
                {
                    string rptPath = Server.MapPath("~/Reports/");

                    rpt.Load(rptPath + "PendingCR.rpt");

                    SetParameterValues(rpt);

                    if (rpt.HasRecords)
                    {
                        string fileName = rptPath + "Temp/PendingCR.pdf";
                        rpt.ExportToDisk(ExportFormatType.PortableDocFormat, fileName);

                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Exists)
                        {
                            return Redirect("~/Download.ashx?f=" + fileName);
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

        private void SetParameterValues(ReportDocument rpt)
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
                }
            }

            rpt.SetDatabaseLogon(uid, pwd, svr, database);
        }

        public ActionResult Index2(string sortOrder, string dco, int? page, string from, string to)
        {
            List<ClearanceRequestModel> list = ClearanceRequestModel.GetModel(true);

            if (!string.IsNullOrEmpty(dco))
            {
                list = list.Where(x => x.DCO == dco).ToList();
            }
            ViewBag.CO = dco;

            if (!String.IsNullOrEmpty(from) || !String.IsNullOrEmpty(to))
            {
                ViewBag.DateRange = "Dates: " + from + " ~ " + to;
            }

            DateTime dt1, dt2;
            if (DateTime.TryParse(from, out dt1))
            {
                ViewBag.FromDate = dt1;
            }
            else
            {
                dt1 = DateTime.MinValue;
            }
            if (!DateTime.TryParse(to, out dt2))
            {
                dt2 = DateTime.Today;
            }
            list = list.Where(x => x.DateClosed >= dt1 && x.DateClosed <= dt2).ToList();
            ViewBag.ListTotal = list.Count;
            ViewBag.ToDate = dt2;

            list = SortModel(list, sortOrder, page);

            //Stats
            decimal total = (decimal)list.Where(x => x.TotalCharged > 0.0m).Sum(x => x.TotalCharged);
            ViewBag.Total = total.ToString("#.0"); ;
            var cnt = list.Where(x => x.TotalCharged > 0.0m).Count();
            ViewBag.Count = cnt;
            if (cnt > 0)
            {
                var avg = total / cnt;
                ViewBag.Average = avg.ToString("#.0");
            }
            else
            {
                ViewBag.Average = "N/A";
            }

            GetViewBags();

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            var model = list.ToPagedList(pageNumber, pageSize);
            foreach (var m in model)
            {
                var ws = db.Worksheets.Where(x => x.ProjectId == m.ProjectID).ToList();
                if (ws.Count > 0)
                {
                    m.TotalCharged = ws.Sum(x => x.Hours + x.Minutes / 60.0m);
                }
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Index2(FormCollection form)
        {
            string searchString = form["searchString"];
            string dco = form["dco"];
            var from = form["fromDate"];
            var to = form["toDate"];

            if (!string.IsNullOrEmpty(searchString))
            {
                var model = ClearanceRequestModel.Search(searchString, true);

                GetViewBags();
                ViewBag.SearchString = searchString;

                return View(model.ToPagedList(1, PAGE_SIZE * 10));
            }
            else
            {
                if (!String.IsNullOrEmpty(form["button"]) && form["button"].Contains("Reset"))
                {
                    dco = "";
                    from = "";
                    to = DateTime.Today.ToShortDateString();
                }

                return RedirectToAction("Index2", new { dco = dco, from = from, to = to });
            }
        }

        public ActionResult Index3()
        {
            List<ClearanceRequestListModel> model = new List<ClearanceRequestListModel>();
            List<SelectListItem> list = GetUsers(string.Empty);

            DateTime from = DateTime.Parse(DateTime.Today.AddMonths(-6).ToString("MM/01/yyyy"));
            if (!String.IsNullOrEmpty(Request.Form["Month"]))
            {
                string[] mo = Request.Form["Month"].Split('/');
                from = DateTime.Parse(mo[0] + "/01/" + mo[1]);
            }
            ViewBag.CurrentMonth = from.ToString("MM/yyyy");
            DateTime dt = DateTime.Parse(DateTime.Today.ToString("MM/01/yyyy"));

            do
            {
                DateTime begin = dt;
                DateTime end = begin.AddMonths(1);
                var query = from p in db.Projects
                            join cr in db.ClearanceRequests on p.Id equals cr.ProjectId
                            where cr.DateModified >= begin && cr.DateModified < end && cr.CurrentStatus.Contains("Sent to Department")
                            select new { DCO = (cr.ProcessedBy == null) ? p.DCO : cr.ProcessedBy };

                ClearanceRequestListModel clm = new ClearanceRequestListModel
                {
                    Month = dt.Month,
                    Year = dt.Year,
                    TotalClearanceRequests = query.Count()
                };

                List<KeyValuePair<string, int>> counts = new List<KeyValuePair<string, int>>();
                foreach (var l in list)
                {
                    int value = query.Where(x => x.DCO == l.Value).Count();

                    KeyValuePair<string, int> count = new KeyValuePair<string, int>(l.Value, value);
                    counts.Add(count);
                }

                clm.Counts = counts;

                model.Add(clm);
                dt = dt.AddMonths(-1);
            } while (dt.Month >= from.Month && dt.Year >= from.Year);

            ViewBag.Months = GetMonths();

            return View(model);
        }

        public ActionResult Index4(int? page)
        {
            var list = (from f in db.PublicProfiles
                        orderby f.DateRegistered descending
                        select new RegistrationApprovalModel
                        {
                            UserID = f.UserID,
                            Name = f.Name,
                            Department = f.Department,
                            Title = f.Title,
                            DateRegistered = f.DateRegistered,
                            DateModified = f.DateModified,
                            IsActive = f.IsActive
                        }).ToList();

            List<RegistrationApprovalModel> model = new List<RegistrationApprovalModel>();
            foreach (var l in list)
            {
                var user = UserManager.Users.FirstOrDefault(x => x.Id == l.UserID);
                if (user != null)
                {
                    l.Email = user.Email;
                    l.PhoneNumber = user.PhoneNumber;

                    model.Add(l);
                }
            }

            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, PAGE_SIZE));
        }

        public ActionResult Index5(string sortOrder, string dco, int? page)
        {
            DateTime dt = DateTime.Today.AddDays(-30);
            List<ClearanceRequestModel> model = ClearanceRequestModel.GetModel(false);
            model = model.Where(x => x.DateRequested < dt).OrderBy(x => x.DateRequested).ToList();

            if (!string.IsNullOrEmpty(dco))
            {
                model = model.Where(x => x.DCO == dco).ToList();
            }

            ViewBag.COs = GetCOs(true);
            ViewBag.CO = dco;
            ViewBag.Total = model.Count;

            model = SortModel(model, sortOrder, page);

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Index6(string sortOrder, string dco, int? page)
        {
            List<ClearanceRequestModel> model = ClearanceRequestModel.GetRejectedModel();

            if (!string.IsNullOrEmpty(dco))
            {
                model = model.Where(x => x.DCO == dco).ToList();
            }

            var from = Request.Form["dateFrom"];
            var to = Request.Form["dateTo"];

            if (!String.IsNullOrEmpty(from) || !String.IsNullOrEmpty(to))
            {
                ViewBag.DateRange = "Dates: " + from + " ~ " + to;
            }

            DateTime dt1, dt2;
            if (!DateTime.TryParse(from, out dt1))
            {
                dt1 = DateTime.MinValue;
            }
            if (!DateTime.TryParse(to, out dt2))
            {
                dt2 = DateTime.MaxValue;
            }
            model = model.Where(x => x.DateRejected >= dt1 && x.DateRejected <= dt2).ToList();

            ViewBag.COs = GetCOs(false);
            ViewBag.CO = dco;
            ViewBag.Total = model.Count;

            model = SortModel(model, sortOrder, page);

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Index7(bool refreshCache = false)
        {
            var model = CommonHelper.GetCrExceptions(refreshCache);

            return View(model);
        }

        [HttpPost]
        public ActionResult Index7(FormCollection form)
        {
            int id = int.Parse(form["id"]);
            string comment = form["Comment"];
            string dco = form["dco"];

            ClearanceRequestException newEx = new ClearanceRequestException
            {
                ClearanceRequestId = id,
                Comment = comment,
                DateCommented = DateTime.Now,
                DCO = UserProfile.UserInitial
            };

            db.ClearanceRequestExceptions.Add(newEx);
            db.SaveChanges();

            return RedirectToAction("Index7", new { refreshCache = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private void GetViewBags()
        {
            var types = new List<ListItem>();
            types.Add(new ListItem { Text = "- Capital & Non-Capical -", Value = "" });
            types.Add(new ListItem { Text = "Capital Project", Value = "0" });
            types.Add(new ListItem { Text = "Non-Capital Project", Value = "1" });
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

            var phases = new List<SelectListItem>();
            phases.Add(new SelectListItem { Text = "- Select phase -", Value = "" });
            phases.Add(new SelectListItem { Text = "A", Value = "A" });
            phases.Add(new SelectListItem { Text = "B", Value = "B" });
            phases.Add(new SelectListItem { Text = "C", Value = "C" });
            phases.Add(new SelectListItem { Text = "P", Value = "P" });
            ViewBag.Phases = phases;

            var contractors = new List<ListItem>();
            contractors.Add(new ListItem { Text = "- Select prime contractor -", Value = "" });
            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                contractors.Add(new ListItem { Text = c.CompanyName, Value = c.Id.ToString() });
            }
            ViewBag.PrimeContractors = contractors;
        }

        private void GetPrimeContractors()
        {
            var contractors = new List<ListItem>();
            contractors.Add(new ListItem { Text = "- Select prime contractor -", Value = "" });

            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                contractors.Add(new ListItem { Text = c.CompanyName, Value = c.Id.ToString() });
            }
            ViewBag.PrimeContractors = contractors;
        }

        public ActionResult Reject(int id)
        {
            ViewBag.ProjectID = id;

            return View();
        }

        [HttpPost]
        public ActionResult Reject(FormCollection form)
        {
            int id = int.Parse(form["ProjectID"]);
            string status = "Rejected by Manager";

            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);
            request.DateModified = DateTime.Now;
            request.CurrentStatus = status;
            db.Entry(request).State = EntityState.Modified;
            db.SaveChanges();

            ClearanceRequestLog log = new ClearanceRequestLog
            {
                ClearanceRequestId = request.Id,
                Date = (DateTime)request.DateModified,
                Activity = status,
                Comment = form["Comment"]
            };

            db.ClearanceRequestLogs.Add(log);
            db.SaveChanges();

            return RedirectToAction("Details5", "Project", new { id = id });
        }

        public ActionResult ActivateRegistration(string id, bool activate, string returnUrl)
        {
            var profile = db.PublicProfiles.FirstOrDefault(x => x.UserID == id);

            profile.DateModified = DateTime.Now;
            profile.IsActive = activate;
            db.Entry(profile).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return Redirect(returnUrl);
        }

        [HttpPost]
        public ActionResult AddExplanation(string dco)
        {
            int id = int.Parse(Request.Form["ClearanceRequestID"]);
            string comment = Request.Form["comment"];

            ClearanceRequestException newEx = new ClearanceRequestException
            {
                ClearanceRequestId = id,
                Comment = comment,
                DateCommented = DateTime.Now,
                DCO = dco
            };

            db.ClearanceRequestExceptions.Add(newEx);
            db.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult FormCR(FormCollection form)
        {
            string msg = "";
            string status = "";
            int id = int.Parse(form["projectID"]);
            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);

            try
            {
                if (ModelState.IsValid)
                {
                    DateTime now = (String.IsNullOrEmpty(form["DateClosed"])) ? DateTime.Now : DateTime.Parse(form["DateClosed"]);
                    string comment = form["Comment"].Trim();

                    if (form["submit"].Contains("Approve"))
                    {
                        status = "Approved by Manager";
                        msg = "Clearance Request was approved successfully.";

                        if (!String.IsNullOrEmpty(comment))
                        {
                            string subject = "CR was approved with a note.";
                            MessageHelper.SendMessage(id, subject, comment, UserProfile.UserInitial);
                        }
                    }
                    else if (form["submit"].Contains("Reject"))
                    {
                        status = "Rejected by Manager";
                        msg = "Clearance Request was rejected.";

                        string subject = "CR was rejected.";
                        MessageHelper.SendMessage(id, subject, comment, UserProfile.UserInitial);
                    }
                    else
                    {
                        status = "Sent to Manager";
                        msg = "Clearance Request was sent to manager successfully.";

                        var project = db.Projects.Find(id);
                        project.DateClosed = now;
                        db.Entry(project).State = EntityState.Modified;
                    }

                    request.DateModified = now;
                    request.CurrentStatus = status;
                    request.ProcessedBy = UserProfile.UserInitial;
                    db.Entry(request).State = EntityState.Modified;

                    ClearanceRequestLog log = new ClearanceRequestLog
                    {
                        ClearanceRequestId = request.Id,
                        Date = now
                    };
                    if (!string.IsNullOrEmpty(comment))
                    {
                        log.Comment = comment;
                    }
                    log.Activity = status;
                    db.ClearanceRequestLogs.Add(log);
                    db.SaveChanges();

                    if (form["submit"].Contains("Approve"))
                    {
                        var reqForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);
                        if (reqForm != null)
                        {
                            reqForm.SmDate = now;

                            db.Entry(reqForm).State = EntityState.Modified;
                        }
                        else
                        {
                            var newForm = CommonHelper.NewCrForm(id);
                            newForm.SmDate = now;

                            db.ClearanceRequestForms.Add(newForm);
                        }
                    }
                    else if (form["submit"].Contains("Reject"))
                    {
                        var project = db.Projects.Find(id);
                        project.DateClosed = null;
                        db.Entry(project).State = EntityState.Modified;

                        var reqForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);
                        if (reqForm == null)
                        {
                            var newForm = CommonHelper.NewCrForm(id);

                            db.ClearanceRequestForms.Add(newForm);
                        }
                    }
                    else
                    {
                        string dco = form["DcoCR1"];
                        string dcoName = db.UserProfiles.FirstOrDefault(x => x.UserInitial == dco).FullName;
                        var reqForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);

                        if (reqForm != null)
                        {
                            if (form["answer"] == "Yes")
                            {
                                reqForm.IsCleared = 1;
                                //safety for blank comment
                                if (String.IsNullOrEmpty(comment))
                                {
                                    comment = GetCrComment(id);
                                }
                            }
                            else if (form["answer"] == "No")
                                reqForm.IsCleared = 2;

                            reqForm.DcoName = dcoName;
                            reqForm.DateClearedByDCO = now;
                            reqForm.Comments = comment;

                            db.Entry(reqForm).State = EntityState.Modified;
                        }
                        else
                        {
                            var newForm = CommonHelper.NewCrForm(id);
                            if (form["answer"] == "Yes")
                            {
                                newForm.IsCleared = 1;
                                //safety for blank comment
                                if (String.IsNullOrEmpty(comment))
                                {
                                    comment = GetCrComment(id);
                                }
                            }
                            else if (form["answer"] == "No")
                                newForm.IsCleared = 2;

                            newForm.DcoName = dcoName;
                            newForm.DateClearedByDCO = now;
                            newForm.Comments = comment;

                            db.ClearanceRequestForms.Add(newForm);
                        }
                    }

                    db.SaveChanges();

                    string filePath = HostingEnvironment.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".PDF";
                    FileInfo fi = new FileInfo(filePath);
                    if (!fi.Exists)
                    {
                        filePath = CommonHelper.CreateCR(id);
                        CommonHelper.Upload(filePath);
                    }

                }
            }
            catch (Exception ex)
            {
                msg = "Error: There was an error.  " + ex.Message;
            }

            if (form["submit"].Contains("to department"))
            {
                return RedirectToAction("EmailCR", "Notice", new { id = id });
            }
            else
            {
                TempData["Message"] = msg;

                return Redirect(Request.UrlReferrer.ToString());
            }
        }

        [HttpPost]
        public ActionResult EditCR(FormCollection form, string returnUrl)
        {
            int projectID = int.Parse(form["projectID1"]);
            string requestedBy = form["RequestedBy"];
            string title = form["Title"];
            string email = form["Email"];

            var crForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == projectID);
            crForm.RequestedBy = requestedBy;
            crForm.Title = title;
            crForm.Email = email;
            db.Entry(crForm).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Message"] = "Clearance Request was saved successfully.";

            return Redirect(returnUrl);
        }

        public string GetCrComment(int id)
        {
            return CommonHelper.GetCrComment(id);
        }

        public JsonResult GetCrData(int id)
        {
            var crForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);

            return Json(crForm);
        }

        public ActionResult ViewDocument(int id, string returnUrl = "")
        {
            string filePath = CommonHelper.CreateCR(id);

            if (!String.IsNullOrEmpty(filePath))
            {
                return Redirect("~/Files/Clearance_Request/Forms/ClearanceRequest_" + id.ToString() + ".pdf");
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        public ActionResult ViewJPG(int id, string returnUrl = "")
        {
            string filePath = HostingEnvironment.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".pdf";
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                filePath = CommonHelper.CreateCR(id);
            }
            PdfToJpg(filePath);

            if (!String.IsNullOrEmpty(filePath))
            {
                return Redirect("~/Files/Clearance_Request/Forms/ClearanceRequest_" + id.ToString() + ".jpg");
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        private void PdfToJpg(string pdfFile)
        {
            string imagePath = HostingEnvironment.MapPath("~/Files/Clearance_Request/Forms/");

            GhostscriptVersionInfo gv = GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL);

            using (GhostscriptProcessor processor = new GhostscriptProcessor(gv, true))
            {
                List<string> switches = new List<string>();

                switches.Add("-empty");
                switches.Add("-dSAFER");
                switches.Add("-dBATCH");
                switches.Add("-dNOPAUSE");
                switches.Add("-dNOPROMPT");
                switches.Add(@"-sFONTPATH=" + System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts));
                //switches.Add("-dFirstPage=" + pageFrom.ToString());
                //switches.Add("-dLastPage=" + pageTo.ToString());
                switches.Add("-sDEVICE=jpeg");
                switches.Add("-r96");
                switches.Add("-dTextAlphaBits=4");
                switches.Add("-dGraphicsAlphaBits=4");

                //switches.Add("-sDEVICE=pdfwrite");

                switches.Add(@"-sOutputFile=" + pdfFile.ToLower().Replace(".pdf", ".jpg"));
                switches.Add(@"-f");
                switches.Add(pdfFile);

                processor.StartProcessing(switches.ToArray(), null);
            }
        }
    }
}
