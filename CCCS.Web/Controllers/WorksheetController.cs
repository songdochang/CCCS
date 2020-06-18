using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using CCCS.Web.Models;
using System.Web.UI.WebControls;
using System.Globalization;
using Newtonsoft.Json;
using System.Reflection;
using PagedList;
using CCCS.Infrastructure;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Web;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Configuration;
using System.Data.SqlClient;
using CCCS.Core.Domain.Reference;
using CCCS.Core.Domain.Log;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Projects;
using CCCS.Core.Domain.Worksheets;
using CCCS.Web.Models.Worksheets;
using CCCS.Core.Domain.Common;

namespace CCCS.Controllers
{
    [Authorize]
    public class WorksheetController : BaseController
    {
        const int PAGE_SIZE = 12;
        // GET: Worksheet/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Worksheet worksheets = db.Worksheets.Find(id);
            if (worksheets == null)
            {
                return HttpNotFound();
            }
            return View(worksheets);
        }

        public ActionResult Index1(string dco, string listStyle, string payPeriod)
        {
            var model = new List<TimesheetModel>();

            ViewBag.COs = GetCOs(true);
            ViewBag.CurrentDCO = dco;

            List<SelectListItem> periods = GetPayPeriods();
            if (!periods.Exists(x => x.Value == payPeriod))
            {
                periods.Add(new SelectListItem { Text = payPeriod, Value = payPeriod });
            }
            ViewBag.PayPeriods = periods;

            ViewBag.Months = GetMonths();
            ViewBag.CurrentMonth = DateTime.Today.ToString("MM/yyyy");

            if (User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
                ViewBag.CurrentDCO = dco;
            }
            else
            {
                ViewBag.CurrentDCO = dco;

                if (String.IsNullOrEmpty(dco))
                {
                    return View(model);
                }
            }

            DateTime from, to;
            if (string.IsNullOrEmpty(payPeriod))
            {
                from = DateTime.Parse(periods[0].Value.Split('~')[0].Trim());
                to = DateTime.Parse(periods[0].Value.Split('~')[1].Trim());
                ViewBag.CurrentPayPeriod = periods[0].Value;
            }
            else
            {
                from = DateTime.Parse(payPeriod.Split('~')[0].Trim());
                to = DateTime.Parse(payPeriod.Split('~')[1].Trim());
                ViewBag.CurrentPayPeriod = payPeriod;
            }

            model = GetTimeWorksheets(dco, from, to);

            if (listStyle == "summary")
            {
                model = PrepareSummaryModel(model);
            }

            if (model.Count > 0)
            {
                TimesheetModel tr = new TimesheetModel
                {
                    Event = "Total",
                    Hours = new List<DayColumn>()
                };
                for (int i = 0; i < model[0].Hours.Count; i++)
                {
                    decimal total = 0.0m;
                    foreach (var m in model)
                    {
                        total += m.Hours[i].Hours;
                    }
                    DayColumn totalHour = new DayColumn
                    {
                        Day = model[0].Hours[i].Day,
                        DayOfWeek = model[0].Hours[i].DayOfWeek,
                        Hours = total
                    };

                    tr.Hours.Add(totalHour);
                }
                model.Add(tr);
            }
            else
            {
                TempData["Message"] = "Error: Data are unavailable for the selected dates.";
            }

            ViewBag.ListStyle = listStyle;

            return View(model);
        }

        private List<TimesheetModel> PrepareSummaryModel(List<TimesheetModel> list)
        {
            list = list.OrderByDescending(x => x.Event).ThenBy(x => x.Unit).ThenBy(x => x.Activity).ToList();
            List<TimesheetModel> model = new List<TimesheetModel>();

            string unit = "";
            string activity = "";
            foreach (var l in list)
            {
                if (unit != l.Unit || activity != l.Activity)
                {
                    unit = l.Unit;
                    activity = l.Activity;

                    TimesheetModel tm = new TimesheetModel
                    {
                        Event = l.Event,
                        Phase = l.Phase,
                        Unit = l.Unit,
                        Activity = l.Activity,
                        Hours = l.Hours
                    };
                    model.Add(tm);
                }
                else
                {
                    for (int i = 0; i < l.Hours.Count; i++)
                    {
                        model[model.Count - 1].Hours[i].Hours += l.Hours[i].Hours;
                    }
                }
            }

            model.OrderBy(x => x.Phase).ThenBy(x => x.Unit).ToList();

            return model;
        }

        private List<TimesheetModel> GetBillables(List<TimesheetModel> model, string dco, DateTime from, DateTime to)
        {
            var query = db.Worksheets.Where(x => x.DCO == dco && x.WorkDate >= from && x.WorkDate <= to && x.EventCode != null);
            var events = query.Select(x => x.EventCode).Distinct().ToArray();
            var units = (from w in query
                         join p in db.Projects on w.ProjectId equals p.Id
                         select p.Unit).Distinct().ToArray();
            var activities = query.Select(x => x.ActivityCode).Distinct().ToArray();

            foreach (var e in events)
            {
                foreach (var u in units)
                {
                    foreach (var a in activities)
                    {
                        var projects = (from p in db.Projects
                                        join q in query on p.Id equals q.ProjectId
                                        where p.Unit == u
                                        select p).Distinct().ToList();
                        foreach (var project in projects)
                        {
                            decimal totalHours = 0.0m;

                            TimesheetModel tsm = new TimesheetModel
                            {
                                Event = e,
                                Phase = project.Phase,
                                Unit = u,
                                Activity = a,
                                ProjectID = project.Id,
                                JOC = project.JOC,
                                ProjectName = project.ProjectName,
                                Hours = new List<DayColumn>()
                            };

                            for (int i = 0; i <= to.Day - from.Day; i++)
                            {
                                DateTime dt1 = from.AddDays(i);
                                DateTime dt2 = from.AddDays(i + 1);

                                var query1 = (from w in db.Worksheets
                                              join p in db.Projects on w.ProjectId equals p.Id into wp
                                              from x in wp.DefaultIfEmpty()
                                              where w.DCO == dco && w.WorkDate >= dt1 && w.WorkDate < dt2 && w.ProjectId == project.Id
                                              select new { EventCode = w.EventCode, Unit = x.Unit, ActivityCode = w.ActivityCode, ProjectID = project.Id, Hours = w.Hours, Minutes = w.Minutes }).ToList();
                                int? h = query1.Where(x => x.EventCode == e && x.Unit == u && x.ActivityCode == a && x.ProjectID == project.Id).Sum(x => (int?)x.Hours);
                                int? m = query1.Where(x => x.EventCode == e && x.Unit == u && x.ActivityCode == a && x.ProjectID == project.Id).Sum(x => (int?)x.Minutes);
                                h = h ?? 0;
                                m = m ?? 0;
                                int hh = (int)h;
                                int mm = (int)m;
                                decimal val = (mm == 0) ? (decimal)hh : (decimal)hh + 0.5m;
                                DayColumn hour = new DayColumn
                                {
                                    Day = dt1.Day,
                                    DayOfWeek = dt1.DayOfWeek.ToString().Substring(0, 3),
                                    Hours = val
                                };

                                tsm.Hours.Add(hour);
                                totalHours += val;
                            }

                            if (totalHours > 0.0m)
                            {
                                model.Add(tsm);
                            } 
                        }
                    }
                }
            }

            model = model.OrderBy(x => x.Event).ThenBy(x => x.Phase).ThenBy(x => x.Unit).ThenBy(x => x.Activity).ThenBy(x => x.ProjectID).ToList();

            return model;
        }

