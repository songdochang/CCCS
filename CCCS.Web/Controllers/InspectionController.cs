using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CCCS.Web.Models;
using System.IO;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using System.Xml;
using iTextSharp.text.pdf;
using CCCS.Infrastructure;
using CCCS.Core.Domain.Inspection;
using CCCS.Core.Domain.Projects;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Documents;
using CCCS.Web.Models.Inspection;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Configuration;
using CCCS.Core.Domain.Notices;

namespace CCCS.Controllers
{
    [Authorize]
    public class InspectionController : BaseController
    {
        public ActionResult Index1()
        {
            List<SiteVisitListModel> model = GetSiteVisitListModel();

            if (User.IsInRole("DCO"))
            {
                ViewBag.CO = UserProfile.UserInitial;
            }

            return View(model);
        }

        public ActionResult Index2(string dco, string from, string to)
        {
            if (User.IsInRole("DCO"))
            {
                return RedirectToAction("Index2", new { id = UserProfile.UserInitial });
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

            List<SiteVisitException> list = (String.IsNullOrEmpty(dco))
                ? db.SiteVisitExceptions.ToList()
                : db.SiteVisitExceptions.Where(x => x.DCO == dco).ToList();
            List<SiteVisitException> model = new List<SiteVisitException>();
            foreach (var item in list)
            {
                DateTime dt;
                if (DateTime.TryParse(item.Week, out dt) && dt >= dt1 && dt <= dt2)
                {
                    model.Add(item);
                }
            }

            ViewBag.COs = GetCOs();
            ViewBag.Total = model.Count;

            if (!String.IsNullOrEmpty(from) || !String.IsNullOrEmpty(to))
            {
                ViewBag.DateRange = "Dates: " + from + " ~ " + to;
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Index2(FormCollection form)
        {
            string dco = form["DCO"];
            var from = Request.Form["dateFrom"];
            var to = Request.Form["dateTo"];

            return RedirectToAction("Index2", new { dco = dco, from = from, to = to });
        }

        public ActionResult Index3(string dco)
        {
            if (User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
            }

            var list = from i in db.Inspections
                       join c in db.Contractors on i.ContractorId equals c.Id
                       join p in db.Projects on i.ProjectId equals p.Id
                       where !new[] { "VC sent to Department", "SVC email notification", "SVC sent to Department" }.Contains(i.Status)
                            && i.DateCancelled == null
                       orderby i.DateRequested descending
                       select new InspectionListModel
                       {
                           InspectionID = i.Id,
                           ProjectID = i.ProjectId,
                           JOC = p.JOC,
                           ProjectName = p.ProjectName,
                           ContractorID = i.ContractorId,
                           CompanyName = c.CompanyName,
                           DateRequested = i.DateRequested,
                           DateApproved = i.DateApproved,
                           DateContractorNotification = i.DateContractorNotification,
                           DateOfVisit = i.DateOfVisit,
                           DateSiteVisitCompletion = i.DateSiteVisitCompletion,
                           DateViolationCorrection = i.DateViolationCorrection,
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
                           RoundTripHours = i.RoundTripHours,
                           Status = i.Status
                       };

            List<InspectionListModel> model = (String.IsNullOrEmpty(dco)) ? list.ToList() : list.Where(x => x.DCO == dco).ToList();

            foreach (var item in model.OrderByDescending(x => x.DateLastUpdated).ToList())
            {
                var ct = db.Contracts.FirstOrDefault(x => x.ProjectId == item.ProjectID && x.ContractorId == item.ContractorID);

                if (ct != null)
                {
                    item.PS = (ct.SubTo == 0) ? "P" : "S";
                }
            }

            ViewBag.CO = dco;

            return View(model);
        }

        [HttpPost]
        public ActionResult Index3(FormCollection form)
        {
            string dco = form["DCO"];

            return RedirectToAction("Index3", new { dco = dco });
        }

        private List<SiteVisitListModel> GetSiteVisitListModel()
        {
            var query = from i in db.Inspections
                        join c in db.Contractors on i.ContractorId equals c.Id
                        where i.DateOfVisit != null
                        select new SiteVisitTemp
                        {
                            ProjectID = i.ProjectId,
                            ContractorID = i.ContractorId,
                            CompanyName = c.CompanyName,
                            DateOfVisit = i.DateOfVisit,
                            DCO = i.DCO,
                            NumberInterviews = i.NumberInterviews
                        };

            List<SiteVisitTemp> list = query.ToList();

            foreach (var l in list)
            {
                var ct = db.Contracts.FirstOrDefault(x => x.ProjectId == l.ProjectID && x.ContractorId == l.ContractorID);

                if (ct != null)
                {
                    l.PS = (ct.SubTo == 0) ? "P" : "S";
                }
                else
                {
                    l.PS = "P";
                }
            };
            list = list.OrderBy(x => x.PS).ThenBy(x => x.CompanyName).ThenBy(x => x.DateOfVisit).ToList();

            List<SiteVisitListModel> model = new List<SiteVisitListModel>();
            string ps = "";
            string company = "";
            string date = "";
            List<SelectListItem> dcos = GetUsers(string.Empty);

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                string pre = item.PS + "_" + item.ContractorID;
                string dt1 = (item.DateOfVisit == null) ? "" : ((DateTime)item.DateOfVisit).ToShortDateString();
                string dt2 = dt1.Replace("/", "");
                string dco = item.DCO;

                if (item.PS != ps)
                {
                    ps = item.PS;

                    var t = GetSiteVisitModel(list, dcos, ps, "", "", pre, "");
                    model.Add(t);
                }

                if (item.CompanyName != company)
                {
                    company = item.CompanyName;
                    date = (item.DateOfVisit == null) ? "" : ((DateTime)item.DateOfVisit).ToShortDateString();

                    var c = GetSiteVisitModel(list, dcos, ps, company, "", pre, company);
                    model.Add(c);

                    var d = GetSiteVisitModel(list, dcos, ps, company, dt2, pre, date);
                    model.Add(d);

                    continue;
                }
                else
                {
                    if (((DateTime)item.DateOfVisit).ToShortDateString() != date)
                    {
                        date = ((DateTime)item.DateOfVisit).ToShortDateString();

                        var d = GetSiteVisitModel(list, dcos, ps, company, dt2, pre, date);
                        model.Add(d);
                        continue;
                    }
                }
            }

            var g = GetSiteVisitModel(list, dcos, "", "", "", "", "Grand Total");
            model.Add(g);

            return model;
        }

        private SiteVisitListModel GetSiteVisitModel(List<SiteVisitTemp> list, List<SelectListItem> dcos, string ps, string company, string date, string pre, string text)
        {
            List<KeyValuePair<string, int>> v = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> i = new List<KeyValuePair<string, int>>();
            string id = "";
            int level = 9;

            if (string.IsNullOrEmpty(ps))
            {
                foreach (var d in dcos)
                {
                    int cnt = list.Where(x => x.DCO == d.Value).Count();
                    KeyValuePair<string, int> vPair = new KeyValuePair<string, int>(d.Value, cnt);
                    v.Add(vPair);

                    cnt = list.Where(x => x.DCO == d.Value).Sum(x => x.NumberInterviews);
                    KeyValuePair<string, int> iPair = new KeyValuePair<string, int>(d.Value, cnt);
                    i.Add(iPair);
                };
            }
            else if (string.IsNullOrEmpty(company))
            {
                id = ps;
                level = 0;
                text = (ps == "P") ? "Prime" : "Subcontractor";

                foreach (var d in dcos)
                {
                    int cnt = list.Where(x => x.DCO == d.Value && x.PS == ps).Count();
                    KeyValuePair<string, int> vPair = new KeyValuePair<string, int>(d.Value, cnt);
                    v.Add(vPair);

                    cnt = list.Where(x => x.DCO == d.Value && x.PS == ps).Sum(x => x.NumberInterviews);
                    KeyValuePair<string, int> iPair = new KeyValuePair<string, int>(d.Value, cnt);
                    i.Add(iPair);
                };
            }
            else if (string.IsNullOrEmpty(date))
            {
                id = pre;
                level = 1;

                foreach (var d in dcos)
                {
                    int cnt = list.Where(x => x.DCO == d.Value && x.PS == ps && x.CompanyName == company).Count();
                    KeyValuePair<string, int> vPair = new KeyValuePair<string, int>(d.Value, cnt);
                    v.Add(vPair);

                    cnt = list.Where(x => x.DCO == d.Value && x.PS == ps && x.CompanyName == company).Sum(x => x.NumberInterviews);
                    KeyValuePair<string, int> iPair = new KeyValuePair<string, int>(d.Value, cnt);
                    i.Add(iPair);
                };
            }
            else
            {
                id = pre + "_" + date;
                level = 2;

                foreach (var d in dcos)
                {
                    int cnt = list.Where(x => x.DCO == d.Value && x.PS == ps && x.CompanyName == company && x.DateOfVisit != null && ((DateTime)x.DateOfVisit).ToShortDateString() == text).Count();
                    KeyValuePair<string, int> vPair = new KeyValuePair<string, int>(d.Value, cnt);
                    v.Add(vPair);

                    cnt = list.Where(x => x.DCO == d.Value && x.PS == ps && x.CompanyName == company && x.DateOfVisit != null && ((DateTime)x.DateOfVisit).ToShortDateString() == text).Sum(x => x.NumberInterviews);
                    KeyValuePair<string, int> iPair = new KeyValuePair<string, int>(d.Value, cnt);
                    i.Add(iPair);
                };
            }

            return new SiteVisitListModel { Id = id, Level = level, Text = text, Visits = v, Interviews = i, TotalVisits = v.Sum(x => x.Value), TotalInterviews = i.Sum(x => x.Value) };
        }

        public ActionResult Index4(string dco, DateTime? fromDate, DateTime? toDate)
        {
            if (User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
            }

            if (fromDate == null)
                fromDate = DateTime.Today.AddMonths(-3);
            if (toDate == null)
                toDate = DateTime.Today;

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            var list = from i in db.Inspections
                       join c in db.Contractors on i.ContractorId equals c.Id
                       join p in db.Projects on i.ProjectId equals p.Id
                       where new[] { "VC sent to Department", "SVC email notification", "SVC sent to Department" }.Contains(i.Status)
                            && (i.DateSiteVisitCompletion >= fromDate && i.DateSiteVisitCompletion <= toDate)
                       select new InspectionListModel
                       {
                           InspectionID = i.Id,
                           ProjectID = i.ProjectId,
                           JOC = p.JOC,
                           ContractorID = i.ContractorId,
                           CompanyName = c.CompanyName,
                           DateOfVisit = i.DateOfVisit,
                           DateSiteVisitCompletion = i.DateSiteVisitCompletion,
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

            List<InspectionListModel> model = (String.IsNullOrEmpty(dco)) ? list.ToList() : list.Where(x => x.DCO == dco).ToList();

            foreach (var m in model)
            {
                var ct = db.Contracts.FirstOrDefault(x => x.ProjectId == m.ProjectID && x.ContractorId == m.ContractorID);

                if (ct != null)
                {
                    m.PS = (ct.SubTo == 0) ? "P" : "S";
                }
            }

            ViewBag.CO = dco;

            return View(model);
        }

        [HttpPost]
        public ActionResult Index4(FormCollection form)
        {
            string dco = form["DCO"];

            DateTime fromDate, toDate;
            DateTime.TryParse(form["fromDate"], out fromDate);
            DateTime.TryParse(form["toDate"], out toDate);

            if (String.IsNullOrEmpty(form["button"]) || form["button"].Contains("Refresh"))
                return RedirectToAction("Index4", new { dco = dco, fromDate = fromDate, toDate = toDate });
            else
            {
                using (ReportDocument rpt = new ReportDocument())
                {
                    try
                    {
                        string rptPath = Server.MapPath("~/Reports/");

                        rpt.Load(rptPath + "CompletedSV.rpt");

                        SetParameterValues(rpt, fromDate, toDate, dco);

                        if (rpt.HasRecords)
                        {
                            string fileName = rptPath + "Temp/CompletedSV.pdf";
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

                    return Redirect(Request.UrlReferrer.ToString());
                }
            }
        }

        private void SetParameterValues(ReportDocument rpt, DateTime fromDate, DateTime toDate, string dco)
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

            ParameterFieldDefinition fd1 = crParameterFieldDefinitions["@fromDate"];
            crParameterDiscreteValue.Value = fromDate;
            crParameterValues.Add(crParameterDiscreteValue);
            fd1.ApplyCurrentValues(crParameterValues);

            ParameterFieldDefinition fd2 = crParameterFieldDefinitions["@toDate"];
            crParameterDiscreteValue.Value = toDate;
            crParameterValues.Add(crParameterDiscreteValue);
            fd2.ApplyCurrentValues(crParameterValues);

            if (!String.IsNullOrEmpty(dco))
                rpt.RecordSelectionFormula = "{rpt_CompletedSV;1.DCO}='" + dco + "'";
        }

        public ActionResult Index5(string dco)
        {
            if (User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
            }

            var list = from i in db.Inspections
                       join c in db.Contractors on i.ContractorId equals c.Id
                       join p in db.Projects on i.ProjectId equals p.Id
                       join g in db.InspectionLogs on i.Id equals g.InspectionID into ig
                       from x in ig.Where(x => x.Activity.Contains("SV rejected by Manager")).DefaultIfEmpty()
                       where i.Status.Contains("Rejected")
                       select new InspectionListModel
                       {
                           InspectionID = i.Id,
                           ProjectID = i.ProjectId,
                           JOC = p.JOC,
                           ContractorID = i.ContractorId,
                           CompanyName = c.CompanyName,
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
                           RoundTripHours = i.RoundTripHours,
                           Comment = x.Comment
                       };

            List<InspectionListModel> model = (String.IsNullOrEmpty(dco)) ? list.ToList() : list.Where(x => x.DCO == dco).ToList();

            foreach (var m in model)
            {
                var ct = db.Contracts.FirstOrDefault(x => x.ProjectId == m.ProjectID && x.ContractorId == m.ContractorID);

                if (ct != null)
                {
                    m.PS = (ct.SubTo == 0) ? "P" : "S";
                }
            }

            ViewBag.CO = dco;

            return View(model);
        }

        public ActionResult Index6(string dco)
        {
            if (User.IsInRole("DCO"))
            {
                dco = UserProfile.UserInitial;
            }

            var list = from i in db.Inspections
                       join l in db.InspectionLogs on i.Id equals l.InspectionID into il
                       from x in il.DefaultIfEmpty()
                       join c in db.Contractors on i.ContractorId equals c.Id
                       join p in db.Projects on i.ProjectId equals p.Id
                       where i.Status.Contains("cancelled") && i.DateCancelled != null && x.Activity.Contains("cancelled")
                       orderby i.DateCancelled descending
                       select new InspectionListModel
                       {
                           InspectionID = i.Id,
                           ProjectID = i.ProjectId,
                           JOC = p.JOC,
                           ProjectName = p.ProjectName,
                           ContractorID = i.ContractorId,
                           CompanyName = c.CompanyName,
                           DateRequested = i.DateRequested,
                           DateOfVisit = i.DateOfVisit,
                           DateCancelled = i.DateCancelled,
                           DCO = i.DCO,
                           Address = i.Address,
                           City = i.City,
                           Comment = x.Comment
                       };

            List<InspectionListModel> model = (String.IsNullOrEmpty(dco)) ? list.ToList() : list.Where(x => x.DCO == dco).ToList();

            foreach (var item in model.OrderByDescending(x => x.DateLastUpdated).ToList())
            {
                var ct = db.Contracts.FirstOrDefault(x => x.ProjectId == item.ProjectID && x.ContractorId == item.ContractorID);

                if (ct != null)
                {
                    item.PS = (ct.SubTo == 0) ? "P" : "S";
                }
            }

            ViewBag.CO = dco;

            return View(model);
        }

        [HttpPost]
        public ActionResult Index6(FormCollection form)
        {
            string dco = form["DCO"];

            return RedirectToAction("Index6", new { dco = dco });
        }

        private void GetProjectsByDCO(string dco)
        {
            List<SelectListItem> list = db.Projects.Where(x => x.DCO == dco && x.DateClosed == null)
                    .OrderBy(x => x.JOC)
                    .Select(x => new SelectListItem { Text = x.JOC + " (" + x.ProjectName + ")", Value = x.Id.ToString() }).ToList();

            list.Insert(0, new SelectListItem { Text = "- Select Project -", Value = "" });

            ViewBag.Projects = list;
        }

        public decimal GetMiles(string start, string end)
        {
            decimal miles = 0.0m;

            if (string.IsNullOrEmpty(start))
            {
                start = db.Settings.FirstOrDefault(x => x.Key == "SiteVisit.StartPoint").Value;
            }

            string NAMESPACE = "http://schemas.microsoft.com/search/local/ws/rest/v1";
            string url = "http://dev.virtualearth.net/REST/V1/Routes/Driving";
            url += "?output=xml&key=AmUbXHpEIMjtES3JDqCUulruvrXteDxiMUmtLn8UIDWBJzWt3I_zBBQg_k_smn5H";
            url += "&distanceUnit=mi";
            url += "&wp.0=" + HttpUtility.UrlEncode(start);
            url += "&wp.1=" + HttpUtility.UrlEncode(end);

            try
            {
                WebRequest request = HttpWebRequest.Create(url);
                WebResponse response = request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());

                XmlNodeList routes = doc.GetElementsByTagName("TravelDistance", NAMESPACE);
                miles = Math.Round(decimal.Parse(routes[0].InnerText), 1);
            }
            catch (Exception ex)
            {
            }

            return miles;
        }

        public string GetRouteRequest(string dco, DateTime dt)
        {
            DateTime dt1 = dt.AddDays(1);
            string office = db.Settings.FirstOrDefault(x => x.Key == "SiteVisit.StartPoint").Value;

            string url = "http://dev.virtualearth.net/REST/v1/Routes";
            url += "?key=AmUbXHpEIMjtES3JDqCUulruvrXteDxiMUmtLn8UIDWBJzWt3I_zBBQg_k_smn5H";
            url += "&routePathOutput=Points&output=json&jsonp=RouteCallback";
            url += "&wp.0=" + HttpUtility.UrlEncode(office);

            var wps = db.Inspections.Where(x => x.DCO == dco && x.DateOfVisit >= dt && x.DateOfVisit < dt1).ToList();
            for (int i = 0; i < wps.Count; i++)
            {
                string wp = wps[i].Address + " " + wps[i].City + " " + wps[i].State;
                url += "&wp." + (i + 1).ToString() + "=" + HttpUtility.UrlEncode(wp);
            }
            url += "&wp." + (wps.Count + 1).ToString() + "=" + HttpUtility.UrlEncode(office);

            return url;
        }

        public string GetMapImage(string dco, DateTime dt)
        {
            DateTime dt1 = dt.AddDays(1);
            string office = db.Settings.FirstOrDefault(x => x.Key == "SiteVisit.StartPoint").Value;

            string url = "http://dev.virtualearth.net/REST/v1/Imagery/Map/Road/Routes";
            url += "?key=AmUbXHpEIMjtES3JDqCUulruvrXteDxiMUmtLn8UIDWBJzWt3I_zBBQg_k_smn5H";
            url += "&mapSize=540,450";
            url += "&wp.0=" + HttpUtility.UrlEncode(office);

            var wps = db.Inspections.Where(x => x.DCO == dco).ToList();
            wps = wps.Where(x => x.DateOfVisit >= dt && x.DateOfVisit < dt1).ToList();

            for (int i = 0; i < wps.Count; i++)
            {
                string wp = wps[i].Address + " " + wps[i].City + " " + wps[i].State;
                url += "&wp." + (i + 1).ToString() + "=" + HttpUtility.UrlEncode(wp);
            }
            url += "&wp." + (wps.Count + 1).ToString() + "=" + HttpUtility.UrlEncode(office);

            try
            {
                WebRequest request = HttpWebRequest.Create(url);
                WebResponse response = request.GetResponse();

                return response.ResponseUri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string GetLocationMapImage(string wp)
        {
            string office = db.Settings.FirstOrDefault(x => x.Key == "SiteVisit.StartPoint").Value;

            string url = "http://dev.virtualearth.net/REST/v1/Imagery/Map/Road/Routes";
            url += "?key=AmUbXHpEIMjtES3JDqCUulruvrXteDxiMUmtLn8UIDWBJzWt3I_zBBQg_k_smn5H";
            url += "&mapSize=540,450";
            url += "&wp.0=" + HttpUtility.UrlEncode(office);
            url += "&wp.1=" + HttpUtility.UrlEncode(wp);

            try
            {
                WebRequest request = HttpWebRequest.Create(url);
                WebResponse response = request.GetResponse();

                return response.ResponseUri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string GetRouteMapImage(string wps)
        {
            string url = "http://dev.virtualearth.net/REST/v1/Imagery/Map/Road/Routes";
            url += "?key=AmUbXHpEIMjtES3JDqCUulruvrXteDxiMUmtLn8UIDWBJzWt3I_zBBQg_k_smn5H";
            url += "&mapSize=540,450";
            url += "&" + wps;

            try
            {
                WebRequest request = HttpWebRequest.Create(url);
                WebResponse response = request.GetResponse();

                return response.ResponseUri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }


        // GET: Inspection/Create
        public ActionResult Create(int? id, string dco, string returnUrl = "")
        {
            Inspection model = new Inspection
            {
                State = "CA"
            };

            if (id != null)
            {
                Project project = db.Projects.FirstOrDefault(x => x.Id == id);
                if (project == null)
                {
                    return HttpNotFound();
                }

                ViewBag.Project = project;

                model = new Inspection
                {
                    ProjectId = project.Id,
                    State = "CA"
                };

                var contractor = db.Contractors.FirstOrDefault(x => x.Id == project.PrimeContractorID);
                if (contractor != null)
                {
                    ViewBag.PrimeContractor = contractor.CompanyName;
                    model.ContractorId = contractor.Id;
                }
                else
                {
                    ViewBag.PrimeContractor = "N/A";
                }
            }

            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        // POST: Inspection/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Inspection model, string returnUrl = "")
        {
            var sv = db.Inspections.FirstOrDefault(x => x.DateOfVisit == model.DateOfVisit && x.ProjectId == model.ProjectId
                && x.Address == model.Address && x.DateCancelled == null);
            if (sv != null)
            {
                ModelState.AddModelError(string.Empty, "You may visit the same site no more than once a day.");
                GetContractors(model.ProjectId);

                return View(model);
            }

            if (model.ProjectId > 0 && model.ContractorId > 0)
            {
                //DateTime dtVisit;
                //if (!DateTime.TryParse(model.DateOfVisit.ToString(), out dtVisit) || dtVisit < DateTime.Today.AddMonths(-3) || dtVisit > DateTime.Today.AddMonths(3))
                //{
                //    ModelState.AddModelError(string.Empty, "Invalid 'Date of Visit'.  Date must be within 3 months from today.");
                //}

                if (ModelState.IsValid)
                {
                    DateTime now = DateTime.Now;
                    bool noApproval = (Request.Form["NoApproval"].Contains("true"));
                    if (noApproval)
                    {
                        model.DateApproved = now;
                        model.Status = "Site Visit created";
                    }
                    else
                    {
                        model.Status = "Site Visit Request sent to Manager";
                    }

                    model.DateRequested = now;
                    db.Inspections.Add(model);
                    db.SaveChanges();

                    InspectionLog log = new InspectionLog
                    {
                        InspectionID = model.Id,
                        Date = DateTime.Now,
                        ProcessedBy = UserProfile.UserInitial
                    };

                    if (noApproval)
                    {
                        log.Activity = "Site Visit created";
                        db.InspectionLogs.Add(log);
                        db.SaveChanges();

                        return RedirectToAction("Details", "Inspection", new { id = model.Id });
                    }
                    else
                    {
                        log.Activity = "Site Visit Request sent to Manager";
                        db.InspectionLogs.Add(log);
                        db.SaveChanges();

                        if (string.IsNullOrEmpty(returnUrl))
                            return RedirectToAction("Details4", "Project", new { id = model.ProjectId });
                        else
                            return Redirect(returnUrl);
                    }
                }
                else
                {
                    var project = db.Projects.Find(model.ProjectId);
                    ViewBag.Project = project;
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Project and contractor are required.");
            }

            GetContractors(model.ProjectId);
            ViewBag.ReturnUrl = returnUrl;
            var contractor = db.Contractors.Find(model.ContractorId);
            if (contractor != null)
                ViewBag.PrimeContractor = contractor.CompanyName;

            return View(model);
        }

        // GET: Inspection/Details/5
        public ActionResult Details(int id, string returnUrl = "")
        {
            Inspection inspection = db.Inspections.Find(id);
            if (inspection == null)
            {
                return HttpNotFound();
            }

            //This is necessary because an activity after SVC email notification alters inspection status
            var log = db.InspectionLogs.FirstOrDefault(x => x.InspectionID == id && x.Activity.Contains("SVC email notification"));
            if (log != null)
            {
                inspection.Status = log.Activity;
            }

            int projectId = inspection.ProjectId;
            var project = db.Projects.FirstOrDefault(x => x.Id == projectId);
            ViewBag.ProjectName = project.ProjectName;
            ViewBag.JOC = project.JOC;
            ViewBag.Contractorname = db.Contractors.FirstOrDefault(x => x.Id == inspection.ContractorId).CompanyName;

            ViewBag.ReturnUrl = returnUrl;
            GetContractors(projectId);
            ViewBag.PrimeContractorId = project.PrimeContractorID;

            //make sure session variable has no data
            Session["interviews"] = null;

            return View(inspection);
        }

        public PartialViewResult DetailMap(int id)
        {
            List<InspectionMapModel> model = new List<InspectionMapModel>();
            string startAddress = db.Settings.FirstOrDefault(x => x.Key == "SiteVisit.StartPoint.Address").Value;
            string startCity = db.Settings.FirstOrDefault(x => x.Key == "SiteVisit.StartPoint.City").Value;
            string start = startAddress + " " + startCity + " CA";
            string end = startAddress + " " + startCity + " CA";

            model.Add(new InspectionMapModel
            {
                Point = 0,
                Address = startAddress,
                City = startCity
            });

            var inspection = db.Inspections.FirstOrDefault(x => x.Id == id && x.DateCancelled == null);
            if (inspection != null)
            {
                string dco = inspection.DCO;
                DateTime dt = (DateTime)inspection.DateOfVisit;
                DateTime dt1 = dt.AddDays(1);

                IEnumerable<Inspection> inspections = db.Inspections.Where(x => x.DCO == dco
                    && x.DateOfVisit >= dt && x.DateOfVisit < dt1 && x.DateCancelled == null).ToList();
                int cnt = 1;
                foreach (var insp in inspections)
                {
                    InspectionMapModel imm = GetNearest(dco, dt, dt1, start, model);
                    imm.Point = cnt;
                    model.Add(imm);
                    start = imm.Address + " " + imm.City + " CA";
                    cnt++;
                }

                string wps = "wp.0=" + start;
                for (int i = 1; i < model.Count; i++)
                {
                    string wp = model[i].Address + " " + model[i].City + " CA";
                    wps += "&wp." + i.ToString() + "=" + wp;
                }
                wps += "&wp." + model.Count.ToString() + "=" + start;

                ViewBag.wps = wps;
                string last = model[model.Count - 1].Address + " " + model[model.Count - 1].City + " CA";
                model.Add(new InspectionMapModel { Point = model.Count, Address = startAddress, City = startCity, Miles = GetMiles(last, end) });

                ViewBag.CurrentAddress = inspection.Address;
                ViewBag.CO = dco;
                ViewBag.DateOfVisit = dt.ToShortDateString();
            }

            return PartialView("_DetailMap", model);
        }

        private InspectionMapModel GetNearest(string dco, DateTime dt, DateTime dt1, string start, List<InspectionMapModel> list)
        {
            Inspection wp = null;
            IEnumerable<Inspection> inspections = db.Inspections.Where(x => x.DCO == dco && x.DateOfVisit >= dt && x.DateOfVisit < dt1).ToList();
            int[] inspectionIds = list.Select(x => x.InspectionID).ToArray();
            IEnumerable<Inspection> covered = inspections.Where(x => inspectionIds.Any(y => y == x.Id));
            inspections = inspections.Except(covered);

            int cnt = 1;
            decimal d = 0.0m;
            foreach (var insp in inspections)
            {
                string end = insp.Address + " " + insp.City + " CA";
                decimal m = GetMiles(start, end);
                if (cnt == 1)
                {
                    wp = insp;
                    d = m;
                }
                else
                {
                    if (m < d)
                    {
                        wp = insp;
                        d = m;
                    }
                }
            }

            return new InspectionMapModel { InspectionID = wp.Id, Address = wp.Address, City = wp.City, Miles = d };
        }

        [HttpPost]
        public ActionResult Update(FormCollection form, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                int id = int.Parse(form["InspectionID"]);
                DateTime dateOfVisit = DateTime.Parse(form["DateOfVisit"]);
                Inspection inspection = db.Inspections.Find(id);

                int contractorId = int.Parse(form["Contractor"]);
                inspection.ContractorId = contractorId;

                inspection.DateOfVisit = dateOfVisit;
                inspection.NumberInterviews = int.Parse(form["NumberInterviews"]);
                decimal hours = Decimal.Parse(form["RoundTripHours"]);
                inspection.RoundTripHours = hours;
                decimal miles = decimal.Parse(form["RoundTripMiles"]);
                inspection.RoundTripMiles = (int)Math.Round(miles, 0);

                inspection.PhotosTaken = form["PhotosTaken"].Contains("true");
                inspection.Violations = form["Violations"].Contains("true");

                db.Entry(inspection).State = EntityState.Modified;
                db.SaveChanges();

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Details4", "Project", new { id = inspection.ProjectId });
                else
                    return Redirect(returnUrl);
            }

            return Redirect(returnUrl);
        }

        // GET: Inspection/Delete/5
        public ActionResult Delete(int? id, string returnUrl = "")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Inspection inspection = db.Inspections.Find(id);
            if (inspection == null)
            {
                return HttpNotFound();
            }

            ViewBag.ProjectID = inspection.ProjectId;
            ViewBag.ReturnUrl = returnUrl;

            return View(inspection);
        }

        // POST: Inspection/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, string returnUrl = "")
        {
            Inspection inspection = db.Inspections.Find(id);
            db.Inspections.Remove(inspection);
            db.SaveChanges();

            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Details4", "Project", new { id = inspection.ProjectId });
            else
                return Redirect(returnUrl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [HttpPost]
        public ActionResult UploadPhoto(FormCollection form, string returnUrl)
        {
            int inspectionId = int.Parse(form["InspectionID1"]);

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentType == "image/jpeg" && file.ContentLength > 0)
                {
                    try
                    {
                        var fileName = string.Concat(inspectionId.ToString(), "_", Path.GetFileName(file.FileName));
                        var path = Path.Combine(Server.MapPath("~/Files/Site_Inspection/Photos/"), fileName);
                        file.SaveAs(path);

                        InspectionPhoto photo = new InspectionPhoto
                        {
                            InspectionID = inspectionId,
                            FileName = fileName,
                            Path = path,
                            DateUploaded = DateTime.Now
                        };
                        db.InspectionPhotos.Add(photo);
                        db.SaveChanges();

                        TempData["Message"] = "Photo is uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = "Error: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Details", "Inspection", new { id = inspectionId });
        }

        public ActionResult ViewPhoto(int id, string returnUrl)
        {
            var model = db.InspectionPhotos.Where(x => x.InspectionID == id).ToList();

            ViewBag.InspectionID = id;
            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        [HttpPost]
        public ActionResult UploadInspectionDocument(FormCollection form, string returnUrl)
        {
            int id = int.Parse(form["Id"]);
            var inspection = db.Inspections.Find(id);

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        string url = "~/Files/Site_Inspection/Uploads/" + id.ToString() + "/";
                        var path = Server.MapPath(url);
                        DirectoryInfo di = new DirectoryInfo(path);
                        if (!di.Exists)
                        {
                            di.Create();
                        }
                        string fileName = Path.GetFileName(file.FileName);

                        var filePath = path + fileName.Replace("#", "_");
                        file.SaveAs(filePath);

                        string activity = "Inspection document uploaded";
                        DateTime now = DateTime.Now;
                        inspection.DateSiteInspectionUploaded = now;
                        inspection.DateLastUpdated = now;
                        inspection.Status = activity;
                        db.Entry(inspection).State = EntityState.Modified;

                        InspectionLog log = new InspectionLog
                        {
                            InspectionID = id,
                            Date = now,
                            Activity = activity,
                            ProcessedBy = UserProfile.UserInitial
                        };
                        db.InspectionLogs.Add(log);

                        Document document = new Document
                        {
                            ProjectID = inspection.ProjectId,
                            InspectionID = id,
                            DocumentName = "Inspection Document",
                            FileName = fileName,
                            Path = url,
                            DateUploaded = now
                        };
                        db.Documents.Add(document);
                        db.SaveChanges();

                        TempData["Message"] = "File is uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = "Error: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Details", "Inspection", new { id = id });
        }

        // POST: Inspection/DeletePhoto/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewPhoto(FormCollection form, int id, string returnUrl = "")
        {
            foreach (string c in form.Keys)
            {
                if (c.Contains("pid_"))
                {
                    bool isChecked = (form[c].Contains("true")) ? true : false;

                    if (isChecked)
                    {
                        int photoId = int.Parse(c.Replace("pid_", ""));
                        InspectionPhoto photo = db.InspectionPhotos.Find(photoId);
                        db.InspectionPhotos.Remove(photo);
                        db.SaveChanges();
                    }
                }
            }

            return RedirectToAction("ViewPhoto", "Inspection", new { id = id, returnUrl = returnUrl });
        }

        [HttpPost]
        public ActionResult UploadForm(FormCollection collection, int id)
        {
            string message = "";

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];
                int inspectionId = int.Parse(collection["UploadInspectionID"].Replace("u_", ""));

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        var fileName = string.Concat(id.ToString(), "_", inspectionId.ToString(), "_", Path.GetFileName(file.FileName));
                        var path = Path.Combine(Server.MapPath("~/Files/Site_Inspection/Forms/"), fileName);
                        file.SaveAs(path);

                        Inspection inspection = db.Inspections.Find(inspectionId);
                        //inspection.FileName = fileName;

                        db.Entry(inspection).State = EntityState.Modified;
                        db.SaveChanges();

                        message = "Investigation form is uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        message = "Error: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Details4", "Project", new { id = id, message = message });
        }

        public ActionResult DeleteForm(int id, int iId)
        {
            Inspection inspection = db.Inspections.Find(iId);
            //inspection.FileName = null;

            db.Entry(inspection).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Details4", "Project", new { id = id });
        }

        public ActionResult ApproveInspection(FormCollection form)
        {
            string status = "";
            string msg = "";
            string subject = "";
            int id = int.Parse(form["id"]);

            try
            {
                if (ModelState.IsValid)
                {
                    if (form["submit"].Contains("Approve"))
                    {
                        status = "SV approved by Manager";
                        msg = "'Site Visit' was approved successfully.";
                        subject = "'Site Visit' was approved.";
                    }
                    else if (form["submit"].Contains("Reject"))
                    {
                        status = "SV rejected by Manager";
                        msg = "'Site Visit' was rejected.";
                        subject = "'Site Visit' was rejected.";
                    }

                    var inspection = db.Inspections.First(x => x.Id == id);

                    DateTime now = DateTime.Now;
                    inspection.DateApproved = now;
                    inspection.DateLastUpdated = now;
                    inspection.Status = status;
                    db.Entry(inspection).State = EntityState.Modified;

                    InspectionLog log = new InspectionLog
                    {
                        InspectionID = id,
                        Date = now,
                        Activity = status,
                        ProcessedBy = UserProfile.UserInitial
                    };
                    if (!String.IsNullOrEmpty(form["Comment"]))
                        log.Comment = form["Comment"];

                    db.InspectionLogs.Add(log);

                    db.SaveChanges();

                    if (!String.IsNullOrEmpty(log.Comment))
                        SendMessage(inspection.ProjectId, inspection.DCO, subject, log.Comment);
                }
            }
            catch (Exception ex)
            {
                msg = "Error: There was an error.  " + ex.Message;
            }

            TempData["Message"] = msg;

            return RedirectToAction("Manager1", "Home");
        }

        public void SendMessage(int projectId, string recipient, string subject, string message)
        {
            string sender = UserProfile.UserInitial;

            var project = db.Projects.Find(projectId);
            MessageThread newThread = new MessageThread
            {
                ProjectId = projectId,
                JOC = project.JOC,
                Subject = subject,
                DateCreated = DateTime.Now
            };

            db.MessageThreads.Add(newThread);
            db.SaveChanges();

            Message newMessage = new Message
            {
                ThreadId = newThread.Id,
                Sender = sender,
                Recipient = recipient,
                Text = message,
                DateSent = DateTime.Now
            };

            db.Messages.Add(newMessage);
            db.SaveChanges();
        }

        public ActionResult SiteInspectionForm(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/");
            string template = "Template/SiteInspection.pdf";
            string fileName = "SiteInspection_" + id.ToString() + ".pdf";

            Inspection inspection = db.Inspections.FirstOrDefault(x => x.Id == id);
            Project project = db.Projects.FirstOrDefault(x => x.Id == inspection.ProjectId);
            Contractor contractor = db.Contractors.FirstOrDefault(x => x.Id == project.PrimeContractorID);

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            fields.SetField("Project Number", project.JOC);
            fields.SetField("Project Name", project.ProjectName);
            fields.SetField("Company Name", contractor.CompanyName);
            fields.SetField("Site Address", inspection.Address + ", " + inspection.City);
            fields.SetField("Date of Visit", inspection.DateOfVisit.ToString());

            if (project.StartDate != null)
                fields.SetField("Project Start Date", ((DateTime)project.StartDate).ToShortDateString());

            if (project.EndDate != null)
                fields.SetField("Estimate Project Completion Date", ((DateTime)project.EndDate).ToShortDateString());

            var prev = db.Inspections.Where(x => x.ProjectId == project.Id && x.ContractorId == contractor.Id && x.DateOfVisit < inspection.DateOfVisit)
                .OrderByDescending(x => x.DateOfVisit).ToList();
            if (prev.Count > 0)
            {
                fields.SetField("Date of Previous Site Visit", ((DateTime)prev.First().DateOfVisit).ToShortDateString());
            }

            if (project.FederalFunds)
                fields.SetField("Federal Fund Yes", "Yes");
            else
                fields.SetField("Federal Fund No", "Yes");

            fields.SetField("Date of Visit", DateTime.Parse(inspection.DateOfVisit.ToString()).ToShortDateString());
            fields.SetField("CCCS Manager initials", "D.S.");

            pdfStamper.Close();
            pdfReader.Close();

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,

                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());

            byte[] fileData = System.IO.File.ReadAllBytes(path + fileName);
            string contentType = MimeMapping.GetMimeMapping(path + fileName);

            return File(fileData, contentType);
        }

        public JsonResult GetProjects(string q)
        {
            List<Project> data = db.Projects.Where(x => x.JOC.StartsWith(q)).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddExplanation(string dco)
        {
            string week = Request.Form["week"];

            var ex = db.SiteVisitExceptions.FirstOrDefault(x => x.Week == week && x.DCO == dco);

            ex.CommentText = Request.Form["comment"];
            ex.DateComment = DateTime.Now;
            db.Entry(ex).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        #region Forms

        public ActionResult FormCN(int id)
        {
            var cn = db.ContractorNotifications.FirstOrDefault(x => x.InspectionID == id);

            ContractorNotification model = new ContractorNotification();
            if (cn == null)
            {
                var inspection = db.Inspections.Find(id);
                var project = db.Projects.Find(inspection.ProjectId);

                var user = db.UserProfiles.FirstOrDefault(x => x.UserInitial == inspection.DCO);
                var dco = UserManager.Users.FirstOrDefault(x => x.Id == user.UserID);
                model = new ContractorNotification
                {
                    InspectionID = inspection.Id,
                    JOC = project.JOC,
                    ProjectDescription = project.ProjectName,
                    Location = inspection.Address + " " + inspection.City + " " + inspection.Zip,
                    DateOfVisit = (inspection.DateOfVisit == null) ? "" : ((DateTime)inspection.DateOfVisit.Value).ToShortDateString(),
                    DCO = user.FullName,
                    DcoContactInfo = dco.Email + "; " + dco.PhoneNumber
                };
            }
            else
            {
                model = cn;
                ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/ContractorNotification/ContractorNotification_" + model.InspectionID.ToString() + ".pdf");
            }

            ViewBag.ReturnUrl = Request.UrlReferrer.ToString();

            return View(model);
        }

        [HttpPost]
        public ActionResult FormCN(ContractorNotification model, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                var cn = db.ContractorNotifications.FirstOrDefault(x => x.InspectionID == model.InspectionID);
                if (cn != null)
                {
                    db.ContractorNotifications.Remove(cn);
                }

                db.ContractorNotifications.Add(model);
                db.SaveChanges();

                SaveContractorNotification(model.InspectionID);
            }

            TempData["Message"] = "Data was saved successfully.";
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/ContractorNotification/ContractorNotification_" + model.InspectionID.ToString() + ".pdf");

            return View(model);
        }

        public ActionResult FormSI(int id)
        {
            var si = db.SiteInspections.FirstOrDefault(x => x.InspectionID == id);
            var inspection = db.Inspections.Find(id);

            SiteInspection model = new SiteInspection();
            if (si == null)
            {
                var project = db.Projects.Find(inspection.ProjectId);

                var user = db.UserProfiles.FirstOrDefault(x => x.UserInitial == inspection.DCO);
                var dco = UserManager.Users.FirstOrDefault(x => x.Id == user.UserID);
                model = new SiteInspection
                {
                    InspectionID = inspection.Id,
                    ProjectNumber = project.JOC,
                    ProjectDescription = project.ProjectName,
                    ProjectAddress = inspection.Address + " " + inspection.City + " " + inspection.Zip,
                    DateOfVisit = (inspection.DateOfVisit == null) ? "" : ((DateTime)inspection.DateOfVisit.Value).ToShortDateString(),
                    DCO = user.FullName,
                };

                var contractor = db.Contractors.Find(project.PrimeContractorID);
                if (contractor != null)
                    model.Contractor = contractor.CompanyName;
            }
            else
            {
                model = si;
                ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/SiteInspection/SiteInspection_" + model.InspectionID.ToString() + ".pdf");
            }

            ViewBag.Violations = inspection.Violations;
            ViewBag.ReturnUrl = Request.UrlReferrer.ToString();

            return View(model);
        }

        [HttpPost]
        public ActionResult FormSI(SiteInspection model, string returnUrl = "")
        {
            var inspection = db.Inspections.Find(model.InspectionID);
            if (ModelState.IsValid)
            {
                var si = db.SiteInspections.FirstOrDefault(x => x.InspectionID == model.InspectionID);
                if (si != null)
                {
                    db.SiteInspections.Remove(si);
                }

                db.SiteInspections.Add(model);
                db.SaveChanges();

                SaveSiteInspection(model.InspectionID);

                TempData["Message"] = "Data was saved successfully.";
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/SiteInspection/SiteInspection_" + model.InspectionID.ToString() + ".pdf");

            return View(model);
        }

        public ActionResult ViewSI(int id, string returnUrl = "")
        {
            string filePath = CommonHelper.CreateSI(id);

            if (!String.IsNullOrEmpty(filePath))
            {
                return Redirect("~/Files/Site_Inspection/Forms/SiteInspection/SiteInspection_" + id.ToString() + ".pdf");
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        public ActionResult FormSVC(int id)
        {
            var svc = db.SiteVisitCompletions.FirstOrDefault(x => x.InspectionID == id);
            var inspection = db.Inspections.Find(id);

            SiteVisitCompletion model = new SiteVisitCompletion();
            if (svc == null)
            {
                var project = db.Projects.Find(inspection.ProjectId);

                var user = db.UserProfiles.FirstOrDefault(x => x.UserInitial == inspection.DCO);
                var dco = UserManager.Users.FirstOrDefault(x => x.Id == user.UserID);
                model = new SiteVisitCompletion
                {
                    InspectionID = inspection.Id,
                    ProjectNumber = project.JOC,
                    ProjectDescription = project.ProjectName,
                    ProjectAddress = inspection.Address + " " + inspection.City + " " + inspection.Zip,
                    DateOfVisit = (inspection.DateOfVisit == null) ? "" : ((DateTime)inspection.DateOfVisit.Value).ToShortDateString(),
                    DCO = user.FullName,
                    DcoContactInfo = dco.Email + "; " + dco.PhoneNumber,
                    NoViolations = !inspection.Violations
                };

                var department = db.Departments.Find(project.DepartmentID);
                if (department != null)
                    model.Department = department.DepartmentName;

                var contacts = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == inspection.ProjectId);
                if (contacts != null)
                    model.DepartmentContact = contacts.DeptContact;

                var contractor = db.Contractors.Find(project.PrimeContractorID);
                if (contractor != null)
                    model.Contractor = contractor.CompanyName;
            }
            else
            {
                svc.NoViolations = !inspection.Violations;
                db.Entry(svc).State = EntityState.Modified;
                db.SaveChanges();

                model = svc;
                ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/SiteVisitCompletion/SiteVisitCompletion_" + model.InspectionID.ToString() + ".pdf");
            }

            ViewBag.Violations = inspection.Violations;
            ViewBag.ReturnUrl = Request.UrlReferrer.ToString();

            if (inspection.Status == "SVC approved by Manager")
                ViewBag.SendTo = "Department";
            else if (inspection.Status == "SVC sent to Manager")
                ViewBag.SendTo = "Pending";
            else if (inspection.Status == "SVC email notification")
                ViewBag.SendTo = "Email";

            return View(model);
        }

        [HttpPost]
        public ActionResult FormSVC(SiteVisitCompletion model, string returnUrl = "")
        {
            var inspection = db.Inspections.Find(model.InspectionID);
            if (ModelState.IsValid)
            {
                var svc = db.SiteVisitCompletions.FirstOrDefault(x => x.InspectionID == model.InspectionID);
                if (svc != null)
                {
                    db.SiteVisitCompletions.Remove(svc);
                }

                model.NoViolations = !inspection.Violations;
                db.SiteVisitCompletions.Add(model);
                db.SaveChanges();

                SaveSiteVisitCompletion(model.InspectionID);

                if (inspection.Status == "SVC approved by Manager")
                    ViewBag.SendTo = "Department";
                else if (inspection.Status == "SVC sent to Manager")
                    ViewBag.SendTo = "Pending";

                TempData["Message"] = "Data was saved successfully.";
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/SiteVisitCompletion/SiteVisitCompletion_" + model.InspectionID.ToString() + ".pdf");
            ViewBag.Violations = inspection.Violations;

            return View(model);
        }

        public ActionResult ViewSVC(int id, string returnUrl = "")
        {
            string filePath = CommonHelper.CreateSVC(id);

            if (!String.IsNullOrEmpty(filePath))
            {
                return Redirect("~/Files/Site_Inspection/Forms/SiteVisitCompletion/SiteVisitCompletion_" + id.ToString() + ".pdf");
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        public ActionResult FormVC(int id)
        {
            var vc = db.ViolationCorrections.FirstOrDefault(x => x.InspectionID == id);
            var inspection = db.Inspections.Find(id);

            ViolationCorrection model = new ViolationCorrection();
            if (vc == null)
            {
                var project = db.Projects.Find(inspection.ProjectId);

                var user = db.UserProfiles.FirstOrDefault(x => x.UserInitial == inspection.DCO);
                var dco = UserManager.Users.FirstOrDefault(x => x.Id == user.UserID);
                model = new ViolationCorrection
                {
                    InspectionID = inspection.Id,
                    ProjectNumber = project.JOC,
                    ProjectDescription = project.ProjectName,
                    ProjectAddress = inspection.Address + " " + inspection.City + " " + inspection.Zip,
                    DateOfVisit = (inspection.DateOfVisit == null) ? "" : ((DateTime)inspection.DateOfVisit.Value).ToShortDateString(),
                    DCO = user.FullName,
                    DcoContactInfo = dco.Email + "; " + dco.PhoneNumber
                };

                var department = db.Departments.Find(project.DepartmentID);
                if (department != null)
                    model.Department = department.DepartmentName;

                var contacts = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == inspection.ProjectId);
                if (contacts != null)
                    model.DepartmentContact = contacts.DeptContact;

                var contractor = db.Contractors.Find(project.PrimeContractorID);
                if (contractor != null)
                    model.Contractor = contractor.CompanyName;
            }
            else
            {
                model = vc;
                ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/ViolationCorrection/ViolationCorrection_" + model.InspectionID.ToString() + ".pdf");
            }

            ViewBag.Violations = inspection.Violations;
            ViewBag.ReturnUrl = Request.UrlReferrer.ToString();

            var log = db.InspectionLogs.FirstOrDefault(x => x.InspectionID == id && x.Activity.Contains("VC approved by Manager"));
            ViewBag.SendTo = (log == null) ? "Manager" : "Department";

            return View(model);
        }

        [HttpPost]
        public ActionResult FormVC(ViolationCorrection model, string returnUrl = "")
        {
            var inspection = db.Inspections.Find(model.InspectionID);
            if (ModelState.IsValid)
            {
                var vc = db.ViolationCorrections.FirstOrDefault(x => x.InspectionID == model.InspectionID);
                if (vc != null)
                {
                    db.ViolationCorrections.Remove(vc);
                }

                db.ViolationCorrections.Add(model);
                db.SaveChanges();

                db.Entry(inspection).State = EntityState.Modified;
                db.SaveChanges();

                SaveFormVC(model.InspectionID);

                TempData["Message"] = "Data was saved successfully.";
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FileUrl = Url.Content("/Files/Site_Inspection/Forms/ViolationCorrection/ViolationCorrection_" + model.InspectionID.ToString() + ".pdf");
            ViewBag.Violations = inspection.Violations;

            return View(model);
        }

        private void SaveContractorNotification(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/ContractorNotification/");
            string template = "Template/ContractorNotification.pdf";
            string fileName = "ContractorNotification_" + id.ToString() + ".pdf";

            ContractorNotification cn = db.ContractorNotifications.FirstOrDefault(x => x.InspectionID == id);

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            fields.SetField("Contractor Representative", cn.ContractorRepresentative);

            fields.SetField("Project Number", cn.JOC);
            fields.SetField("Project Description", cn.ProjectDescription);

            fields.SetField("Date of Site Visit", cn.DateOfVisit);
            fields.SetField("Time of Visit", cn.TimeOfVisit);
            fields.SetField("Project Location", cn.Location);

            fields.SetField("DCO Name", cn.DCO);
            fields.SetField("DCO Contact Information", cn.DcoContactInfo);

            if (cn.SiteInspection)
                fields.SetField("chkSiteInspection", "X");
            if (cn.EmployeeInterviews)
                fields.SetField("chkEmployeeInterviews", "X");
            if (cn.GoodFaithReview)
                fields.SetField("chkGoodFaithReview", "X");

            pdfStamper.Close();
            pdfReader.Close();
        }

        private void SaveSiteInspection(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/SiteInspection/");
            string template = "Template/SiteInspection.pdf";
            string fileName = "SiteInspection_" + id.ToString() + ".pdf";

            SiteInspection svc = db.SiteInspections.FirstOrDefault(x => x.InspectionID == id);

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            fields.SetField("Project Number", svc.ProjectNumber);
            fields.SetField("Project Description", svc.ProjectDescription);
            fields.SetField("Project Address", svc.ProjectAddress);
            fields.SetField("Prime and Sub Contractor", svc.Contractor);

            fields.SetField("DCO", svc.DCO);
            fields.SetField("Site Visit Date", svc.DateOfVisit);

            pdfStamper.Close();
            pdfReader.Close();
        }

        private void SaveSiteVisitCompletion(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/");
            string template = "Template/SiteVisitCompletion.pdf";
            string fileName = "SiteVisitCompletion_" + id.ToString() + ".pdf";

            SiteVisitCompletion svc = db.SiteVisitCompletions.FirstOrDefault(x => x.InspectionID == id);

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            fields.SetField("Department", svc.Department);
            fields.SetField("Department Contact", svc.DepartmentContact);

            fields.SetField("Todays Date", svc.TodaysDate);
            fields.SetField("Department ContactPhone", svc.DepartmentContact);

            fields.SetField("Project Number", svc.ProjectNumber);
            fields.SetField("Project Description", svc.ProjectDescription);
            fields.SetField("Project Address", svc.ProjectAddress);
            fields.SetField("Contractor", svc.Contractor);

            fields.SetField("DCO", svc.DCO);
            fields.SetField("Contact Info", svc.DcoContactInfo);
            fields.SetField("Site Visit Date", svc.DateOfVisit);

            if (svc.NoViolations)
                fields.SetField("chkViolations", "X");

            if (svc.EeoPostings)
                fields.SetField("EEO Postings", "X");
            if (svc.Graffiti)
                fields.SetField("Graffiti", "X");
            if (svc.SegregatedFacilities)
                fields.SetField("Segregated Facilities", "X");
            if (svc.Referrals)
                fields.SetField("Referrals to OFCCADFEHDHR", "X");

            fields.SetField("Comments", svc.Comments);

            pdfStamper.Close();
            pdfReader.Close();
        }

        private void SaveFormVC(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/ViolationCorrection/");
            string template = "Template/ViolationCorrection.pdf";
            string fileName = "ViolationCorrection_" + id.ToString() + ".pdf";

            ViolationCorrection vc = db.ViolationCorrections.FirstOrDefault(x => x.InspectionID == id);

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            fields.SetField("Department", vc.Department);

            fields.SetField("Todays Date", vc.TodaysDate);
            fields.SetField("Department ContactPhone", vc.DepartmentContact);

            fields.SetField("Project Number", vc.ProjectNumber);
            fields.SetField("Project Description", vc.ProjectDescription);
            fields.SetField("Project Address", vc.ProjectAddress);
            fields.SetField("Prime and Sub Contractor", vc.Contractor);

            fields.SetField("DCO Contact Info", vc.DCO + ", " + vc.DcoContactInfo);
            fields.SetField("Date of Visit", vc.DateOfVisit);

            if (vc.EeoPostings)
                fields.SetField("EEO Postings", "X");
            if (vc.Graffiti)
                fields.SetField("Graffiti", "X");
            if (vc.SegregatedFacilities)
                fields.SetField("Segregated Facilities", "X");
            if (vc.Referrals)
                fields.SetField("Referrals", "X");

            fields.SetField("Comments", vc.Comments);

            pdfStamper.Close();
            pdfReader.Close();
        }

        public ActionResult ApproveSVC(FormCollection form)
        {
            int id = int.Parse(form["id"]);
            string activity = (form["submit"] == "Approve") ? "SVC approved by Manager" : "SVC rejected by Manager";

            DateTime now = DateTime.Now;
            var inspection = db.Inspections.Find(id);
            inspection.DateLastUpdated = now;
            inspection.Status = activity;
            db.Entry(inspection).State = EntityState.Modified;

            InspectionLog log = new InspectionLog
            {
                InspectionID = id,
                Date = now,
                ProcessedBy = UserProfile.UserInitial
            };

            log.Activity = activity;
            db.InspectionLogs.Add(log);

            db.SaveChanges();

            return RedirectToAction("Manager1", "Home");
        }

        public ActionResult SendVcToManager(int id, string returnUrl)
        {
            string activity = "VC sent to Manager";

            DateTime now = DateTime.Now;
            var inspection = db.Inspections.Find(id);
            inspection.DateLastUpdated = now;
            inspection.Status = activity;
            db.Entry(inspection).State = EntityState.Modified;

            InspectionLog log = new InspectionLog
            {
                InspectionID = id,
                Date = now,
                Activity = activity,
                ProcessedBy = UserProfile.UserInitial
            };
            db.InspectionLogs.Add(log);
            db.SaveChanges();

            TempData["Message"] = "'Violation Correction' was sent to Manager for approval.";

            return Redirect(returnUrl);
        }

        public ActionResult SendSvcToManager(int id, string returnUrl)
        {
            string activity = "SVC sent to Manager";
            DateTime now = DateTime.Now;

            var inspection = db.Inspections.Find(id);
            inspection.Status = activity;
            inspection.DateLastUpdated = now;
            db.Entry(inspection).State = EntityState.Modified;

            InspectionLog log = new InspectionLog
            {
                InspectionID = id,
                Date = now,
                Activity = activity,
                ProcessedBy = UserProfile.UserInitial
            };
            db.InspectionLogs.Add(log);

            db.SaveChanges();

            TempData["Message"] = "'Site Visit Completion' was sent to Manager for approval.";

            return Redirect(returnUrl);
        }

        #endregion

        [HttpPost]
        public ActionResult CancelSiteVisitRequest(FormCollection form, string returnUrl)
        {
            string status = "Site Visit Cancelled";
            DateTime now = DateTime.Now;
            int id = int.Parse(form["InspectionID2"]);
            string comment = form["Comment"];

            Inspection inspection = db.Inspections.Find(id);
            inspection.Status = status;
            inspection.DateCancelled = now;
            inspection.DateLastUpdated = now;
            db.Entry(inspection).State = EntityState.Modified;

            InspectionLog log = new InspectionLog
            {
                InspectionID = id,
                Activity = status,
                Comment = comment,
                Date = now,
                ProcessedBy = UserProfile.UserInitial
            };
            db.InspectionLogs.Add(log);

            db.SaveChanges();

            ViewBag.ReturnUrl = returnUrl;

            return Redirect(returnUrl);
        }

        public ActionResult ApproveVC(FormCollection form)
        {
            int id = int.Parse(form["id"]);

            string status = (form["submit"] == "Approve") ? "VC approved by Manager" : "VC rejected by Manager";

            DateTime now = DateTime.Now;

            Inspection inspection = db.Inspections.Find(id);
            inspection.Status = status;
            inspection.DateLastUpdated = now;
            db.Entry(inspection).State = EntityState.Modified;

            InspectionLog log = new InspectionLog
            {
                InspectionID = id,
                Date = DateTime.Now,
                ProcessedBy = UserProfile.UserInitial
            };
            log.Activity = status;
            db.InspectionLogs.Add(log);
            db.SaveChanges();

            return RedirectToAction("Manager1", "Home");
        }

        public PartialViewResult InspectionLog(int id)
        {
            var model = db.InspectionLogs.Where(x => x.InspectionID == id).ToList();

            return PartialView("_InspectionLog", model);
        }

        public PartialViewResult InspectionDocument(int id)
        {
            var model = db.Documents.Where(x => x.InspectionID == id && x.DocumentName == "Inspection Document")
                .OrderBy(x => x.DateUploaded).ToList();

            return PartialView("_InspectionDocument", model);
        }

        public ActionResult Worksheet(int id, string returnUrl)
        {
            var inspection = db.Inspections.Find(id);
            if (inspection == null)
            {
                return HttpNotFound();
            }
            int projectId = inspection.ProjectId;

            var model = new InspectionWorksheetModel
            {
                InspectionId = id,
                ProjectId = projectId,
                DateOfVisit = inspection.DateOfVisit,
                RoundTripHours = inspection.RoundTripHours,
                RoundTripMiles = inspection.RoundTripMiles,
                PhotosTaken = inspection.PhotosTaken,
                Violations = inspection.Violations
            };

            var project = db.Projects.FirstOrDefault(x => x.Id == projectId);
            model.ProjectName = project.ProjectName;
            model.JOC = project.JOC;
            model.PrimeContractorId = project.PrimeContractorID;

            var interviews = (from i in db.InspectionInterviews
                              join c in db.Contractors on i.ContractorId equals c.Id
                              where i.InspectionId == id
                              select new InterviewModel
                              {
                                  Id = i.Id,
                                  ContractorId = i.ContractorId,
                                  CompanyName = c.CompanyName,
                                  Hours = i.Hours
                              }).ToList();

            Session["interviews"] = interviews;
            model.Interviews = interviews;

            GetContractors(projectId);

            return View(model);
        }

        [HttpPost]
        public ActionResult Worksheet(FormCollection form, string returnUrl)
        {
            int id = int.Parse(form["InspectionID"]);

            if (form["submit"] == "Save")
            {
                if (ModelState.IsValid)
                {
                    var interviews = db.InspectionInterviews.Where(x => x.InspectionId == id).ToList();
                    foreach (var interview in interviews)
                    {
                        db.InspectionInterviews.Remove(interview);
                    }
                    db.SaveChanges();

                    var list = Session["interviews"] as List<InterviewModel>;
                    foreach (var i in list)
                    {
                        var interview = new InspectionInterview
                        {
                            InspectionId = id,
                            ContractorId = i.ContractorId,
                            Hours = i.Hours
                        };

                        db.InspectionInterviews.Add(interview);
                    }
                    db.SaveChanges();

                    DateTime dateOfVisit = DateTime.Parse(form["DateOfVisit"]);
                    Inspection inspection = db.Inspections.Find(id);

                    inspection.DateOfVisit = dateOfVisit;
                    inspection.NumberInterviews = list.Count;

                    decimal hours = Decimal.Parse(form["RoundTripHours"]);
                    inspection.RoundTripHours = hours;
                    decimal miles = decimal.Parse(form["RoundTripMiles"]);
                    inspection.RoundTripMiles = (int)Math.Round(miles, 0);

                    inspection.PhotosTaken = form["PhotosTaken"].Contains("true");
                    inspection.Violations = form["Violations"].Contains("true");

                    db.Entry(inspection).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Details", new { id = id });
        }

        public string AddInterview(FormCollection form)
        {
            var id = int.Parse(form["inspectionId"]);
            var contractorId = int.Parse(form["contractorId"]);
            var hours = decimal.Parse(form["hours"]);

            var list = (Session["interviews"] == null) ? new List<InterviewModel>() : Session["interviews"] as List<InterviewModel>;
            var item = list.FirstOrDefault(x => x.Id == id && x.ContractorId == contractorId);

            string html = "";
            if (item == null)
            {
                var interview = new InterviewModel
                {
                    Id = id,
                    ContractorId = contractorId,
                    CompanyName = db.Contractors.Find(contractorId).CompanyName,
                    Hours = hours
                };

                list.Add(interview);
                Session["interviews"] = list;

            }

            foreach (var l in list)
            {
                html += "<tr><td class=\"text-left\">" + l.CompanyName + "</td>";
                html += "<td class=\"text-right\">" + l.Hours.ToString("0.00") + "</td>";
                html += "<td class=\"text-right\"><a onclick=\"deleteInterview(" + l.Id + "," + l.ContractorId + ");\" href=\"#\">Delete</a>";
                html += "</td></tr>";
            }

            return html;
        }

        public string DeleteInterview(int inspectionId, int contractorId)
        {
            var list = Session["interviews"] as List<InterviewModel>;
            var interview = list.FirstOrDefault(x => x.Id == inspectionId && x.ContractorId == contractorId);
            list.Remove(interview);
            Session["interviews"] = list;

            string html = "";
            foreach (var l in list)
            {
                html += "<tr><td class=\"text-left\">" + l.CompanyName + "</td>";
                html += "<td class=\"text-right\">" + l.Hours.ToString("0.00") + "</td>";
                html += "<td class=\"text-right\"><a onclick=\"deleteInterview(" + l.Id + "," + l.ContractorId + ");\" href=\"#\">Delete</a>";
                html += "</td></tr>";
            }

            return html;
        }
    }
}
