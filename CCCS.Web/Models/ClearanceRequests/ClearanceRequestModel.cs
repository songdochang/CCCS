using CCCS.Data;
using CCCS.Infrastructure;
using CCCS.Web.Models.Worksheets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;

namespace CCCS.Web.Models.ClearanceRequests
{
    public class ClearanceRequestModel
    {
        const string CLEARANCE_REQUEST_MODEL_KEY = "CLEARANCE_REQUEST_MODEL_{0}";

        public int ClearanceRequestId { get; set; }
        public int ProjectID { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        public int PrimeContractorID { get; set; }
        public string PrimeContractorName { get; set; }
        public string DCO { get; set; }

        [Display(Name = "Date Requested")]
        public DateTime DateRequested { get; set; }
        public DateTime? DateRejected { get; set; }
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }
        public string Department { get; set; }

        [Display(Name = "Date Modified")]
        public DateTime? DateModified { get; set; }
        public DateTime? DateClosed { get; set; }
        public string CurrentStatus { get; set; }
        public string FileUrl { get; set; }
        public int IsCleared { get; set; }
        public string Comment { get; set; }
        public Decimal? TotalCharged { get; set; }
        public int NumberMissingDocuments { get; set; }

        public static List<ClearanceRequestModel> GetRejectedModel()
        {
            using (ContractContext db = new ContractContext())
            {
                var list = from p in db.Projects
                           join cr in db.ClearanceRequests on p.Id equals cr.ProjectId
                           join l in db.ClearanceRequestLogs on cr.Id equals l.ClearanceRequestId
                           where l.Activity.Contains("Rejected")
                           join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                           from x in pc.DefaultIfEmpty()
                           select new ClearanceRequestModel
                           {
                               ClearanceRequestId = cr.Id,
                               DCO = p.DCO,
                               ProjectID = cr.ProjectId,
                               JOC = p.JOC,
                               ProjectName = p.ProjectName,
                               PrimeContractorID = p.PrimeContractorID,
                               PrimeContractorName = x.CompanyName,
                               DateRequested = cr.DateRequested,
                               RequestedBy = cr.RequestedBy,
                               DateModified = cr.DateModified,
                               DateClosed = p.DateClosed,
                               DateRejected = l.Date,
                               Comment = l.Comment
                           };

                List<ClearanceRequestModel> model = list.ToList();

                string url = ConfigurationManager.AppSettings["publicUrl"] + "Files/Clearance_Request/ClearanceRequest_{0}.pdf";
                foreach (var m in model)
                {
                    var user = db.PublicProfiles.FirstOrDefault(x => x.UserID == m.RequestedBy);
                    if (user != null)
                    {
                        m.RequestedBy = user.Name;
                        m.Department = user.Department;
                    }
                    else
                    {
                        m.RequestedBy = "";
                        m.Department = "";
                    }
                    m.FileUrl = string.Format(url, m.ProjectID);
                }

                return model;
            }
        }

        public static List<ClearanceRequestModel> GetModel(bool isClosed)
        {
            List<ClearanceRequestModel> model = new List<ClearanceRequestModel>();

            using (ContractContext db = new ContractContext())
            {
                var ws = db.Worksheets.Where(x => x.ProjectId != null).Select(x => new { ProjectId = (int)x.ProjectId, TotalHours = x.Hours + x.Minutes / 60.0m })
                    .GroupBy(x => x.ProjectId)
                    .Select(g => new { ProjectID = g.Key, TotalHours = g.Sum(s => s.TotalHours) });

                var list = from p in db.Projects
                           join cr in db.ClearanceRequests on p.Id equals cr.ProjectId
                           join c in db.Contractors on p.PrimeContractorID equals c.Id into pc
                           from x in pc.DefaultIfEmpty()
                           join w in ws on p.Id equals w.ProjectID into pw
                           from y in pw.DefaultIfEmpty()
                           select new ClearanceRequestModel
                           {
                               DCO = p.DCO,
                               ProjectID = cr.ProjectId,
                               JOC = p.JOC,
                               ProjectName = p.ProjectName,
                               PrimeContractorID = p.PrimeContractorID,
                               PrimeContractorName = x.CompanyName,
                               DateRequested = cr.DateRequested,
                               RequestedBy = cr.RequestedBy,
                               DateModified = cr.DateModified,
                               DateClosed = p.DateClosed,
                               CurrentStatus = cr.CurrentStatus,
                               TotalCharged = y.TotalHours
                           };

                model = (isClosed) ? list.Where(x => x.CurrentStatus.Contains("to Department") || x.CurrentStatus.Contains("Closed")).ToList()
                                                               : list.Where(x => !x.CurrentStatus.Contains("to Department") && !x.CurrentStatus.Contains("Closed")).ToList();

                string url = ConfigurationManager.AppSettings["publicUrl"] + "Files/Clearance_Request/ClearanceRequest_{0}.pdf";
                foreach (var m in model)
                {
                    var user = db.PublicProfiles.FirstOrDefault(x => x.UserID == m.RequestedBy);
                    if (user != null)
                    {
                        m.RequestedBy = user.Name;
                        m.Department = user.Department;
                    }
                    else
                    {
                        m.RequestedBy = "";
                        m.Department = "";
                    }
                    m.FileUrl = string.Format(url, m.ProjectID);
                    m.TotalCharged = (m.TotalCharged == null) ? 0.0m : m.TotalCharged;
                }

                return model;
            }
        }

        public static List<ClearanceRequestModel> Search(string searchString, bool isClosed = false)
        {
            searchString = searchString.ToLower();

            using (ContractContext db = new ContractContext())
            {
                var list = (from p in db.Projects
                            join cr in db.ClearanceRequests on p.Id equals cr.ProjectId
                            join c in db.Contractors on p.PrimeContractorID equals c.Id
                            where p.JOC.ToLower().Contains(searchString)
                            select new ClearanceRequestModel
                            {
                                DCO = p.DCO,
                                ProjectID = cr.ProjectId,
                                JOC = p.JOC,
                                ProjectName = p.ProjectName,
                                PrimeContractorID = p.PrimeContractorID,
                                PrimeContractorName = c.CompanyName,
                                DateRequested = cr.DateRequested,
                                DateModified = cr.DateModified,
                                CurrentStatus = cr.CurrentStatus
                            }).ToList();

                List<ClearanceRequestModel> model = (isClosed) ? list.Where(x => x.CurrentStatus == "Sent to Dept").ToList() : list.Where(x => x.CurrentStatus != "Sent to Dept").ToList();

                string url = ConfigurationManager.AppSettings["publicUrl"] + "Files/Clearance_Request/ClearanceRequest_{0}.pdf";
                foreach (var m in model)
                {
                    m.FileUrl = string.Format(url, m.ProjectID);
                }

                return model;
            }
        }

    }
}