        private List<TimesheetModel> GetNonBillables(List<TimesheetModel> model, string dco, DateTime from, DateTime to)
        {
            var query = db.Worksheets.Where(x => x.DCO == dco && x.WorkDate >= from && x.WorkDate <= to && x.EventCode == null);
            var units = query.Select(x => x.Unit).Distinct().ToArray();
            var activities = query.Select(x => x.Activity).Distinct().ToArray();

            foreach (var u in units)
            {
                foreach (var a in activities)
                {
                    decimal totalHours = 0.0m;

                    TimesheetModel tsm = new TimesheetModel
                    {
                        Phase = "C",
                        Unit = u,
                        Activity = db.Activities.FirstOrDefault(x => x.ID == a).Description,
                        Hours = new List<DayColumn>()
                    };

                    for (int i = 0; i <= to.Day - from.Day; i++)
                    {
                        DateTime dt1 = from.AddDays(i);
                        DateTime dt2 = from.AddDays(i + 1);

                        var query1 = (from w in db.Worksheets
                                      where w.DCO == dco && w.WorkDate >= dt1 && w.WorkDate < dt2 && w.EventCode == null && w.Activity == a
                                      select new { Unit = w.Unit, Hours = w.Hours, Minutes = w.Minutes }).ToList();
                        int? h = query1.Where(x => x.Unit == u).Sum(x => (int?)x.Hours);
                        int? m = query1.Where(x => x.Unit == u).Sum(x => (int?)x.Minutes);
                        h = h ?? 0;
                        m = m ?? 0;
                        int hh = (int)h;
                        int mm = (int)m;
                        decimal val = (mm == 0) ? (decimal)hh : (decimal)hh + 0.5m;
                        DayColumn hour = new DayColumn
                        {
                            Day = dt1.Day,
                            DayOfWeek = dt1.DayOfWeek.ToString().Substring(0, 3),
                            Hours = val
                        };

                        tsm.Hours.Add(hour);
                        totalHours += val;
                    }

                    if (totalHours > 0.0m)
                    {
                        model.Add(tsm);
                    }
                }
            }

            return model;
        }

        private List<TimesheetModel> GetTimeWorksheets(string dco, DateTime from, DateTime to)
        {
            int days = (int)(to - from).TotalDays;

            List<TimesheetModel> model = (days <= 16 && from.Month == to.Month) ? GetTimeWorksheetsByDay(dco, from, to) : GetTimeWorksheetsByMonth(dco, from, to);

            model = model.OrderByDescending(x => x.Event).ThenBy(x => x.Phase).ThenBy(x => x.Unit).ThenBy(x => x.Activity).ThenBy(x => x.ProjectID).ToList();

            return model;
        }

