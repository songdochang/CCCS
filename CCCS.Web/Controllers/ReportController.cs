using CCCS.Core.Domain.Documents;
using CCCS.Data;
using CCCS.Infrastructure;
using CCCS.Web.Models;
using CCCS.Web.Models.ClearanceRequests;
using CCCS.Web.Models.Documents;
using CCCS.Web.Models.Projects;
using CCCS.Web.Models.Report;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace CCCS.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        const int PAGE_SIZE = 15;

        public ActionResult PastDue1(string dco, string dept, bool refresh = false)
        {
            List<NonComplianceModel> list;

            string key = String.Format(PASTDUE_LIST_KEY, "CURRENT", dept);
            if (!CacheHelper.Get<List<NonComplianceModel>>(key, out list) || refresh)
            {
                list = CommonHelper.GetPastDueDocumentModel(dept);

                foreach (var l in list)
                {
                    DateTime asof = (l.DateReceived == null) ? DateTime.Now : (DateTime)l.DateReceived;

                    if (l.DateRequired != null)
                    {
                        var dt = (DateTime)l.DateRequired.Value;
                        l.PastDueDays = (asof - dt).Days;
                        l.PastDueMonths = l.PastDueDays / (365 / 12);
                    }
                }

                CacheHelper.Add<List<NonComplianceModel>>(list, key, 60.0);
            }

            ViewBag.Dept = dept;

            List<NonComplianceModel> model = (from n in list
                                              group n by new
                                              {
                                                  n.DCO,
                                                  n.JOC,
                                                  n.ProjectName,
                                                  n.ProjectID,
                                                  n.StartDate,
                                                  n.EndDate,
                                                  n.DepartmentID
                                              } into g
                                              select new NonComplianceModel
                                              {
                                                  DCO = g.Key.DCO,
                                                  JOC = g.Key.JOC,
                                                  ProjectName = g.Key.ProjectName,
                                                  ProjectID = g.Key.ProjectID,
                                                  StartDate = g.Key.StartDate,
                                                  EndDate = g.Key.EndDate,
                                                  DepartmentID = g.Key.DepartmentID,
                                                  NumberOfDocs = g.Count(),
                                                  PastDueMonths = g.Max(x => x.PastDueMonths)
                                              }).ToList();

            if (!String.IsNullOrEmpty(dco))
            {
                model = model.Where(x => x.DCO == dco).ToList();
                ViewBag.SelectedValue = dco;
            }

            model = model.OrderByDescending(x => x.PastDueMonths).ThenBy(x => x.EndDate).ToList();

            return View("PastDue1", model);
        }

        [HttpPost]
        public ActionResult PastDue1(FormCollection form, bool excluded = false)
        {
            string dco = form["dco"];
            string dept = form["dept"];
            string action = RouteData.Values["action"].ToString();

            if (form["button"] == "Send")
                return RedirectToAction("EmailDPD", "Notice", new { dept = dept, returnUrl = Request.Params["returnUrl"] });
            else if (form["button"] == "PDF")
                return RedirectToAction("GetPastDueDocumentsByProjectReport", "Report", new { dept = dept });

            return RedirectToAction(action, new { dco = dco, dept = dept });
        }

        public ActionResult PastDue2(string dept, string message = "")
        {
            List<NonComplianceModel> list;

            string key = String.Format(PASTDUE_LIST_KEY, "CURRENT", dept);
            if (!CacheHelper.Get<List<NonComplianceModel>>(key, out list))
            {
                list = CommonHelper.GetPastDueDocumentModel(dept);

                foreach (var l in list)
                {
                    DateTime asof = (l.DateReceived == null) ? DateTime.Now : (DateTime)l.DateReceived;

                    if (l.DateRequired != null)
                    {
                        var dt = (DateTime)l.DateRequired.Value;
                        l.PastDueDays = (asof - dt).Days;
                        l.PastDueMonths = l.PastDueDays / (365 / 12);
                    }
                }

                list = list.OrderByDescending(x => x.PastDueMonths).ThenBy(x => x.ContractorName).ThenBy(x => x.DocumentType).ToList();

                CacheHelper.Add<List<NonComplianceModel>>(list, key, 60.0);
            }

            ViewBag.Dept = dept;

            ViewBag.TotalPastDue = list.Where(x => x.DateReceived == null).Count();
            return View(list);
        }

        public ActionResult PastDue3(string dept, bool refresh = false)
        {
            List<NonComplianceModel> list;

            string key = String.Format(PASTDUE_LIST_KEY, "EXCLUDED", dept);
            if (!CacheHelper.Get<List<NonComplianceModel>>(key, out list) || refresh)
            {
                list = CommonHelper.GetExcludedPastDueDocumentModel(dept);

                foreach (var l in list)
                {
                    DateTime asof = (l.DateReceived == null) ? DateTime.Now : (DateTime)l.DateReceived;

                    if (l.DateRequired != null)
                    {
                        var dt = (DateTime)l.DateRequired.Value;
                        l.PastDueDays = (asof - dt).Days;
                        l.PastDueMonths = l.PastDueDays / (365 / 12);
                    }
                }

                CacheHelper.Add<List<NonComplianceModel>>(list, key, 60.0);
            }

            ViewBag.Dept = dept;

            ViewBag.TotalPastDue = list.Where(x => x.DateReceived == null).Count();
            return View(list);
        }


        public ActionResult BillableByActivity(bool isBillable, int since, string fromDate, string toDate, string message = "")
        {
            if (isBillable)
            {
                ViewBag.Title = "Billable by Activity";
                ViewBag.IsBillable = true;
            }
            else
            {
                ViewBag.Title = "Non-Billable by Activity";
                ViewBag.IsBillable = false;
            }

            ViewBag.Since = since;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View();
        }

        public PartialViewResult PartialBillableByActivity(bool isBillable, int since, string fromDate, string toDate)
        {
            if (String.IsNullOrEmpty(fromDate))
                fromDate = DateTime.Parse("1/1/" + DateTime.Today.Year).ToShortDateString();

            if (String.IsNullOrEmpty(toDate))
                toDate = DateTime.Today.ToShortDateString();

            if (since == 30)
            {
                fromDate = DateTime.Today.AddDays(-30).ToShortDateString();
            }
            else if (since == 3)
            {
                fromDate = DateTime.Today.AddMonths(-3).ToShortDateString();
            }

            DataTable model = null;
            if (isBillable)
            {
                model = GetBillableModel(fromDate, toDate);
                ViewBag.IsBillable = true;
            }
            else
            {
                model = GetNonBillableModel(fromDate, toDate);
                ViewBag.IsBillable = false;

                foreach (DataRow row in model.Rows)
                {
                    int id = int.Parse(row[0].ToString());
                    row[0] = db.Activities.FirstOrDefault(x => x.ID == id).Description;
                }
            }

            ViewBag.FromDate = fromDate;
            ViewBag.Todate = toDate;

            return PartialView("_PartialBillableByActivity", model);
        }

        private DataTable GetBillableModel(string startDate = "", string endDate = "", string activityCode = "")
        {
            List<PivotRow<String, String, Decimal>> activities = null;
            List<string> cos = null;
            DateTime dt1 = DateTime.Parse(startDate);
            DateTime dt2 = (String.IsNullOrEmpty(endDate)) ? DateTime.Today : DateTime.Parse(endDate);

            var worksheets = (String.IsNullOrEmpty(activityCode))
                        ? db.Worksheets.Where(x => x.IsBillable == true && x.WorkDate >= dt1 && x.WorkDate <= dt2).ToList()
                        : db.Worksheets.Where(x => x.IsBillable == true && x.WorkDate >= dt1 && x.WorkDate <= dt2 && x.ActivityCode == activityCode).ToList();

            activities = (from w in worksheets
                          group w by w.ActivityCode into wGroup
                          select new PivotRow<String, String, Decimal>
                          {
                              ObjectId = wGroup.Select(g => g.ActivityCode).FirstOrDefault(),
                              Attributes = wGroup.OrderBy(x => x.DCO).Select(g => g.DCO),
                              Values = wGroup.OrderBy(x => x.DCO).Select(g => g.Hours + g.Minutes / 60.0m)
                          }).ToList();

            // Get the list of attributes.
            ApplicationRole role = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO");
            string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();
            cos = db.UserProfiles.Where(x => memberIDs.Any(y => y == x.UserID)).Select(x => x.UserInitial).ToList();
            // Get the CO's who processed worksheet since start date
            List<string> cos2 = db.Worksheets
                .Where(x => x.WorkDate >= dt1 && x.WorkDate <= dt2 && !String.IsNullOrEmpty(x.DCO) && x.IsBillable)
                .Select(x => x.DCO).Distinct().ToList();
            cos = cos.Union(cos2).ToList();

            return PivotRow<String, String, Decimal>.GetPivotTable(cos, activities);
        }

        private DataTable GetNonBillableModel(string fromDate = "", string toDate = "", string activity = "")
        {
            List<PivotRow<String, String, Decimal>> activities = null;
            List<string> cos = null;
            DateTime dt1 = DateTime.Parse(fromDate);
            DateTime dt2 = (String.IsNullOrEmpty(toDate)) ? DateTime.Today : DateTime.Parse(toDate);

            int act = (String.IsNullOrEmpty(activity)) ? 0 : db.Activities.FirstOrDefault(x => x.Description == activity).ID;
            var worksheets = (String.IsNullOrEmpty(activity))
                        ? db.Worksheets.Where(x => x.IsBillable == false && x.WorkDate >= dt1 && x.WorkDate <= dt2).ToList()
                        : db.Worksheets.Where(x => x.IsBillable == false && x.WorkDate >= dt1 && x.WorkDate <= dt2 && x.Activity == act).ToList();

            activities = (from w in worksheets
                          group w by w.Activity into wGroup
                          select new PivotRow<String, String, Decimal>
                          {
                              ObjectId = wGroup.Select(g => g.Activity).FirstOrDefault().ToString(),
                              Attributes = wGroup.OrderBy(x => x.DCO).Select(g => g.DCO),
                              Values = wGroup.OrderBy(x => x.DCO).Select(g => g.Hours + g.Minutes / 60.0m)
                          }).ToList();

            // Get the list of attributes.
            ApplicationRole role = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO");
            string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();
            cos = db.UserProfiles.Where(x => memberIDs.Any(y => y == x.UserID)).Select(x => x.UserInitial).ToList();
            // Get the CO's who processed worksheet since start date
            List<string> cos2 = db.Worksheets
                .Where(x => x.WorkDate >= dt1 && x.WorkDate <= dt2 && !String.IsNullOrEmpty(x.DCO) && !x.IsBillable)
                .Select(x => x.DCO).Distinct().ToList();
            cos = cos.Union(cos2).ToList();

            return PivotRow<String, String, Decimal>.GetPivotTable(cos, activities);
        }

        public ActionResult ChartByActivity(bool isBillable, string activity, int since, string fromDate, string toDate)
        {
            if (since == 30)
            {
                fromDate = DateTime.Today.AddDays(-30).ToShortDateString();
            }
            else if (since == 3)
            {
                fromDate = DateTime.Today.AddMonths(-3).ToShortDateString();
            }

            var table = (isBillable) ? GetBillableModel(fromDate, toDate, activity) : GetNonBillableModel(fromDate, toDate, activity);
            var data = new Dictionary<string, decimal>();
            for (int i = 1; i < table.Columns.Count; i++)
            {
                var columnName = table.Columns[i].ColumnName;
                var value = table.Rows[0][i].ToString();
                if (!String.IsNullOrEmpty(value))
                    data.Add(columnName, Decimal.Parse(value));
            }

            var chart = new Chart();

            chart.Titles.Add(new Title(
                    activity,
                    Docking.Top,
                    new Font("Helvetica Neue", 10f, FontStyle.Bold),
                    Color.Black
                )
            );
            chart.Titles[0].Alignment = ContentAlignment.TopLeft;

            var area = new ChartArea();
            area.InnerPlotPosition.Auto = false;
            area.InnerPlotPosition.Height = 60;
            area.InnerPlotPosition.Width = 60;
            area.InnerPlotPosition.X = 0;
            area.InnerPlotPosition.Y = 0;
            chart.ChartAreas.Add(area);

            // create and customize your data series.
            var series = new Series();
            foreach (var item in data)
            {
                series.Points.AddXY(item.Key, item.Value);
                var color = db.UserProfiles.FirstOrDefault(x => x.UserInitial == item.Key).UserColor;
                if (color != null)
                {
                    series.Points[series.Points.Count - 1].Color = ColorTranslator.FromHtml(color.ToUpper());
                }
            }
            series.Label = "#VALX-#PERCENT{P0}";
            series.Font = new Font("Segoe UI", 8.0f, FontStyle.Bold);
            series.ChartType = SeriesChartType.Pie;
            series["PieLabelStyle"] = "Inside";
            series["PieDrawingStyle"] = "SoftEdge";

            chart.Series.Add(series);

            var returnStream = new MemoryStream();
            chart.ImageType = ChartImageType.Png;
            chart.SaveImage(returnStream);
            returnStream.Position = 0;

            return new FileStreamResult(returnStream, "image/png");
        }

        public ActionResult ActivityByMonth(int since = 3)
        {
            ViewBag.Since = since;

            return View();
        }

        public PartialViewResult PartialActivityByMonth(int monthAgo)
        {
            DateTime sd = DateTime.Parse(DateTime.Today.Month + "/01/" + DateTime.Today.Year);
            DateTime startDate = sd.AddMonths((-1) * monthAgo);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            DataTable model = GetActivityByMonthModel(startDate, endDate);

            return PartialView("_PartialActivityByMonth", model);
        }

        public ActionResult ChartActivityByMonth(bool isBillable, string activity, int since, string dt)
        {
            string startDate = dt;
            if (since == 30)
            {
                startDate = DateTime.Today.AddDays(-30).ToShortDateString();
            }
            else if (since == 3)
            {
                startDate = DateTime.Today.AddMonths(-3).ToShortDateString();
            }
            else if (since == 6)
            {
                startDate = DateTime.Today.AddMonths(-6).ToShortDateString();
            }

            var table = (isBillable) ? GetBillableModel(startDate, "", activity) : GetNonBillableModel(startDate, "", activity);
            var data = new Dictionary<string, decimal>();
            for (int i = 1; i < table.Columns.Count; i++)
            {
                var columnName = table.Columns[i].ColumnName;
                var value = table.Rows[0][i].ToString();
                if (!String.IsNullOrEmpty(value))
                    data.Add(columnName, Decimal.Parse(value));
            }

            var chart = new Chart();

            chart.Titles.Add(new Title(
                    activity,
                    Docking.Top,
                    new Font("Helvetica Neue", 10f, FontStyle.Bold),
                    Color.Black
                )
            );
            chart.Titles[0].Alignment = ContentAlignment.TopLeft;

            var area = new ChartArea();
            area.InnerPlotPosition.Auto = false;
            area.InnerPlotPosition.Height = 60;
            area.InnerPlotPosition.Width = 60;
            area.InnerPlotPosition.X = 0;
            area.InnerPlotPosition.Y = 0;
            chart.ChartAreas.Add(area);

            // create and customize your data series.
            var series = new Series();
            foreach (var item in data)
            {
                series.Points.AddXY(item.Key, item.Value);
                var color = db.UserProfiles.FirstOrDefault(x => x.UserInitial == item.Key).UserColor;
                if (color != null)
                {
                    series.Points[series.Points.Count - 1].Color = ColorTranslator.FromHtml(color.ToUpper());
                }
            }
            series.Label = "#VALX-#PERCENT{P0}";
            series.Font = new Font("Segoe UI", 8.0f, FontStyle.Bold);
            series.ChartType = SeriesChartType.Pie;
            series["PieLabelStyle"] = "Inside";
            series["PieDrawingStyle"] = "SoftEdge";

            chart.Series.Add(series);

            var returnStream = new MemoryStream();
            chart.ImageType = ChartImageType.Png;
            chart.SaveImage(returnStream);
            returnStream.Position = 0;

            return new FileStreamResult(returnStream, "image/png");
        }

        private DataTable GetActivityByMonthModel(DateTime startDate, DateTime endDate)
        {
            // Get the list of attributes.
            ApplicationRole role = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO");
            string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();
            List<String> dcos = db.UserProfiles.Where(x => memberIDs.Any(y => y == x.UserID))
                .OrderBy(x => x.UserInitial).Select(x => x.UserInitial).ToList();
            // Get the CO's who processed CR regardless of user role or status
            List<string> dcos2 = db.ClearanceRequests
                .Where(x => x.DateModified >= startDate && x.DateModified <= endDate
                        && !String.IsNullOrEmpty(x.ProcessedBy) && x.CurrentStatus.Contains("Sent to Department"))
                .Select(x => x.ProcessedBy).Distinct().ToList();

            dcos = dcos.Union(dcos2).ToList();

            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("Activity");

            foreach (string d in dcos)
            {
                dt.Columns.Add(d);
            }

            // CR Received
            var query1 = db.ClearanceRequests
                .Where(x => x.DateRequested >= startDate && x.DateRequested <= endDate);

            DataRow dr1 = dt.NewRow();
            dr1[0] = "CR Received";

            foreach (string d in dcos)
            {
                var cnt = query1.Where(x => x.ProcessedBy == d).Count();

                dr1[d] = cnt;
            }
            dt.Rows.Add(dr1);

            // CR processed
            var query2 = db.ClearanceRequests
                .Where(x => x.CurrentStatus == "Sent to Department" && x.DateModified >= startDate && x.DateModified <= endDate);

            DataRow dr2 = dt.NewRow();
            dr2[0] = "CR Processed";

            foreach (string d in dcos)
            {
                var cnt = query2.Where(x => x.ProcessedBy == d).Count();

                dr2[d] = cnt;
            }
            dt.Rows.Add(dr2);

            // CR Pending
            var query3 = ClearanceRequestModel.GetModel(false);

            DataRow dr3 = dt.NewRow();
            dr3[0] = "CR Pending";

            foreach (string d in dcos)
            {
                var cnt = query3.Where(x => x.DCO == d).Count();

                dr3[d] = cnt;
            }
            dt.Rows.Add(dr3);

            // CR Rejected
            var query4 = from l in db.ClearanceRequestLogs
                         join cr in db.ClearanceRequests on l.ClearanceRequestId equals cr.Id
                         where l.Activity.Contains("Rejected") && l.Date >= startDate && l.Date <= endDate
                         select cr;

            foreach (var cr in query4.ToList())
            {
                var worksheet = db.Worksheets.Where(x => x.ProjectId == cr.ProjectId).OrderByDescending(x => x.WorkDate).FirstOrDefault();

                if (worksheet != null)
                {
                    cr.ProcessedBy = worksheet.DCO;
                }
            }

            DataRow dr4 = dt.NewRow();
            dr4[0] = "CR Rejected";

            foreach (string d in dcos)
            {
                var cnt = query4.Where(x => x.ProcessedBy == d).Count();

                dr4[d] = cnt;
            }
            dt.Rows.Add(dr4);

            // Site Visits
            var query5 = db.Inspections
                .Where(x => x.Status == "SVC email notification" && x.DateSiteVisitCompletion >= startDate && x.DateSiteVisitCompletion <= endDate);

            DataRow dr5 = dt.NewRow();
            dr5[0] = "Site Visits";

            foreach (string d in dcos)
            {
                var cnt = query5.Where(x => x.DCO == d).Count();

                dr5[d] = cnt;
            }
            dt.Rows.Add(dr5);

            // Site Visit Rejected
            var query6 = from il in db.InspectionLogs
                         join i in db.Inspections on il.InspectionID equals i.Id
                         where il.Activity.ToLower().Contains("rejected") && il.Date >= startDate && il.Date <= endDate
                         select i;

            foreach (var i in query6.ToList())
            {
                var worksheet = db.Worksheets.Where(x => x.ProjectId == i.ProjectId).OrderByDescending(x => x.WorkDate).FirstOrDefault();

                if (worksheet != null)
                {
                    i.DCO = worksheet.DCO;
                }
            }

            DataRow dr6 = dt.NewRow();
            dr6[0] = "SV Rejected";

            foreach (string d in dcos)
            {
                var cnt = query6.Where(x => x.DCO == d).Count();

                dr6[d] = cnt;
            }
            dt.Rows.Add(dr6);

            return dt;
        }

        #region CR Activities
        public ActionResult ClearanceRequests(int Year = 0)
        {
            int currentYear = DateTime.Today.Year;
            List<SelectListItem> years = new List<SelectListItem>();
            for (int i = 0; i <= 2; i++)
            {
                string year = (currentYear - i).ToString();
                years.Add(new SelectListItem { Text = year, Value = year });
            }
            ViewBag.Years = years;

            ViewBag.CurrentYear = (Year == 0) ? currentYear : Year;

            return View();
        }

        public PartialViewResult CrProcessed(int year)
        {
            DateTime startDate = DateTime.Parse("01/01/" + year);
            DateTime endDate = DateTime.Parse("12/31/" + year);

            DataTable model = GetCrProcessedByMonthModel(startDate, endDate);
            ViewBag.Year = year;

            return PartialView("_crProcessed", model);
        }

        private DataTable GetCrProcessedByMonthModel(DateTime startDate, DateTime endDate)
        {
            // Get the list of attributes.
            ApplicationRole role = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO");
            string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();

            List<string> dcos = db.UserProfiles.Where(x => memberIDs.Any(y => y == x.UserID))
                .OrderBy(x => x.UserInitial).Select(x => x.UserInitial).ToList();
            // Get the CO's who processed CR regardless of user role or status
            List<string> dcos2 = db.ClearanceRequests
                .Where(x => x.DateModified >= startDate && x.DateModified <= endDate
                        && !String.IsNullOrEmpty(x.ProcessedBy) && x.CurrentStatus.Contains("Sent to Department"))
                .Select(x => x.ProcessedBy).Distinct().ToList();

            dcos = dcos.Union(dcos2).ToList();

            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("CO");
            dt.Columns.Add("January");
            dt.Columns.Add("February");
            dt.Columns.Add("March");
            dt.Columns.Add("April");
            dt.Columns.Add("May");
            dt.Columns.Add("June");
            dt.Columns.Add("July");
            dt.Columns.Add("August");
            dt.Columns.Add("September");
            dt.Columns.Add("October");
            dt.Columns.Add("November");
            dt.Columns.Add("December");
            dt.Columns.Add("Total");

            foreach (string d in dcos)
            {
                DataRow dr = dt.NewRow();
                dr[0] = d;

                int rowTotal = 0;
                for (int i = 0; i < 12; i++)
                {
                    DateTime dt1 = startDate.AddMonths(i);
                    DateTime dt2 = dt1.AddMonths(1).AddDays(-1);

                    var cnt = db.ClearanceRequests
                        .Where(x => x.CurrentStatus == "Sent to Department" && x.ProcessedBy == d && x.DateModified >= dt1 && x.DateModified <= dt2).Count();

                    dr[i + 1] = (cnt == 0) ? "" : cnt.ToString();
                    rowTotal += cnt;
                }
                dr[13] = rowTotal;

                dt.Rows.Add(dr);
            }
            DataRow totalRow = dt.NewRow();
            for (int j = 1; j <= 13; j++)
            {
                int colTotal = 0;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (!String.IsNullOrEmpty(dt.Rows[i][j].ToString()))
                        colTotal += int.Parse(dt.Rows[i][j].ToString());
                }
                totalRow[j] = colTotal;
            }
            dt.Rows.Add(totalRow);

            return dt;
        }

        #endregion

        public ActionResult HoursByUnit1(string unit, string month1, string month2, string phase = "B")
        {
            var worksheets = db.Worksheets.ToList();
            if (!String.IsNullOrEmpty(month1))
            {
                DateTime dt1 = DateTime.Parse(month1.Replace("/", "/01/"));
                worksheets = worksheets.Where(x => x.WorkDate >= dt1).ToList();
            }
            if (!String.IsNullOrEmpty(month2))
            {
                DateTime dt2 = DateTime.Parse(month2.Replace("/", "/01/"));
                dt2 = dt2.AddMonths(1);
                worksheets = worksheets.Where(x => x.WorkDate < dt2).ToList();
            }

            var hours = from w in worksheets
                        group w by new { w.ProjectId } into grp
                        select new { grp.Key.ProjectId, Hours = grp.Sum(x => x.Hours + x.Minutes / 60.0) };
            var model = (from h in hours
                         join p in db.Projects on h.ProjectId equals p.Id
                         select new HoursByUnitModel
                         {
                             ProjectId = p.Id,
                             ProjectName = p.ProjectName,
                             Unit = p.Unit,
                             JOC = p.JOC,
                             Phase = p.Phase,
                             CO = p.DCO,
                             HoursAvailable = p.HoursAvailable,
                             HoursCharged = h.Hours,
                             HoursRemaining = p.HoursRemaining
                         })
                         .OrderBy(x => x.Unit).ThenBy(x => x.JOC).ToList();

            if (!String.IsNullOrEmpty(unit))
                model = model.Where(x => x.Unit == unit).ToList();
            if (!String.IsNullOrEmpty(phase))
                model = model.Where(x => x.Phase == phase).OrderBy(x => x.Unit).ThenBy(x => x.JOC).ToList();

            // for dropdownlist
            var units = (from w in db.Worksheets
                         join p in db.Projects on w.ProjectId equals p.Id
                         select new ListItem { Text = p.Unit, Value = p.Unit })
                        .Distinct().OrderBy(x => x.Text).ToList();

            units.Insert(0, new ListItem { Text = "- All units -", Value = "" });
            ViewBag.Units = units;
            ViewBag.Unit = unit;
            ViewBag.Month1 = month1;
            ViewBag.Month2 = month2;

            var phases = new List<ListItem>();
            phases.Add(new ListItem { Text = "- All phases -", Value = "" });
            phases.Add(new ListItem { Text = "A", Value = "A" });
            phases.Add(new ListItem { Text = "B", Value = "B" });
            phases.Add(new ListItem { Text = "C", Value = "C" });
            phases.Add(new ListItem { Text = "P", Value = "P" });
            ViewBag.Phases = phases;
            ViewBag.Phase = phase;

            ViewBag.Count = model.Count;

            return View(model);
        }

        public ActionResult HoursByUnit2(string unit, string phase = "B")
        {
            // for dropdownlist
            var units = (from p in db.Projects
                         where !String.IsNullOrEmpty(p.Unit)
                         select new ListItem { Text = p.Unit, Value = p.Unit })
                        .Distinct().OrderBy(x => x.Text).ToList();
            units.Insert(0, new ListItem { Text = "- Select unit -", Value = "" });
            ViewBag.Units = units;
            ViewBag.Unit = unit;

            var phases = new List<ListItem>();
            phases.Add(new ListItem { Text = "- All phases -", Value = "" });
            phases.Add(new ListItem { Text = "A", Value = "A" });
            phases.Add(new ListItem { Text = "B", Value = "B" });
            phases.Add(new ListItem { Text = "C", Value = "C" });
            phases.Add(new ListItem { Text = "P", Value = "P" });
            ViewBag.Phases = phases;
            ViewBag.Phase = phase;

            if (String.IsNullOrEmpty(unit))
                return View();

            int[] projectIds = db.Projects.Where(x=> x.Unit == unit).Select(x => x.Id).Distinct().ToArray();

            var hours = from w in db.Worksheets
                        where projectIds.Any(y => y == w.ProjectId)
                        group w by new { w.ProjectId } into grp
                        select new { grp.Key.ProjectId, Hours = grp.Sum(x => x.Hours + x.Minutes / 60.0) };
            var model = (from p in db.Projects
                         join h in hours on p.Id equals h.ProjectId into ph
                         from h in ph.DefaultIfEmpty()
                         where p.Unit == unit
                         select new HoursByUnitModel
                         {
                             ProjectId = p.Id,
                             ProjectName = p.ProjectName,
                             Unit = p.Unit,
                             JOC = p.JOC,
                             Phase = p.Phase,
                             CO = p.DCO,
                             HoursAvailable = p.HoursAvailable,
                             HoursCharged = h.Hours,
                             HoursRemaining = p.HoursRemaining
                         }).OrderBy(x => x.JOC).ToList();

            if (!String.IsNullOrEmpty(phase))
                model = model.Where(x => x.Phase == phase).OrderBy(x => x.Unit).ThenBy(x => x.JOC).ToList();

            ViewBag.Count = model.Count;

            return View(model);
        }

        #region Past Due Document

        public ActionResult ExcludePastDueProject(string dept)
        {
            int projectId = int.Parse(Request.Form["projectId"]);
            string comment = Request.Form["Comment"];

            var nonCompliances = db.NonCompliances.Where(x => x.ProjectID == projectId).ToList();
            foreach (var nc in nonCompliances)
            {
                ExcludedNonCompliance exculded = new ExcludedNonCompliance
                {
                    ProjectID = nc.ProjectID,
                    ContractorID = nc.ContractorID,
                    DocumentType = nc.DocumentType,
                    Year = nc.Year,
                    Month = nc.Month,
                    DateReceived = nc.DateReceived,
                    DateRegistered = nc.DateRegistered,
                    DateRequired = nc.DateRequired,
                    Comment = comment
                };

                db.NonCompliances.Remove(nc);
                db.ExcludedNonCompliances.Add(exculded);
                db.SaveChanges();
            }

            return RedirectToAction("PastDue1", new { dept = dept, refresh = true });
        }

        public ActionResult IncludePastDueDocument(string dept)
        {
            int id = int.Parse(Request.Form["id"]);

            var excluded = db.ExcludedNonCompliances.FirstOrDefault(x => x.ID == id);
            if (excluded != null)
            {
                NonCompliance nc = new NonCompliance
                {
                    ProjectID = excluded.ProjectID,
                    ContractorID = excluded.ContractorID,
                    DocumentType = excluded.DocumentType,
                    DateReceived = excluded.DateReceived,
                    DateRegistered = excluded.DateRegistered,
                    DateRequired = excluded.DateRequired
                };

                db.ExcludedNonCompliances.Remove(excluded);
                db.NonCompliances.Add(nc);
                db.SaveChanges();
            }

            return RedirectToAction("PastDue3", new { dept = dept, refresh = true });
        }

        public ActionResult GetPastDueDocumentsByProjectReport(string dept)
        {
            using (ReportDocument rpt = new ReportDocument())
            {
                try
                {
                    string rptPath = Server.MapPath("~/Reports/");

                    rpt.Load(rptPath + "PastDueDocumentsByProject.rpt");

                    SetParameterValues(rpt, dept);

                    if (rpt.HasRecords)
                    {
                        string fileName = rptPath + "Temp/PastDueDocuments.pdf";

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

        private void SetParameterValues(ReportDocument rpt, string dept)
        {
            string[] args = ConfigurationManager.ConnectionStrings["ContractContext"].ConnectionString.Split(';');

            string uid = "", pwd = "", svr = "", database = "CCCS";
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
            }

            rpt.SetDatabaseLogon(uid, pwd, svr, database);

            ParameterFieldDefinitions crParameterFieldDefinitions = rpt.DataDefinition.ParameterFields;
            ParameterDiscreteValue crParameterDiscreteValue = new ParameterDiscreteValue();
            ParameterValues crParameterValues = new ParameterValues();

            ParameterFieldDefinition exc = crParameterFieldDefinitions["@dept"];
            crParameterDiscreteValue.Value = dept;
            crParameterValues.Add(crParameterDiscreteValue);
            exc.ApplyCurrentValues(crParameterValues);
        }

        #endregion

        public string GetCrActivitiesDetail(string dco, int month, int year)
        {
            DateTime dt1 = new DateTime(year, month, 1);
            DateTime dt2 = dt1.AddMonths(1).AddDays(-1);

            var cRequests = from cr in db.ClearanceRequests
                            join p in db.Projects on cr.ProjectId equals p.Id
                            join c in db.Contractors on p.PrimeContractorID equals c.Id
                            where cr.CurrentStatus == "Sent to Department" && cr.ProcessedBy == dco && cr.DateModified >= dt1 && cr.DateModified <= dt2
                            select new { ProjectID = p.JOC, ProjectName = p.ProjectName, Date = cr.DateModified, DCO = c.DCO, AltDCO = c.AlternateDCO };

            string html = "<h3>" + dco + "<small class='mar-left-20'>" + CommonHelper.GetMonthName(month) + "</small></h3>";
            html += "<table class='table table-condensed'><thead><tr>";
            html += "<th></th><th class='text-left'>Project ID</th><th class='text-left'>Project Name</th><th class='text-left'>Date Processed</th><th>CO</th><th>Alternate DCO</th>";
            html += "</tr></thead><tbody>";

            int cnt = 1;
            foreach (var cr in cRequests)
            {
                html += "<tr><td style='width: 40px;'>" + cnt + "</td>";
                html += "<td class='col-md-3'>" + cr.ProjectID + "</td>";
                html += "<td>" + cr.ProjectName + "</td>";
                html += "<td class='col-md-2'>" + cr.Date + "</td>";
                html += "<td class='text-center' style='width: 70px;'>" + cr.DCO + "</td>";
                html += "<td class='text-center' style='width: 70px;'>" + cr.AltDCO + "</td>";
                html += "</tr>";

                cnt++;
            }

            html += "</tbody>";
            html += "</table>";

            return html;
        }

        public ActionResult ProjectsWithLateNtp(string sortOrder, string department, string co,
            string listStyle, int? page, DateTime? dateFrom, DateTime? dateTo)
        {
            if (Request["submit"] == "reset")
            {
                sortOrder = "";
                department = "";
                co = "";
                listStyle = "";
                page = 1;
                dateFrom = null;
                dateTo = null;
            }

            List<ProjectWithLateNtpModel> model;

            ProjectWithLateNtpModel pm = new ProjectWithLateNtpModel();
            string key = PROJECT_WITH_LATE_NTP_KEY;
            if (!CacheHelper.Get<List<ProjectWithLateNtpModel>>(key, out model))
            {
                model = pm.GetProjectsWithLateNtp();
                CacheHelper.Add<List<ProjectWithLateNtpModel>>(model, key, 60.0);
            }

            if (!String.IsNullOrEmpty(department))
                model = model.Where(x => x.DepartmentId == department).ToList();

            if (!String.IsNullOrEmpty(co))
                model = model.Where(x => x.DCO == co).ToList();

            if (dateFrom != null)
                model = model.Where(x => x.DateReceived >= (DateTime)dateFrom).ToList();

            if (dateTo != null)
                model = model.Where(x => x.DateReceived <= (DateTime)dateTo).ToList();

            model = SortProjectWithLateNtpModel(model, sortOrder, page);

            ViewBag.Departments = GetDepartments();
            ViewBag.Dept = department;
            ViewBag.COs = GetCOs();
            ViewBag.CO = co;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            ViewBag.Total = model.Count;

            ViewBag.ListStyle = listStyle;

            int pageNumber = (page ?? 1);
            ViewBag.PageNumber = pageNumber;

            var pagedModel = model.ToPagedList(pageNumber, PAGE_SIZE);
            //pagedModel = CommonHelper.GetExtraInfo(pagedModel);
            foreach (var m in pagedModel)
            {
                m.DateLastSiteVisit = GetLastSiteVisitByProject(m.ProjectID);
            }

            return View(pagedModel);
        }

        public List<ProjectWithLateNtpModel> SortProjectWithLateNtpModel(List<ProjectWithLateNtpModel> model, string sortOrder, int? page)
        {
            if (page == null)
            {
                ViewBag.ProjectIdSortParm = (string.IsNullOrEmpty(sortOrder) || sortOrder == "ProjectID") ? "ProjectID_desc" : "ProjectID";
                ViewBag.ProjectNameSortParm = (sortOrder == "ProjectName") ? "ProjectName_desc" : "ProjectName";
                ViewBag.PrimeContractorSortParm = (sortOrder == "PrimeContractor") ? "PrimeContractor_desc" : "PrimeContractor";
                ViewBag.DeptSortParm = (sortOrder == "Dept") ? "Dept_desc" : "Dept";
                ViewBag.StartSortParm = (sortOrder == "Start") ? "Start_desc" : "Start";
                ViewBag.EndSortParm = (sortOrder == "End") ? "End_desc" : "End";
            }

            switch (sortOrder)
            {
                case "ProjectID":
                    model = model.OrderBy(p => p.JOC).ToList();
                    break;
                case "ProjectID_desc":
                    model = model.OrderByDescending(p => p.JOC).ToList();
                    break;
                case "ProjectName":
                    model = model.OrderBy(p => p.ProjectName).ToList();
                    break;
                case "ProjectName_desc":
                    model = model.OrderByDescending(p => p.ProjectName).ToList();
                    break;
                case "PrimeContractor":
                    model = model.OrderBy(p => p.PrimeContractorName).ToList();
                    break;
                case "PrimeContractor_desc":
                    model = model.OrderByDescending(p => p.PrimeContractorName).ToList();
                    break;
                case "Dept":
                    model = model.OrderBy(p => p.DepartmentId).ToList();
                    break;
                case "Dept_desc":
                    model = model.OrderByDescending(p => p.DepartmentId).ToList();
                    break;
                case "Start":
                    model = model.OrderBy(p => p.StartDate).ToList();
                    break;
                case "Start_desc":
                    model = model.OrderByDescending(p => p.StartDate).ToList();
                    break;
                case "End":
                    model = model.OrderBy(p => p.EndDate).ToList();
                    break;
                case "End_desc":
                    model = model.OrderByDescending(p => p.EndDate).ToList();
                    break;
                default:
                    model = model.OrderBy(p => p.NumberDaysReceivedStart).ToList();
                    break;
            }

            ViewBag.SortOrder = (string.IsNullOrEmpty(sortOrder)) ? "ProjectID" : sortOrder;

            return model;
        }


        public string PrintProjectsWithLateNtp(string dept, string co, DateTime? dateFrom, DateTime? dateTo)
        {
            string url = "";

            using (ReportDocument rpt = new ReportDocument())
            {
                try
                {
                    string rptPath = Server.MapPath("~/Reports/");
                    rpt.Load(rptPath + "ProjectsWithLateNTP.rpt");

                    SetLoginInfo(rpt);
                    SetParameterValues(rpt, dept, co, dateFrom, dateTo);

                    if (rpt.HasRecords)
                    {
                        string fileName = rptPath + "Temp/ProjectsWithLateNTP.pdf";

                        rpt.ExportToDisk(ExportFormatType.PortableDocFormat, fileName);

                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Exists)
                        {
                            return Url.Content("~/Reports/Temp/ProjectsWithLateNTP.pdf");
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


        private void SetParameterValues(ReportDocument rpt, string dept, string co, DateTime? dateFrom, DateTime? dateTo)
        {
            ParameterFieldDefinitions crParameterFieldDefinitions = rpt.DataDefinition.ParameterFields;
            ParameterDiscreteValue crParameterDiscreteValue = new ParameterDiscreteValue();
            ParameterValues crParameterValues = new ParameterValues();

            ParameterFieldDefinition p1 = crParameterFieldDefinitions["@dept"];
            crParameterDiscreteValue.Value = dept;
            crParameterValues.Add(crParameterDiscreteValue);
            p1.ApplyCurrentValues(crParameterValues);

            ParameterFieldDefinition p2 = crParameterFieldDefinitions["@co"];
            crParameterDiscreteValue.Value = co;
            crParameterValues.Add(crParameterDiscreteValue);
            p2.ApplyCurrentValues(crParameterValues);

            ParameterFieldDefinition p3 = crParameterFieldDefinitions["@dateFrom"];
            crParameterDiscreteValue.Value = (dateFrom == null) ? DateTime.Parse("1/1/2015") : dateFrom;
            crParameterValues.Add(crParameterDiscreteValue);
            p3.ApplyCurrentValues(crParameterValues);

            ParameterFieldDefinition p4 = crParameterFieldDefinitions["@dateTo"];
            crParameterDiscreteValue.Value = (dateTo == null) ? DateTime.Parse("12/31/9999") : dateTo;
            crParameterValues.Add(crParameterDiscreteValue);
            p4.ApplyCurrentValues(crParameterValues);
        }

    }
}