        private List<TimesheetModel> GetTimeWorksheetsByDay(string dco, DateTime from, DateTime to)
        {
            int days = (int)(to - from).TotalDays;
            string array = "";
            for (int i = 0; i <= days; i++)
            {
                DateTime dt = from.AddDays(i);

                array += "[" + dt.Day + "],";
            }
            array = array.Substring(0, array.Length - 1);

            List<TimesheetModel> model = new List<TimesheetModel>();

            DataSet ds = new DataSet();
            using (SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["ContractContext"].ConnectionString))
            {
                using (SqlCommand cmd = cnn.CreateCommand())
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    cmd.CommandText = "sp_TimeWorksheet";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@array", array));
                    cmd.Parameters.Add(new SqlParameter("@dco", dco));
                    cmd.Parameters.Add(new SqlParameter("@from", from));
                    cmd.Parameters.Add(new SqlParameter("@to", to));

                    adapter.Fill(ds);
                }
            }

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                decimal totalHours = 0.0m;
                int projectId = int.Parse(dr["ProjectID"].ToString());

                TimesheetModel tsm = new TimesheetModel
                {
                    Event = dr["EventCode"].ToString(),
                    Phase = dr["Phase"].ToString(),
                    Unit = dr["Unit"].ToString(),
                    Activity = dr["ActivityCode"].ToString(),
                    ProjectID = projectId,
                    JOC = dr["JOC"].ToString(),
                    Hours = new List<DayColumn>()
                };

                for (int i = 0; i <= days; i++)
                {
                    DateTime dt = from.AddDays(i);
                    string col = dt.Day.ToString();
                    Decimal hours = (String.IsNullOrEmpty(dr[col].ToString())) ? 0.0m : Decimal.Parse(dr[col].ToString());

                    DayColumn hour = new DayColumn
                    {
                        Day = dt.Day,
                        DayOfWeek = dt.DayOfWeek.ToString().Substring(0, 3),
                        Hours = hours
                    };

                    tsm.Hours.Add(hour);
                    totalHours += hours;
                }

                if (totalHours > 0.0m)
                {
                    model.Add(tsm);
                }
            }

            model = model.OrderByDescending(x => x.Event).ThenBy(x => x.Phase).ThenBy(x => x.Unit).ThenBy(x => x.Activity).ThenBy(x => x.ProjectID).ToList();

            return model;
        }

        private List<TimesheetModel> GetTimeWorksheetsByMonth(string dco, DateTime from, DateTime to)
        {
            int months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
            string array = "";
            for (int i = 0; i <= months; i++)
            {
                DateTime dt = from.AddMonths(i);

                array += "[" + dt.Year + dt.Month + "],";
            }
            array = array.Substring(0, array.Length - 1);

            List<TimesheetModel> model = new List<TimesheetModel>();

            DataSet ds = new DataSet();
            using (SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["ContractContext"].ConnectionString))
            {
                using (SqlCommand cmd = cnn.CreateCommand())
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    cmd.CommandText = "sp_TimeWorksheet";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@array", array));
                    cmd.Parameters.Add(new SqlParameter("@dco", dco));
                    cmd.Parameters.Add(new SqlParameter("@from", from));
                    cmd.Parameters.Add(new SqlParameter("@to", to));
                    cmd.Parameters.Add(new SqlParameter("@interval", "m"));

                    adapter.Fill(ds);
                }
            }

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                decimal totalHours = 0.0m;
                int projectId = int.Parse(dr["ProjectID"].ToString());

                TimesheetModel tsm = new TimesheetModel
                {
                    Event = dr["EventCode"].ToString(),
                    Phase = dr["Phase"].ToString(),
                    Unit = dr["Unit"].ToString(),
                    Activity = dr["ActivityCode"].ToString(),
                    ProjectID = projectId,
                    JOC = dr["JOC"].ToString(),
                    Hours = new List<DayColumn>()
                };

                for (int i = 0; i <= months; i++)
                {
                    DateTime dt = from.AddMonths(i);
                    string col = dt.Year.ToString() + dt.Month.ToString();
                    Decimal hours = (String.IsNullOrEmpty(dr[col].ToString())) ? 0.0m : Decimal.Parse(dr[col].ToString());

                    DayColumn hour = new DayColumn
                    {
                        Day = dt.Month,
                        DayOfWeek = dt.Year.ToString(),
                        Hours = hours
                    };

                    tsm.Hours.Add(hour);
                    totalHours += hours;
                }

                if (totalHours > 0.0m)
                {
                    model.Add(tsm);
                }
            }

            model = model.OrderByDescending(x => x.Event).ThenBy(x => x.Phase).ThenBy(x => x.Unit).ThenBy(x => x.Activity).ThenBy(x => x.ProjectID).ToList();

            return model;
        }

        [HttpPost]
        public ActionResult Index1(FormCollection form, string dco, string listStyle)
        {
            string payPeriod = form["PayPeriod"];
            string dateFrom = form["dateFrom"];
            string dateTo = form["dateTo"];

            if (!String.IsNullOrEmpty(dateFrom) && !String.IsNullOrEmpty(dateTo))
            {
                payPeriod = dateFrom + " ~ " + dateTo;
            }

            if (String.IsNullOrEmpty(Request.Form["PDF"]))
                return RedirectToAction("Index1", new { dco = dco, listStyle = listStyle, payPeriod = payPeriod });
            else
                return RedirectToAction("GetPdfWorksheetByDCO", new { dco = dco, payPeriod = payPeriod, listStyle = listStyle });
        }

        // GET: Worksheet
        public ActionResult Index2(string dco, string payPeriod)
        {
            if (string.IsNullOrEmpty(dco) && User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
            }

            ViewBag.COs = GetCOs(true);
            ViewBag.CurrentDCO = dco;

            List<SelectListItem> periods = GetPayPeriods();
            ViewBag.PayPeriods = periods;

            DateTime fromDt, to;
            if (string.IsNullOrEmpty(payPeriod))
            {
                fromDt = DateTime.Parse(periods[0].Value.Split('~')[0].Trim());
                to = DateTime.Parse(periods[0].Value.Split('~')[1].Trim());
                ViewBag.CurrentPayPeriod = periods[0].Value;
            }
            else
            {
                fromDt = DateTime.Parse(payPeriod.Split('~')[0].Trim());
                to = DateTime.Parse(payPeriod.Split('~')[1].Trim());
                ViewBag.CurrentPayPeriod = payPeriod;
            }

            var model = (from w in db.Worksheets
                         join p in db.Projects on w.ProjectId equals p.Id
                         where w.IsBillable == true && w.DCO == dco && w.WorkDate >= fromDt && w.WorkDate < to
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

            ViewBag.Editable = (to >= DateTime.Today);

            return View(model);
        }

        [HttpPost]
        public ActionResult Index2(string dco)
        {
            string payPeriod = Request.Form["PayPeriod"];
            string action = RouteData.Values["action"].ToString();

            return RedirectToAction(action, new { dco = dco, payPeriod = payPeriod });
        }

        // GET: Worksheet
        public ActionResult Index3(string dco, string payPeriod, string returnUrl = "")
        {
            if (string.IsNullOrEmpty(dco) && User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
            }

            ViewBag.COs = GetCOs(true);
            ViewBag.CurrentDCO = dco;

            List<SelectListItem> periods = GetPayPeriods();
            if (!periods.Exists(x => x.Value == payPeriod))
            {
                periods.Add(new SelectListItem { Text = payPeriod, Value = payPeriod });
            }
            ViewBag.PayPeriods = periods;

            DateTime from, to;
            if (string.IsNullOrEmpty(payPeriod))
            {
                from = DateTime.Parse(periods[0].Value.Split('~')[0].Trim());
                to = DateTime.Parse(periods[0].Value.Split('~')[1].Trim());
                ViewBag.CurrentPayPeriod = periods[0].Value;
            }
            else
            {
                from = DateTime.Parse(payPeriod.Split('~')[0].Trim());
                to = DateTime.Parse(payPeriod.Split('~')[1].Trim());
                ViewBag.CurrentPayPeriod = payPeriod;
            }

            var list = db.Worksheets.Where(x => x.IsBillable == false && x.DCO == dco && x.WorkDate >= from && x.WorkDate <= to)
                .OrderByDescending(x => x.WorkDate);

            List<NonBillableListModel> model = new List<NonBillableListModel>();
            foreach (var l in list.ToList())
            {
                NonBillableListModel m = new NonBillableListModel
                {
                    WorksheetID = l.Id,
                    WorkDate = l.WorkDate,
                    DCO = l.DCO,
                    ActivityCode = l.ActivityCode,
                    Unit = l.Unit,
                    Comment = l.Comment
                };

                var activity = db.Activities.FirstOrDefault(x => x.ID == l.Activity);
                if (activity != null)
                {
                    m.ActivityDescription = l.ActivityCode + " - " + activity.Description;
                }

                decimal hours = l.Hours + (l.Minutes / 60.0m);
                m.FormattedHours = hours.ToString("0.0");

                model.Add(m);
            }

            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        [HttpPost]
        public ActionResult Index3(string dco)
        {
            string payPeriod = Request.Form["payPeriod"];
            string action = RouteData.Values["action"].ToString();
            string dateFrom = Request.Form["dateFrom"];
            string dateTo = Request.Form["dateTo"];
            dateTo = (String.IsNullOrEmpty(dateTo)) ? DateTime.Today.ToShortDateString() : dateTo;

            if (!String.IsNullOrEmpty(dateFrom) && !String.IsNullOrEmpty(dateTo))
            {
                payPeriod = dateFrom + " ~ " + dateTo;
            }

            return RedirectToAction(action, new { dco = dco, payPeriod = payPeriod });
        }

        public ActionResult Index4(string dco, DateTime? from, DateTime? to)
        {
            if (string.IsNullOrEmpty(dco))
            {
                dco = UserProfile.UserInitial;
            }

            List<SelectListItem> periods = GetPayPeriods();
            ViewBag.PayPeriods = periods;

            from = (from == null) ? DateTime.Parse(periods[0].Value.Split('~')[0].Trim()) : from;
            to = (to == null) ? DateTime.Parse(periods[0].Value.Split('~')[1].Trim()).AddDays(1) : to;

            List<ActivityListModel> model = new List<ActivityListModel>();
            var list = db.ActivityLogs.Where(x => x.ExecutedBy == dco && x.ActivityDate >= from && x.ActivityDate < to)
                .OrderByDescending(x => x.ActivityDate).ToList();
            foreach (var l in list)
            {
                ActivityListModel m = GetActivityListModel(l);
                model.Add(m);
            }

            ViewBag.CurrentPayPeriod = ((DateTime)from).ToString("MM/dd/yyyy") + " ~ " + ((DateTime)to).ToString("MM/dd/yyyy");
            GetViewBags();

            return View(model);
        }

        [HttpPost]
        public ActionResult Index4(string dco)
        {
            string payPeriod = Request.Form["PayPeriod"];

            return RedirectToAction("Index4", new { dco = dco, payPeriod = payPeriod });
        }

        public ActionResult Index5(string dept = "", string fundOrg = "", int? page = 1)
        {
            var accountNumbers = db.AccountNumbers.ToList();

            if (!string.IsNullOrEmpty(dept))
            {
                accountNumbers = accountNumbers.Where(x => x.DepartmentID == dept).ToList();
            }
            if (!string.IsNullOrEmpty(fundOrg))
            {
                accountNumbers = accountNumbers.Where(x => x.FundOrg == fundOrg).ToList();
            }

            List<AccountNumberListModel> model = new List<AccountNumberListModel>();
            foreach (var ac in accountNumbers)
            {
                AccountNumberListModel m = new AccountNumberListModel
                {
                    ID = ac.ID,
                    FundOrg = ac.FundOrg,
                    SubaccountNo = ac.SubaccountNo,
                    DepartmentID = ac.DepartmentID,
                    Description = ac.Description,
                    AccountDescription = ac.Description,
                    Phase = ac.Phase,
                    UsedFor = ac.UsedFor,
                    IsActive = ac.IsActive,
                    AccountType = ac.AccountType,
                    ActivityCodes = new List<ActivityCode>()
                };

                if (!string.IsNullOrEmpty(ac.ActivityCodes))
                {
                    string[] codes = ac.ActivityCodes.Split(';');
                    foreach (string c in codes)
                    {
                        var activityCode = db.ActivityCodes.FirstOrDefault(x => x.Code == c);
                        if (activityCode != null)
                        {
                            m.ActivityCodes.Add(activityCode);
                        }
                    }
                }
                model.Add(m);
            }
            model = model.OrderBy(x => x.FundOrg).ThenBy(x => x.SubaccountNo).ToList();

            ViewBag.Departments = GetDepartments(true);
            ViewBag.FundOrgs = GetFundOrgs();
            ViewBag.CurrentDepartment = dept;
            ViewBag.CurrentFundOrg = fundOrg;

            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, PAGE_SIZE));
        }

        [HttpPost]
        public ActionResult Index5()
        {
            string dept = Request.Form["Department"];
            string fundOrg = Request.Form["FundOrg"];

            return RedirectToAction("Index5", new { dept = dept, fundOrg = fundOrg });
        }

        public ActionResult Index6(string month, string dept = "", string fundOrg = "", string returnUrl = "")
        {
            if (string.IsNullOrEmpty(month))
            {
                month = DateTime.Today.ToString("MM/yyyy");
            }

            var model = CommonHelper.GetDeptBillingModel(month, dept, fundOrg);

            ViewBag.Departments = GetDepartments(true);
            ViewBag.FundOrgs = GetFundOrgs();
            ViewBag.CurrentDepartment = dept;
            ViewBag.CurrentFundOrg = fundOrg;
            ViewBag.Months = GetMonths();
            ViewBag.CurrentMonth = month;
            ViewBag.ReturnUrl = returnUrl;

            List<SelectListItem> list = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(dept))
            {
                var query = db.EmailRecipients.Where(x => x.DepartmentId == dept);

                list = query.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToList();
            }
            ViewBag.Contacts = list;

            model = PrepareDepartmentalBillingModel(model);

            return View(model);
        }

        private List<DepartmentalBillingModel> PrepareDepartmentalBillingModel(List<DepartmentalBillingModel> model)
        {
            model = model.OrderBy(x => x.Unit).ThenBy(x => x.ActivityCode).ThenBy(x => x.JOC).ToList();
            List<DepartmentalBillingModel> newModel = new List<DepartmentalBillingModel>();

            string unit = "", activity = "", project = "";
            decimal unitTotal = 0.0m, activityTotal = 0.0m, projectTotal = 0.0m, grandTotal = 0.0m;
            for (int i = 0; i < model.Count; i++)
            {
                var m = model[i];
                unitTotal += m.TotalHours;
                activityTotal += m.TotalHours;
                projectTotal += m.TotalHours;
                grandTotal += m.TotalHours;

                if (i == 0)
                {
                    newModel.Add(m);
                }
                else
                {
                    if (m.Unit1 == model[i - 1].Unit1)
                    {
                        m.Unit = "";
                    }
                    if (m.ActivityCode1 == model[i - 1].ActivityCode1)
                    {
                        m.ActivityCode = "";
                    }

                    newModel.Add(m);
                }

                if (i == model.Count - 1)
                {
                    DepartmentalBillingModel dpm = new DepartmentalBillingModel
                    {
                        Unit = m.JOC1 + " Total",
                        ActivityCode = "Project Total",
                        TotalHours = projectTotal
                    };
                    newModel.Add(dpm);

                    dpm = new DepartmentalBillingModel
                    {
                        Unit = m.ActivityCode1 + " Total",
                        ActivityCode = "Activity Total",
                        TotalHours = activityTotal
                    };
                    newModel.Add(dpm);

                    dpm = new DepartmentalBillingModel
                    {
                        Unit = m.Unit1 + " Total",
                        ActivityCode = "Unit Total",
                        TotalHours = unitTotal
                    };
                    newModel.Add(dpm);

                    dpm = new DepartmentalBillingModel
                    {
                        Unit = "Grand Total",
                        ActivityCode = "Grand Total",
                        TotalHours = grandTotal
                    };
                    newModel.Add(dpm);
                }
                else
                {
                    if (model[i + 1].JOC != project)
                    {
                        DepartmentalBillingModel dpm = new DepartmentalBillingModel
                        {
                            Unit = m.JOC1 + " Total",
                            ActivityCode = "Project Total",
                            TotalHours = projectTotal
                        };

                        newModel.Add(dpm);

                        project = model[i + 1].JOC;
                        projectTotal = 0.0m;
                    }
                    if (model[i + 1].ActivityCode != activity)
                    {
                        DepartmentalBillingModel dpm = new DepartmentalBillingModel
                        {
                            Unit = m.ActivityCode1 + " Total",
                            ActivityCode = "Activity Total",
                            TotalHours = activityTotal
                        };

                        newModel.Add(dpm);

                        activity = model[i + 1].ActivityCode;
                        activityTotal = 0.0m;
                    }
                    if (model[i + 1].Unit != unit)
                    {
                        DepartmentalBillingModel dpm = new DepartmentalBillingModel
                        {
                            Unit = m.Unit1 + " Total",
                            ActivityCode = "Unit Total",
                            TotalHours = unitTotal
                        };

                        newModel.Add(dpm);

                        unit = model[i + 1].Unit;
                        unitTotal = 0.0m;
                    }
                }

            }

            return newModel;
        }

        [HttpPost]
        public ActionResult Index6(FormCollection form)
        {
            string dept = form["Department"];
            string fundOrg = form["FundOrg"];
            string month = form["Month"];
            string action = RouteData.Values["action"].ToString();

            if (form["button"] == "Send")
                return RedirectToAction("EmailDB", "Notice", new { month = month, dept = dept, fundOrg = fundOrg, returnUrl = Request.Params["returnUrl"] });

            return RedirectToAction(action, new { month = month, dept = dept, fundOrg = fundOrg });
        }

        private ActivityListModel GetActivityListModel(ActivityLog log)
        {
            ActivityListModel m = new ActivityListModel
            {
                ID = log.Id,
                ActivityDate = log.ActivityDate,
                ExecutedBy = log.ExecutedBy
            };

            if (log.EntityName == "Inspection" && log.Action == "Added")
            {
                m.Action = "Site Visit Requested";
            }
            else
            {
                m.Action = log.EntityName + " " + log.Action;
            }

            var entity = JsonConvert.DeserializeObject(log.Entity, Type.GetType(log.EntityType));
            foreach (var p in entity.GetType().GetProperties())
            {
                if (p.Name == "ProjectID")
                {
                    int id = int.Parse(p.GetValue(entity).ToString());

                    if (id > 0)
                    {
                        var project = db.Projects.Find(id);
                        m.ProjectID = id;
                        m.JOC = project.JOC;
                        m.ProjectName = project.ProjectName;
                        m.DCO = project.DCO;
                    }
                }
            }

            return m;
        }

        private List<SelectListItem> GetPayPeriods()
        {
            List<SelectListItem> list = new List<SelectListItem>();

            for (int i = 0; i > -3; i--)
            {
                DateTime dt0 = DateTime.Today.AddMonths(i);
                string txt1, from1, to1;
                string txt2, from2, to2;
                if (dt0.Day <= 15)
                {
                    from1 = dt0.ToString("MM/01/yyyy");
                    to1 = dt0.ToString("MM/15/yyyy");
                    to2 = DateTime.Parse(from1).AddDays(-1).ToString("MM/dd/yyyy");
                    from2 = DateTime.Parse(to2).ToString("MM/16/yyyy");
                }
                else
                {
                    from1 = dt0.ToString("MM/16/yyyy");
                    DateTime dt1 = DateTime.Parse(dt0.ToString("MM/01/yyyy"));
                    DateTime dt2 = dt1.AddMonths(1).AddDays(-1);
                    to1 = dt2.ToString("MM/dd/yyyy");
                    from2 = dt0.ToString("MM/01/yyyy");
                    DateTime dt3 = DateTime.Parse(from1);
                    to2 = dt3.AddDays(-1).ToString("MM/dd/yyyy");
                }

                txt1 = from1 + " ~ " + to1;
                list.Add(new SelectListItem { Text = txt1, Value = txt1 });
                txt2 = from2 + " ~ " + to2;
                list.Add(new SelectListItem { Text = txt2, Value = txt2 });
            }

            return list;
        }

        // GET: Worksheet/CreateBillable
        public ActionResult CreateBillable(int? id, string returnUrl = "")
        {
            string dco = User.Identity.Name;

            var model = new Worksheet
            {
                DCO = dco,
                WorkDate = DateTime.Today,
                EventCode = "099"
            };

            if (id != null)
            {
                Project project = db.Projects.Find(id);

                if (!User.IsInRole("DCO"))
                {
                    model.DCO = project.DCO;
                }

                model.ProjectId = (int)id;
                model.Phase = project.Phase;

                ViewBag.ProjectName = project.ProjectName;
                ViewBag.JOC = project.JOC;
                var contractor = db.Contractors.FirstOrDefault(x => x.Id == project.PrimeContractorID);
                ViewBag.PrimeContractor = (contractor != null) ? contractor.CompanyName : "";
            }

            GetViewBags(dco);
            GetContractors(id);

            ViewBag.Phases = GetPhases();
            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        // POST: Worksheet/CreateBillable

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBillable(FormCollection form, Worksheet worksheet, string returnUrl = "")
        {
            Project project = new Project();

            // This became necessary because Admin and Clerical may add comments and/or billable hours
            if (String.IsNullOrEmpty(worksheet.DCO) || worksheet.DCO.Length != 2)
            {
                if (User.IsInRole("DCO"))
                {
                    worksheet.DCO = UserProfile.UserInitial;
                }
                else
                {
                    project = db.Projects.FirstOrDefault(x => x.Id == worksheet.ProjectId);
                    if (project != null)
                    {
                        worksheet.DCO = project.DCO;
                    }
                }
            }
            worksheet.ActivityCode = form["ActivityCode"];

            if (ModelState.IsValid)
            {
                if (worksheet.ProjectId == null)
                {
                    project = db.Projects.FirstOrDefault(x => x.JOC == worksheet.JOC);

                    if (project == null)
                    {
                        TempData["Message"] = "Error: Entered project doesn't exist.  Please enter valid project ID.";
                        return RedirectToAction("CreateBillable");
                    }
                    else
                        worksheet.ProjectId = project.Id;
                }
                if (string.IsNullOrEmpty(worksheet.JOC))
                {
                    project = db.Projects.FirstOrDefault(x => x.Id == worksheet.ProjectId);
                    worksheet.JOC = project.JOC;
                }

                worksheet.Phase = project.Phase;
                worksheet.EventCode = "099";

                int contractorId;
                if (int.TryParse(form["Contractor"], out contractorId))
                    worksheet.ContractorId = contractorId;

                worksheet.Comment = form["Comment"];
                worksheet.Unit = project.Unit;

                int hour = int.Parse(form["Hour"]);
                int minute = int.Parse(form["Minute"]);
                worksheet.Hours = hour;
                worksheet.Minutes = minute;
                worksheet.IsBillable = true;
                worksheet.CreatedBy = UserProfile.UserInitial;

                db.Worksheets.Add(worksheet);

                decimal hours = hour + minute / 60.0m;
                project.HoursRemaining = project.HoursRemaining - hours;
                db.Entry(project).State = EntityState.Modified;

                db.SaveChanges();

                return Redirect(returnUrl);
            }
            else
            {
                GetViewBags(worksheet.DCO);
                ViewBag.Phases = GetPhases();
                ViewBag.ReturnUrl = returnUrl;

                return View(worksheet);
            }
        }

        // GET: Worksheet/Create
        public ActionResult CreateNonBillable(string returnUrl = "")
        {
            string dco = User.Identity.Name;

            var model = new Worksheet
            {
                DCO = dco,
                WorkDate = DateTime.Today
            };

            ViewBag.Activities = GetActivities();
            ViewBag.Hours = GetHours();
            ViewBag.Minutes = GetMinutes();
            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        // POST: Worksheet/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNonBillable(FormCollection form, Worksheet model)
        {
            string msg = "";
            model.Phase = "C";
            model.DCO = UserProfile.UserInitial;
            model.IsBillable = false;

            if (model.Hours == 0 && model.Minutes == 0)
            {
                msg = "Error: Hours cannot be amounted to 0.";
                ModelState.AddModelError("Time", msg);
            }

            if (ModelState.IsValid)
            {
                db.Worksheets.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index3");
            }

            ViewBag.Activities = GetActivities();
            ViewBag.Hours = GetHours();
            ViewBag.Minutes = GetMinutes();
            ViewBag.ReturnUrl = Request.UrlReferrer;

            TempData["Message"] = (String.IsNullOrEmpty(msg)) ? "Error: There was an error while saving non-billable activity." : msg;

            return View(model); ;
        }

        public ActionResult CreateAccount()
        {
            var model = new AccountNumber
            {
                AccountDescription = "CO-WIDE CONT COM MON SPRT"
            };
            ViewBag.Phases = GetPhases();
            ViewBag.ActivityCodes = GetActivityCodes(false);
            ViewBag.AccountTypes = GetAccountTypes();

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateAccount(FormCollection form, AccountNumber accountNumber)
        {
            if (ModelState.IsValid)
            {
                string unit = accountNumber.FundOrg + accountNumber.SubaccountNo;
                accountNumber.AccountNo = unit;

                string activityCodes = "";
                foreach (var ac in db.ActivityCodes)
                {
                    foreach (var key in form.Keys)
                    {
                        string val = form[key.ToString()];
                        if (val == ac.Code)
                        {
                            activityCodes += val + ";";
                        }
                    }
                }
                accountNumber.ActivityCodes = activityCodes;

                db.AccountNumbers.Add(accountNumber);
                db.SaveChanges();
            }

            return RedirectToAction("Index5");
        }

        public ActionResult EditAccount(int id)
        {
            AccountNumber accountNumber = db.AccountNumbers.Find(id);
            if (accountNumber == null)
            {
                return HttpNotFound();
            }

            ViewBag.Departments = GetDepartments(false);
            ViewBag.Phases = GetPhases();
            ViewBag.ActivityCodes = GetActivityCodes(false);
            ViewBag.AccountTypes = GetAccountTypes();

            return View(accountNumber);
        }

        [HttpPost]
        public ActionResult EditAccount(FormCollection form, AccountNumber accountNumber)
        {
            if (ModelState.IsValid)
            {
                string activityCodes = "";
                foreach (var ac in db.ActivityCodes)
                {
                    foreach (var key in form.Keys)
                    {
                        string val = form[key.ToString()];
                        if (val == ac.Code)
                        {
                            activityCodes += val + ";";
                        }
                    }
                }
                accountNumber.ActivityCodes = activityCodes;

                db.Entry(accountNumber).State = EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Index5");
        }

        // GET: Worksheet/EditBillable/5
        public ActionResult EditBillable(int? id, string returnUrl = "")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Worksheet worksheet = db.Worksheets.Find(id);
            if (worksheet == null)
            {
                return HttpNotFound();
            }

            GetViewBags(User.Identity.Name);
            ViewBag.ReturnUrl = returnUrl;

            int projectId = (int)worksheet.ProjectId;
            Project project = db.Projects.Find(projectId);
            if (project != null)
            {
                ViewBag.ProjectName = project.ProjectName;
            }
            GetContractors(projectId);

            return View(worksheet);
        }

        // POST: Worksheet/EditBillable/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBillable(FormCollection form, Worksheet ws, string submitButton, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                string commentText = form["Comment"].Trim();

                if (submitButton == "Save")
                {
                    ws.ActivityCode = form["ActivityCode"];
                    ws.Comment = commentText;

                    ws.Hours = int.Parse(form["Hour"]);
                    ws.Minutes = int.Parse(form["Minute"]);
                    ws.IsBillable = true;

                    db.Entry(ws).State = EntityState.Modified;
                    db.SaveChanges();

                    var project = db.Projects.Find(ws.ProjectId);
                    Decimal hours = db.Worksheets.Where(x => x.ProjectId == ws.ProjectId && x.IsBillable).Sum(x => x.Hours + x.Minutes / 60.0m);
                    project.HoursRemaining = project.HoursAvailable - hours;
                    db.Entry(project).State = EntityState.Modified;
                }
                else
                {
                    Worksheet newWs = new Worksheet
                    {
                        ProjectId = ws.ProjectId,
                        WorkDate = DateTime.Parse(form["WorkDate"]),
                        DCO = ws.DCO,
                        ActivityCode = form["ActivityCode"],
                        EventCode = ws.EventCode,
                        Phase = ws.Phase,
                        Comment = commentText,
                        Hours = int.Parse(form["Hour"]),
                        Minutes = int.Parse(form["Minute"]),
                        IsBillable = true
                    };

                    db.Worksheets.Add(newWs);
                }

                db.SaveChanges();

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Index2");
                else
                    return Redirect(returnUrl);
            }

            GetViewBags(User.Identity.Name);

            return View(ws);
        }

        // GET: Worksheet/EditNonBillable/5
        public ActionResult EditNonBillable(int? id, string returnUrl = "")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Worksheet worksheet = db.Worksheets.Find(id);
            if (worksheet == null)
            {
                return HttpNotFound();
            }

            ViewBag.Activities = GetActivities();
            ViewBag.HourList = GetHours();
            ViewBag.MinuteList = GetMinutes();
            ViewBag.ReturnUrl = returnUrl;

            return View(worksheet);
        }

        // POST: Worksheet/EditNonBillable/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditNonBillable(FormCollection form, Worksheet worksheet, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                string comment = form["Comment"];

                db.Entry(worksheet).State = EntityState.Modified;
                db.SaveChanges();

                if (String.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Index3");
                else
                    return Redirect(returnUrl);
            }

            return View(worksheet);
        }

        // GET: Worksheet/Delete/5
        public ActionResult Delete(int? id, string returnUrl = "")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Worksheet worksheets = db.Worksheets.Find(id);
            if (worksheets == null)
            {
                return HttpNotFound();
            }

            ViewBag.ReturnUrl = returnUrl;

            return View(worksheets);
        }

        // POST: Worksheet/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, string returnUrl)
        {
            Worksheet worksheet = db.Worksheets.Find(id);
            if (worksheet.IsBillable)
            {
                Project project = db.Projects.Find(worksheet.ProjectId);
                Decimal hours = worksheet.Hours + worksheet.Minutes / 60.0m;
                project.HoursRemaining = project.HoursRemaining + hours;
                db.Entry(project).State = EntityState.Modified;
            }

            db.Worksheets.Remove(worksheet);
            db.SaveChanges();

            return Redirect(returnUrl);
        }

        public ActionResult DeleteAccount(int id)
        {
            AccountNumber ac = db.AccountNumbers.Find(id);
            if (ac != null)
            {
                db.AccountNumbers.Remove(ac);
                db.SaveChanges();
            }

            return RedirectToAction("Index5");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string GetMonthYear(DateTime dt)
        {
            string month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dt.Month);
            string year = dt.Year.ToString();

            return month + " " + year;
        }

        private void GetViewBags(string dco = "")
        {
            var projects = new List<ListItem>();
            projects.Add(new ListItem { Text = "- All Projects -", Value = "" });
            var query1 = (string.IsNullOrEmpty(dco)) ? db.Projects : db.Projects.Where(x => x.DCO == dco);
            foreach (var p in query1.OrderBy(x => x.ProjectName))
            {
                projects.Add(new ListItem { Text = p.JOC, Value = p.Id.ToString() });
            }
            ViewBag.Projects = projects;

            ViewBag.Departments = GetDepartments(true);

            var months = new List<ListItem>();
            for (int i = 0; i > -6; i--)
            {
                DateTime month = DateTime.Today.AddMonths(i);
                string monthName = GetMonthYear(month);
                months.Add(new ListItem { Text = monthName, Value = month.ToString("MMyyyy") });
            }
            ViewBag.Months = months;

            ViewBag.COs = GetCOs();
            ViewBag.CurrentDCO = dco;

            var list = (string.IsNullOrEmpty(dco)) ? db.Projects : db.Projects.Where(x => x.DCO == dco);
            var JOCs = list.OrderBy(x => x.JOC).Select(x => new SelectListItem { Text = x.JOC + " (" + x.ProjectName + ")", Value = x.Id.ToString() }).ToList();
            JOCs.Insert(0, new SelectListItem { Text = "- Select project -", Value = "" });
            ViewBag.JOCs = JOCs;

            ViewBag.ActivityCodes = GetActivityCodes(true);

            ViewBag.Activities = GetActivities();

            var events = new List<SelectListItem>();

            events.Add(new SelectListItem { Text = "- Select event -", Value = "" });
            events.Add(new SelectListItem { Text = "012", Value = "012" });
            events.Add(new SelectListItem { Text = "019", Value = "019" });
            events.Add(new SelectListItem { Text = "099", Value = "099" });

            ViewBag.EventCodes = events;

            ViewBag.Phases = GetPhases();

            ViewBag.Hours = GetHours();

            ViewBag.Minutes = GetMinutes();

            var categories = new List<ListItem>();
            categories.Add(new ListItem { Text = "- Select category -", Value = "" });

            foreach (var c in db.CommentCategories.OrderBy(x => x.Category))
            {
                categories.Add(new ListItem { Text = c.Category, Value = c.CommentCategoryId.ToString() });
            }
            ViewBag.CommentCategories = categories;
        }

        private List<SelectListItem> GetDepartments(bool includeAll)
        {
            var departments = new List<SelectListItem>();

            if (includeAll)
            {
                departments.Add(new SelectListItem { Text = "- All Departments -", Value = "" });
            }
            else
            {
                departments.Add(new SelectListItem { Text = "- Select department -", Value = "" });
            }

            foreach (var d in db.Departments.ToList())
            {
                string txt = d.DepartmentName + " (" + d.DepartmentId + ")";
                departments.Add(new SelectListItem { Text = txt, Value = d.DepartmentId });
            }

            return departments;
        }

        private List<SelectListItem> GetFundOrgs()
        {
            var fundOrgs = new List<SelectListItem>();

            fundOrgs.Add(new SelectListItem { Text = "- Select fund org -", Value = "" });

            foreach (var a in db.AccountNumbers.Select(x => x.FundOrg).Distinct().ToList())
            {
                fundOrgs.Add(new SelectListItem { Text = a.ToString(), Value = a.ToString() });
            }

            return fundOrgs;
        }

        private List<SelectListItem> GetAccountTypes()
        {
            var fundOrgs = new List<SelectListItem>();

            fundOrgs.Add(new SelectListItem { Text = "- Select account type -", Value = "" });

            fundOrgs.Add(new SelectListItem { Text = "Client Accounts", Value = "Client Account" });
            fundOrgs.Add(new SelectListItem { Text = "ED-Receivable", Value = "ED-Receivable" });
            fundOrgs.Add(new SelectListItem { Text = "Internal Acts", Value = "Internal Acts" });

            return fundOrgs;
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

        private List<SelectListItem> GetActivities()
        {
            var activities = new List<SelectListItem>();
            activities.Add(new SelectListItem { Text = "- Select activity -", Value = "" });
            var activityList = db.Activities.Where(x => x.IsActive).ToList();
            foreach (var a in activityList)
            {
                activities.Add(new SelectListItem { Text = a.Description, Value = a.ID.ToString() });
            }

            return activities;
        }

        private List<SelectListItem> GetActivityCodes(bool dropDownList)
        {
            var activityCodes = new List<SelectListItem>();

            if (dropDownList)
            {
                activityCodes.Add(new SelectListItem { Text = "- Select activity code -", Value = "" });
            }

            foreach (var ac in db.ActivityCodes)
            {
                string txt = ac.Code + " - " + ac.Description;
                activityCodes.Add(new SelectListItem { Text = txt, Value = ac.Code });
            }

            return activityCodes;
        }

        public PartialViewResult WorksheetList(int id)
        {
            var model = (from w in db.Worksheets
                         join p in db.Projects on w.ProjectId equals p.Id
                         where w.IsBillable == true && p.Id == id
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

                m.Editable = true;
                if (User.IsInRole("Clerical2"))
                {
                    m.Editable = false;
                }
                else if (User.IsInRole("DCO"))
                {
                    if (!String.IsNullOrEmpty(m.Worksheet.CreatedBy) && m.Worksheet.CreatedBy != UserProfile.UserInitial)
                    {
                        m.Editable = false;
                    }
                }
            }

            return PartialView("_WorksheetList", model);
        }

        private Project GetProject(int? id)
        {
            Project project = new Project();

            if (id != null)
            {
                project = db.Projects.FirstOrDefault(x => x.Id == id);
            }

            return project;
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

        public ActionResult GetContractors(int projectId)
        {
            var contractors = new List<SelectListItem>();

            if (projectId > 0)
            {
                contractors = (from p in db.Projects
                               join ct in db.Contracts on p.Id equals ct.ProjectId
                               join c in db.Contractors on ct.ContractorId equals c.Id
                               where p.Id == projectId
                               select new SelectListItem
                               {
                                   Text = c.CompanyName,
                                   Value = c.Id.ToString(),
                                   Selected = (c.Id == p.PrimeContractorID)
                               }).ToList();
            }

            ViewBag.Contractors = contractors;

            return Json(contractors);
        }

        private decimal GetHours(int? id)
        {
            decimal hours = 0.0m;

            if (id != null)
            {
                var obj = (from w in db.Worksheets
                           where w.Id == id
                           select new { Hours = w.Hours, Minutes = w.Minutes }).FirstOrDefault();

                hours = obj.Hours + obj.Minutes / 60m;
            }

            return hours;
        }

        private List<SelectListItem> GetHours()
        {
            var hours = new List<SelectListItem>();
            for (int i = 0; i <= 10; i++)
            {
                hours.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });
            }

            return hours;
        }

        private List<SelectListItem> GetMinutes()
        {
            var minutes = new List<SelectListItem>();
            minutes.Add(new SelectListItem { Text = "00", Value = "00" });
            minutes.Add(new SelectListItem { Text = "30", Value = "30" });

            return minutes;
        }


        public string GetActivityCodes(int id)
        {
            string result = "";

            var activity = db.Activities.FirstOrDefault(x => x.ID == id);
            result = activity.ActivityCode + ";" + activity.FundOrg;

            return result;
        }

        public string GetEntityData(int id)
        {
            string result = "<table class=\"table\"><tbody>";

            var log = db.ActivityLogs.FirstOrDefault(x => x.Id == id);
            if (!string.IsNullOrEmpty(log.Entity))
            {
                var entity = JsonConvert.DeserializeObject(log.Entity, Type.GetType(log.EntityType));
                foreach (var p in entity.GetType().GetProperties())
                {
                    result += FormattedResult(p, entity);
                }

                result += "</tbody></table>";

                return result;
            }

            return "Data not available";
        }

        private string FormattedResult(PropertyInfo p, object entity)
        {
            string[] hides = { "ID", "Category", "CommentID", "ContractorID", "InspectionID", "ProjectID", "UserID", "WorksheetID" };

            Dictionary<string, string> displays = new Dictionary<string, string>()
            {
                { "CommentText", "Comment Text" },
                { "DateClosed", "Date Closed" },
                { "DateDCO", "Date sent to CO" },
                { "DateDepartment", "Date sent to Department" },
                { "DateManager", "Date sent to Manager" },
                { "DateReceived", "Date Received" },
                { "DateRegistered", "Date/Time" },
                { "DateRequested", "Date Requested" },
                { "DateUploaded", "Date Uploaded" },
                { "DocumentType", "Document Type" },
                { "EmployeeID", "Employee ID" },
                { "EndDate", "End Date" },
                { "FederalFunds", "Federal Funds" },
                { "FileName", "File Name" },
                { "FilePath", "File Path" },
                { "HoursAvailable", "Hours Available" },
                { "HoursRemaining", "Hours Remaining" },
                { "LastUpdateDate", "Last Update Date" },
                { "NumberNotice", "Number Nitice" },
                { "NumberSubcontractors", "Number Subcontractors" },
                { "MainAccount", "Main Account" },
                { "PrimeContractorID", "Prime Contractor ID" },
                { "ProjectName", "Comment Text" },
                { "ProjectType", "Project Type" },
                { "StartDate", "Start Date" },
                { "SubAccount", "Sub Account" },
                { "UserColor", "User Color" },
                { "UserInitial", "User Initial" }
            };

            string result = "";

            if (!hides.Any(x => x == p.Name))
            {
                result += "<tr><td class=\"col-md-3 title-cell\">";

                var val = displays.FirstOrDefault(x => x.Key == p.Name).Value;
                if (string.IsNullOrEmpty(val))
                    result += p.Name;
                else
                    result += val;

                result += "</td><td class=\"col-md-9\">" + p.GetValue(entity) + "</td></tr>";
            }

            return result;
        }

        public JsonResult GetAccountDescription(string q)
        {
            string[] data = db.AccountNumbers.Where(x => x.Description.StartsWith(q)).Select(x => x.Description)
                .Distinct().ToArray();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetJOC(string q)
        {
            List<Project> data = db.Projects.Where(x => x.JOC.StartsWith(q)).OrderBy(x => x.JOC).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetComment(string q)
        {
            string[] data = db.Worksheets.Where(x => x.Comment.StartsWith(q)).Select(x => x.Comment).Distinct().ToArray();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetExcelWorksheetByDCO(string dco, string month)
        {
            if (String.IsNullOrEmpty(dco))
            {
                return Redirect(Request.UrlReferrer.ToString());
            }

            var model = new List<TimesheetModel>();

            string[] mmyyyy = month.Split('/');
            DateTime from = DateTime.Parse(mmyyyy[0] + "/01/" + mmyyyy[1]);
            DateTime to = from.AddMonths(1).AddDays(-1);

            model = GetBillables(model, dco, from, to);

            string filePath = GetExcelWorksheet(model, dco, from, to);

            return File(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public ActionResult GetPdfWorksheetByDCO(string dco, string payPeriod, string listStyle)
        {
            if (String.IsNullOrEmpty(dco))
            {
                if (User.IsInRole("DCO"))
                    return Redirect(Request.UrlReferrer.ToString());
                else
                    dco = "";
            }

            string[] mmyyyy = payPeriod.Split('~');
            DateTime from = DateTime.Parse(mmyyyy[0].Trim());
            DateTime to = DateTime.Parse(mmyyyy[1].Trim());

            using (ReportDocument rpt = new ReportDocument())
            {
                try
                {
                    string rptPath = Server.MapPath("~/Reports/");

                    if (listStyle == "summary")
                    {
                        rpt.Load(rptPath + "TimeWorksheetSummary.rpt");
                    }
                    else
                    {
                        rpt.Load(rptPath + "TimeWorksheetDetails.rpt");
                    }

                    SetParameterValues(rpt, dco, from, to);

                    if (rpt.HasRecords)
                    {
                        string fileName = (listStyle == "summary")
                            ? rptPath + "Temp/TimeWorksheetSummary_" + dco + ".pdf"
                            : rptPath + "Temp/TimeWorksheetDetails_" + dco + ".pdf";

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

        private void SetParameterValues(ReportDocument rpt, string dco, DateTime from, DateTime to)
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

            ParameterFieldDefinitions crParameterFieldDefinitions = rpt.DataDefinition.ParameterFields;
            ParameterDiscreteValue crParameterDiscreteValue = new ParameterDiscreteValue();
            ParameterValues crParameterValues = new ParameterValues();

            ParameterFieldDefinition p1 = crParameterFieldDefinitions["@DCO"];
            crParameterDiscreteValue.Value = dco;
            crParameterValues.Add(crParameterDiscreteValue);
            p1.ApplyCurrentValues(crParameterValues);

            ParameterFieldDefinition p2 = crParameterFieldDefinitions["@from"];
            crParameterDiscreteValue.Value = from;
            crParameterValues.Add(crParameterDiscreteValue);
            p2.ApplyCurrentValues(crParameterValues);

            ParameterFieldDefinition p3 = crParameterFieldDefinitions["@to"];
            crParameterDiscreteValue.Value = to;
            crParameterValues.Add(crParameterDiscreteValue);
            p3.ApplyCurrentValues(crParameterValues);
        }

        private string GetExcelWorksheet(List<TimesheetModel> data, string dco, DateTime from, DateTime to)
        {
            string path = Server.MapPath("~/Files/WorksheetByDCO/");
            DirectoryInfo outputDir = new DirectoryInfo(path);
            string dt = DateTime.Today.ToShortDateString().Replace("/", "");
            string destPath = string.Concat(path, dt, ".xlsx");

            FileInfo template = new FileInfo(path + "Template/template.xlsx");
            FileInfo newFile = new FileInfo(destPath);

            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(destPath);
            }

            using (ExcelPackage xlPackage = new ExcelPackage(newFile, template))
            {
                ExcelWorksheet worksheet = xlPackage.Workbook.Worksheets[1];

                if (worksheet != null)
                {
                    int row = 2;
                    int cols = data[0].Hours.Count;

                    string name = db.UserProfiles.FirstOrDefault(x => x.UserInitial == dco).FullName;
                    string title = String.Format("Worksheet for {0} ({1} ~ {2})", name, from.ToShortDateString(), to.ToShortDateString());
                    worksheet.Cells[1, 1, 1, 6].Merge = true;
                    worksheet.Cells[1, 1].Value = title;

                    worksheet.Cells[row, 7 + cols, row, 10 + cols].Clear();

                    foreach (var d in data)
                    {
                        row++;

                        worksheet.Cells[row, 2].Value = d.Phase;
                        worksheet.Cells[row, 3].Value = d.Unit;
                        worksheet.Cells[row, 4].Value = d.Activity;
                        worksheet.Cells[row, 5].Value = d.JOC;
                        worksheet.Cells[row, 6].Value = d.ProjectName;

                        for (int i = 0; i < cols; i++)
                        {
                            decimal hours = d.Hours[i].Hours;
                            if (hours > 0.0m)
                            {
                                worksheet.Cells[row, 7 + i].Value = hours.ToString("#.0");
                            }
                        }
                    }

                    using (var range = worksheet.Cells[row + 1, 1, row + 1, 6])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                    for (int i = 0; i < cols; i++)
                    {
                        decimal total = 0.0m;
                        foreach (var d in data)
                        {
                            decimal hours = d.Hours[i].Hours;
                            total += hours;
                        }

                        if (total > 0.0m)
                        {
                            worksheet.Cells[row + 1, 7 + i].Value = total.ToString("#.0");
                        }
                    }

                    xlPackage.Save();
                }
            }

            return destPath;
        }

        public JsonResult GetWorksheetUrl(string id, string unit, string activity, string day, string payPeriod)
        {
            string period = payPeriod.Split(' ')[0];
            string mmddyyyy = period.Substring(0, 3) + day + period.Substring(5, 5);
            DateTime dt = DateTime.Parse(mmddyyyy);
            int projectId = int.Parse(id);

            string uf = Request.UrlReferrer.ToString();
            var qs = uf.Substring(uf.IndexOf('?') + 1).Split('&');
            string dco = qs[0].ToUpper().Replace("DCO=", "");

            var query = db.Worksheets.Where(x => x.WorkDate == dt);

            string url = "";
            if (id != "0")
            {
                query = query.Where(x => x.ProjectId == projectId && x.ActivityCode == activity);

                if (query.ToList().Count == 1)
                {
                    int worksheetId = query.ToList()[0].Id;
                    string returnUrl = "/worksheet/index1?dco=" + dco + "&payPeriod=" + payPeriod;
                    UrlHelper u = new UrlHelper(this.ControllerContext.RequestContext);
                    url = u.Action("EditBillable", "Worksheet", new { id = worksheetId, returnUrl = returnUrl });
                }
            }
            else
            {
                string returnUrl = "/worksheet/index1?dco=" + dco + "&payPeriod=" + payPeriod;
                UrlHelper u = new UrlHelper(this.ControllerContext.RequestContext);
                url = u.Action("Index3", "Worksheet", new { payPeriod = payPeriod, returnUrl = returnUrl });
            }

            return Json(url, JsonRequestBehavior.AllowGet);
        }
    }
}
