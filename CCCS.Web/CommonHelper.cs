using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Documents;
using CCCS.Core.Domain.Inspection;
using CCCS.Core.Domain.Projects;
using CCCS.Data;
using CCCS.Web.Models;
using CCCS.Web.Models.ClearanceRequests;
using CCCS.Web.Models.Contractors;
using CCCS.Web.Models.Documents;
using CCCS.Web.Models.Projects;
using CCCS.Web.Models.Worksheets;
using iTextSharp.text.pdf;
using Microsoft.AspNet.Identity;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace CCCS.Infrastructure
{
    public static class CommonHelper
    {
        static string CR_EXCEPTION_LIST_KEY = "CCCS_CR_EXCEPTION_LIST";

        public static List<DepartmentalBillingModel> GetDeptBillingModel(string month, string dept, string fundOrg)
        {
            using (ContractContext db = new ContractContext())
            {
                DateTime firstDate = DateTime.Parse(month.Substring(0, 2) + "/01/" + month.Substring(3, 4));
                DateTime lastDate = firstDate.AddMonths(1).AddDays(-1);

                var model = (from w in db.Worksheets
                             join p in db.Projects on w.ProjectID equals p.ProjectID
                             join a in db.AccountNumbers on p.Unit equals a.AccountNo
                             where w.WorkDate >= firstDate && w.WorkDate <= lastDate
                             select new DepartmentalBillingModel
                             {
                                 WorksheetID = w.WorksheetID,
                                 WorkDate = w.WorkDate,
                                 Unit = p.Unit,
                                 ActivityCode = w.ActivityCode,
                                 ProjectID = p.ProjectID,
                                 Unit1 = p.Unit,
                                 ActivityCode1 = w.ActivityCode,
                                 JOC = p.JOC,
                                 JOC1 = p.JOC,
                                 ProjectDescription = p.ProjectName,
                                 DepartmentID = a.DepartmentID,
                                 DCO = w.DCO,
                                 TotalHours = (w.Minutes == 0) ? w.Hours : w.Hours + 0.5m
                             }).ToList();

                if (!string.IsNullOrEmpty(dept))
                {
                    model = model.Where(x => x.DepartmentID == dept).ToList();
                }
                if (!string.IsNullOrEmpty(fundOrg))
                {
                    model = model.Where(x => x.Unit.StartsWith(fundOrg)).ToList();
                }

                foreach (var m in model)
                {
                    var user = db.UserProfiles.FirstOrDefault(x => x.UserInitial == m.DCO);
                    if (user != null)
                    {
                        m.EmployeeName = user.FullName;
                    }

                    var contact = db.ProjectContacts.FirstOrDefault(x => x.ProjectID == m.ProjectID);
                    if (contact != null)
                    {
                        if (!String.IsNullOrEmpty(contact.DeptContact))
                        {
                            m.DepartmentContactName = contact.DeptContact;
                            m.DepartmentContactPhone = contact.DeptContactPhoneNumber;
                        }
                        else
                        {
                            m.DepartmentContactName = contact.ProjectManager;
                            m.DepartmentContactPhone = contact.ProjectManagerPhoneNumber;
                        }
                    }
                }

                return model;
            }
        }

        public static List<NonComplianceModel> GetPastDueDocumentModel(string dept)
        {
            using (ContractContext db = new ContractContext())
            {
                var query = from n in db.NonCompliances
                            join p in db.Projects on n.ProjectID equals p.ProjectID
                            join c in db.Contractors on n.ContractorID equals c.ContractorID
                            select new NonComplianceModel
                            {
                                ID = n.ID,
                                DCO = p.DCO,
                                ProjectID = p.ProjectID,
                                ContractorID = c.ContractorID,
                                ContractorName = c.CompanyName,
                                JOC = p.JOC,
                                ProjectName = p.ProjectName,
                                DepartmentID = p.DepartmentID,
                                StartDate = (DateTime)p.StartDate,
                                EndDate = (DateTime)p.EndDate,
                                Year = n.Year,
                                Month = n.Month,
                                DocumentType = n.DocumentType,
                                DateRequired = n.DateRequired,
                                DateReceived = n.DateReceived,
                                IsExcluded = n.IsExcluded,
                                IsPrime = (c.ContractorID == p.PrimeContractorID)
                            };

                var model = (String.IsNullOrEmpty(dept)) ? query.ToList() : query.Where(x => x.DepartmentID == dept).ToList();

                return model;
            }
        }

        public static List<NonComplianceModel> GetExcludedPastDueDocumentModel(string dept)
        {
            using (ContractContext db = new ContractContext())
            {
                var query = from n in db.ExcludedNonCompliances
                            join p in db.Projects on n.ProjectID equals p.ProjectID
                            join c in db.Contractors on n.ContractorID equals c.ContractorID
                            orderby p.StartDate, n.DocumentType, p.ProjectName, n.Year, n.Month
                            select new NonComplianceModel
                            {
                                ID = n.ID,
                                DCO = p.DCO,
                                ProjectID = p.ProjectID,
                                ContractorID = c.ContractorID,
                                ContractorName = c.CompanyName,
                                JOC = p.JOC,
                                ProjectName = p.ProjectName,
                                DepartmentID = p.DepartmentID,
                                StartDate = (DateTime)p.StartDate,
                                EndDate = (DateTime)p.EndDate,
                                Year = n.Year,
                                Month = n.Month,
                                DocumentType = n.DocumentType,
                                DateRequired = n.DateRequired,
                                DateReceived = n.DateReceived,
                                Comment = n.Comment,
                                IsPrime = (c.ContractorID == p.PrimeContractorID)
                            };

                var model = (String.IsNullOrEmpty(dept)) ? query.ToList() : query.Where(x => x.DepartmentID == dept).ToList();

                return model;
            }
        }


        public static void Upload(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);

            string ftpUrl = ConfigurationManager.AppSettings["ftpUrl"] + fi.Name;

            FtpWebRequest reqFTP;

            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpUrl));
            reqFTP.Credentials = new NetworkCredential("e510974", "Cs2364do!");

            reqFTP.KeepAlive = false;
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            reqFTP.UseBinary = true;
            reqFTP.ContentLength = fi.Length;

            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;

            FileStream fs = fi.OpenRead();

            try
            {
                Stream strm = reqFTP.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);

                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }

                strm.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public static string CreateCR(int id)
        {
            string template = HostingEnvironment.MapPath("~/Files/Clearance_Request/Template/ClearanceRequest.pdf");
            string filePath = HostingEnvironment.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".PDF";

            using (ContractContext db = new ContractContext())
            {
                var cr = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectID == id);
                if (cr == null)
                {
                    cr = NewCrForm(id);
                    db.ClearanceRequestForms.Add(cr);
                    db.SaveChanges();
                }

                PdfReader pdfReader = new PdfReader(template);
                PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(filePath, FileMode.Create));
                AcroFields fields = pdfStamper.AcroFields;

                fields.SetField("DATE", cr.Date.ToShortDateString());
                fields.SetField("DCO ASSIGNED TO PROJECT", cr.DCO);

                fields.SetField("DEPARTMENT", cr.Department);
                fields.SetField("PROJECT NUMBER", cr.JOC);

                fields.SetField("PROJECT NAME", cr.ProjectName);
                fields.SetField("CONTRACTOR NAME", cr.ContractorName);

                if (cr.StartDate != null)
                {
                    DateTime dt = (DateTime)cr.StartDate;
                    fields.SetField("START DATE", dt.ToShortDateString());
                }

                if (cr.EndDate != null)
                {
                    DateTime dt = (DateTime)cr.EndDate;
                    fields.SetField("COMPLETION DATE", dt.ToShortDateString());
                }

                fields.SetField("NAME", cr.RequestedBy);
                fields.SetField("TITLE", cr.Title);
                fields.SetField("EMAIL", cr.Email);

                if (cr.IsCleared == 1)
                {
                    fields.SetField("Yes", "X");
                }
                else if (cr.IsCleared == 2)
                {
                    fields.SetField("No", "X");
                }

                fields.SetField("COMMENT", cr.Comments);
                fields.SetField("DCO NAME", cr.DcoName);
                if (cr.DateClearedByDCO != null)
                {
                    DateTime dt = (DateTime)cr.DateClearedByDCO;
                    fields.SetField("DATE2", dt.ToShortDateString());
                }

                if (cr.SmDate != null)
                {
                    DateTime dt = (DateTime)cr.SmDate;
                    fields.SetField("SM DATE", dt.ToShortDateString());
                }

                pdfStamper.Close();
                pdfReader.Close();
            }

            return filePath;
        }

        public static string CreateSI(int id)
        {
            string path = "~/Files/Site_Inspection/Forms/SiteInspection/";
            string serverPath = HostingEnvironment.MapPath(path);
            string template = "Template/SiteInspection.pdf";
            string fileName = "SiteInspection_" + id.ToString() + ".pdf";
            string filePath = serverPath + fileName;

            using (ContractContext db = new ContractContext())
            {
                var si = db.SiteInspections.FirstOrDefault(x => x.InspectionID == id);
                if (si == null)
                {
                    try
                    {
                        si = NewSiForm(id);
                        db.SiteInspections.Add(si);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {

                    }
                }

                Project project = (from p in db.Projects
                                   join i in db.Inspections on p.ProjectID equals i.ProjectID
                                   where i.InspectionID == id
                                   select p).First();
                Contractor contractor = db.Contractors.FirstOrDefault(x => x.ContractorID == project.PrimeContractorID);
                var dco = si.DCO;
                Inspection inspection = db.Inspections.FirstOrDefault(x => x.InspectionID == id);

                string documentName = "Site Inspection";

                PdfReader pdfReader = new PdfReader(serverPath + template);
                PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(serverPath + fileName, FileMode.Create));
                AcroFields fields = pdfStamper.AcroFields;


                fields.SetField("Project Number", si.ProjectNumber);
                fields.SetField("Project Name", si.ProjectDescription);

                fields.SetField("Company Name", si.Contractor);
                fields.SetField("Site Address", si.ProjectAddress);

                fields.SetField("Date of Visit", si.DateOfVisit);

                if (project.StartDate != null)
                    fields.SetField("Start Date", ((DateTime)project.StartDate).ToShortDateString());

                if (project.EndDate != null)
                    fields.SetField("End Date", ((DateTime)project.EndDate).ToShortDateString());

                DateTime dateOfVisit;
                if (DateTime.TryParse(si.DateOfVisit, out dateOfVisit))
                {
                    var previousInspections = db.Inspections.Where(x => x.ProjectID == project.ProjectID
                        && x.ContractorID == contractor.ContractorID
                        && x.Address == inspection.Address && x.DateOfVisit < dateOfVisit).ToList();
                    if (previousInspections.Count > 0)
                    {
                        var pi = previousInspections.OrderByDescending(x => x.DateOfVisit).First();
                        fields.SetField("Date of Previous Site Visit", ((DateTime)pi.DateOfVisit).ToShortDateString());
                    }
                }

                if (inspection.DateApproved != null)
                {
                    fields.SetField("CCCS Manager initials", "DS");
                }

                pdfStamper.Close();
                pdfReader.Close();

                Document notification = db.Documents.Where(x => x.InspectionID == id && x.DocumentName == documentName).FirstOrDefault();

                if (notification == null)
                {
                    db.Documents.Add(new Document
                    {
                        ProjectID = project.ProjectID,
                        ContractorID = inspection.ContractorID,
                        InspectionID = id,
                        Electronic = true,
                        DocumentName = documentName,
                        FileName = fileName,
                        Path = path,
                        DateUploaded = DateTime.Now
                    });
                }

                db.SaveChanges();
            }

            return filePath;
        }

        public static string CreateSVC(int id)
        {
            string template = HostingEnvironment.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/Template/SiteVisitCompletion.pdf");
            string filePath = HostingEnvironment.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/") + "SiteVisitCompletion_" + id.ToString() + ".PDF";

            using (ContractContext db = new ContractContext())
            {
                var svc = db.SiteVisitCompletions.FirstOrDefault(x => x.InspectionID == id);
                if (svc == null)
                {
                    try
                    {
                        svc = NewSvcForm(id);
                        db.SiteVisitCompletions.Add(svc);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {

                    }
                }

                PdfReader pdfReader = new PdfReader(template);
                PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(filePath, FileMode.Create));
                AcroFields fields = pdfStamper.AcroFields;

                fields.SetField("Department", svc.Department);

                fields.SetField("Todays Date", svc.TodaysDate);
                fields.SetField("Department Contact", svc.DepartmentContact);

                fields.SetField("Project Number", svc.ProjectNumber);
                fields.SetField("Project Description", svc.ProjectDescription);

                fields.SetField("Project Address", svc.ProjectAddress);
                fields.SetField("Contractor", svc.Contractor);

                fields.SetField("DCO", svc.DCO);
                fields.SetField("Contact Info", svc.DcoContactInfo);

                fields.SetField("Site Visit Date", svc.DateOfVisit);

                if (svc.NoViolations)
                {
                    fields.SetField("chkViolations", "X");
                }
                else
                {
                    if (svc.EeoPostings)
                    {
                        fields.SetField("EEO Postings", "X");
                    }
                    if (svc.Graffiti)
                    {
                        fields.SetField("Graffiti", "X");
                    }
                    if (svc.SegregatedFacilities)
                    {
                        fields.SetField("Segregated Facilities", "X");
                    }
                    if (svc.Referrals)
                    {
                        fields.SetField("Referrals to OFCCADFEHDHR", "X");
                    }
                }

                pdfStamper.Close();
                pdfReader.Close();
            }

            return filePath;
        }

        public static ClearanceRequestForm NewCrForm(int id, string requestedBy = "")
        {
            using (ContractContext db = new ContractContext())
            {
                var p = db.Projects.FirstOrDefault(x => x.ProjectID == id);
                var c = db.Contractors.Find(p.PrimeContractorID);

                ClearanceRequestForm form = new ClearanceRequestForm
                {
                    ProjectID = id,
                    Date = DateTime.Today,
                    JOC = p.JOC,
                    ProjectName = p.ProjectName,
                    ContractorName = c.CompanyName,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    RequestedBy = requestedBy
                };

                // If DCO is not assigned to the project, it throws an exception.
                var u = db.UserProfiles.FirstOrDefault(x => x.UserInitial == p.DCO);
                if (u != null)
                {
                    form.DCO = u.FullName;
                }

                var d = db.Departments.Find(p.DepartmentID);
                if (d != null)
                {
                    form.Department = d.DepartmentName;
                }

                return form;
            }
        }

        public static string GetCrComment(int id)
        {
            string result = "This is to inform you that {0} has met the requirement for Equal Employment Opportunity (EEO) clearance for project number {1}.";

            using (ContractContext db = new ContractContext())
            {
                Project p = db.Projects.Find(id);
                Contractor c = db.Contractors.Find(p.PrimeContractorID);
                result = String.Format(result, c.CompanyName, p.JOC);

                return result;
            }
        }

        public static SiteVisitCompletion NewSvcForm(int id, string requestedBy = "")
        {
            using (ContractContext db = new ContractContext())
            {
                var i = db.Inspections.FirstOrDefault(x => x.InspectionID == id);
                var p = db.Projects.FirstOrDefault(x => x.ProjectID == i.ProjectID);
                var c = db.Contractors.Find(p.PrimeContractorID);
                var u = db.UserProfiles.FirstOrDefault(x => x.UserInitial == p.DCO);

                SiteVisitCompletion form = new SiteVisitCompletion
                {
                    InspectionID = id,
                    ProjectNumber = p.JOC,
                    ProjectDescription = p.ProjectName,
                    ProjectAddress = p.Address + " " + p.City + ", " + p.Zip,
                    Contractor = c.CompanyName,
                    DCO = u.FullName,
                    NoViolations = !i.Violations
                };

                var d = db.Departments.Find(p.DepartmentID);
                if (d != null)
                {
                    form.Department = d.DepartmentName;
                }

                return form;
            }
        }

        public static SiteInspection NewSiForm(int id, string requestedBy = "")
        {
            using (ContractContext db = new ContractContext())
            {
                var i = db.Inspections.FirstOrDefault(x => x.InspectionID == id);
                var p = db.Projects.FirstOrDefault(x => x.ProjectID == i.ProjectID);
                var c = db.Contractors.Find(p.PrimeContractorID);
                var u = db.UserProfiles.FirstOrDefault(x => x.UserInitial == p.DCO);

                SiteInspection form = new SiteInspection
                {
                    DateOfVisit = ((DateTime)i.DateOfVisit).ToShortDateString(),
                    InspectionID = id,
                    ProjectNumber = p.JOC,
                    ProjectDescription = p.ProjectName,
                    ProjectAddress = p.Address + " " + p.City + ", " + p.Zip,
                    Contractor = c.CompanyName,
                    DCO = u.FullName
                };

                return form;
            }
        }

        public static bool IsProjectApproved(int id)
        {
            using (ContractContext db = new ContractContext())
            {
                var log = (from cr in db.ClearanceRequests
                           join cl in db.ClearanceRequestLogs on cr.ID equals cl.ClearanceRequestID
                           where cr.ProjectID == id && cl.Activity.Contains("Approved by Manager")
                           select cl).FirstOrDefault();

                return (log != null);
            }
        }

        public static List<SelectListItem> GetUsers()
        {
            List<SelectListItem> list = new List<SelectListItem>();

            using (ContractContext db = new ContractContext())
            {
                foreach (var u in db.UserProfiles.Where(x => x.IsActive).OrderBy(x => x.UserInitial).ToList())
                {
                    string txt = u.FullName + " (" + u.UserInitial + ")";
                    list.Add(new SelectListItem { Text = txt, Value = u.UserID });
                }

                list.Insert(0, new SelectListItem { Text = "- Select a user -", Value = "" });
            }

            return list;
        }

        public static string GetMonthName(int month)
        {
            switch (month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
                default:
                    return "";
            }
        }

        public static IPagedList<ProjectModel> GetExtraInfo(IPagedList<ProjectModel> model)
        {
            using (ContractContext db = new ContractContext())
            {
                foreach (var m in model)
                {
                    if (m.StartDate != null && m.EndDate != null)
                    {
                        int percent;
                        if (m.EndDate <= DateTime.Today)
                        {
                            percent = 100;
                        }
                        else
                        {
                            double max = ((DateTime)m.EndDate - (DateTime)m.StartDate).TotalDays;
                            double now = (DateTime.Today - (DateTime)m.StartDate).TotalDays;
                            percent = (int)Math.Round(now / max * 100);
                        }
                        m.ProgressPercent = percent;
                    }
                    m.NumberSubs = db.Contracts.Where(x => x.ProjectID == m.ProjectID && x.SubTo != 0).Count();

                    var siteVisits = db.Inspections.Where(x => x.ProjectID == m.ProjectID).ToList();
                    if (siteVisits.Count > 0)
                    {
                        m.DateLastSiteVisit = siteVisits.OrderByDescending(x => x.DateOfVisit).First().DateOfVisit;
                    }

                    var documentFiles = db.DocumentFiles.Where(x => x.ProjectID == m.ProjectID).ToList();
                    if (documentFiles.Count > 0)
                    {
                        m.DateLastDocumentReceived = DateTime.Parse(documentFiles.OrderByDescending(x => x.DateReceived).First().DateReceived);
                    }

                    var logs = db.ActivityLogs.Where(x => x.ProjectID == m.ProjectID).ToList();
                    if (logs.Count > 0)
                    {
                        m.DateLastUpdate = logs.OrderByDescending(x => x.ActivityDate).First().ActivityDate;
                    }

                    var clearanceRequest = db.ClearanceRequests.FirstOrDefault(x => x.ProjectID == m.ProjectID);
                    if (clearanceRequest != null)
                    {
                        m.ClearanceRequestStatus = clearanceRequest.CurrentStatus;

                        m.ClearanceRequestStatusDate = (clearanceRequest.DateModified == null)
                             ? clearanceRequest.DateRequested : (DateTime)clearanceRequest.DateModified;
                    }

                    ProjectContact projectContact = db.ProjectContacts.FirstOrDefault(x => x.ProjectID == m.ProjectID);
                    if (projectContact != null)
                    {
                        m.Analyst = projectContact.Analyst;
                        m.AnalystPhoneNumber = projectContact.AnalystPhoneNumber;
                        m.ProjectManager = projectContact.ProjectManager;
                        m.ProjectManagerPhoneNumber = projectContact.ProjectManagerPhoneNumber;
                    }

                    m.Comments = db.Comments.Where(x => x.ProjectID == m.ProjectID).ToList();
                    m.NumberMissingDocuments = GetMissingDocumentCount(m.ProjectID);
                }

                return model;
            }
        }

        public static int GetMissingDocumentCount(int id)
        {
            using (ContractContext db = new ContractContext())
            {
                var project = db.Projects.Find(id);
                if (project == null)
                    return -1;

                var query = from c1 in db.Contractors
                            join ct in db.Contracts on c1.ContractorID equals ct.ContractorID
                            where ct.ProjectID == id
                            select new ContractorListModel
                            {
                                CompanyName = c1.CompanyName,
                                ContractID = ct.ContractID,
                                ContractorID = ct.ContractorID,
                                StartDate = ct.StartDate,
                                EndDate = ct.EndDate
                            };

                var list = query.ToList();

                int cnt = 0;
                foreach (var l in list)
                {
                    DateTime startDate = (l.StartDate == null) ? DateTime.Today : (DateTime)l.StartDate;
                    DateTime endDate = (l.EndDate == null) ? DateTime.Parse("12/31/" + DateTime.Today.Year) : (DateTime)l.EndDate;
                    int startYear = startDate.Year;
                    int endYear = endDate.Year;

                    var list1 = db.DocumentFiles.Where(x => x.ContractorID == l.ContractorID
                            && x.DocumentType != "ListSub" && x.DocumentType != "NtceEEO" && x.Year >= startYear);
                    var list2 = db.DocumentFiles.Where(x => x.ContractorID == l.ContractorID && x.ProjectID == id
                            && (x.DocumentType == "ListSub" || x.DocumentType == "NtceEEO") && x.Year >= startYear);
                    var files = list1.Union(list2).OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

                    if (startYear == endYear)
                    {
                        if (endDate.Month >= 3 && endDate.Day >= 10)
                        {
                            if (project.FederalFunds)
                            {
                                var prevPerf = files.FirstOrDefault(x => x.DocumentType == "PrevPerf" && x.Year == endYear);
                                if (prevPerf == null)
                                    cnt++;
                            }

                            var nonSeg = files.FirstOrDefault(x => x.DocumentType == "NonSeg" && x.Year == endYear);
                            if (nonSeg == null)
                                cnt++;

                            var gfe = files.FirstOrDefault(x => x.DocumentType == "GFE" && x.Year == endYear);
                            if (gfe == null)
                                cnt++;

                            var listSub = files.FirstOrDefault(x => x.DocumentType == "ListSub" && x.Year == endYear && x.ProjectID == id);
                            if (listSub == null)
                                cnt++;

                            var ntceEEO = files.FirstOrDefault(x => x.DocumentType == "NtceEEO" && x.Year == endYear && x.ProjectID == id);
                            if (ntceEEO == null)
                                cnt++;

                            var wiba = files.FirstOrDefault(x => x.DocumentType == "WIBA" && x.Year == endYear);
                            if (wiba == null)
                                cnt++;

                            int eur;
                            int missingEur;

                            if (startDate.Month >= 3)
                            {
                                eur = files.Where(x => x.DocumentType == "EUR" && x.Year == endYear
                                                        && x.Month >= 3).Count();

                                if (endDate.Month <= 8)
                                    missingEur = 0;
                                else
                                    missingEur = 1 - eur;
                            }
                            else
                            {
                                eur = files.Where(x => x.DocumentType == "EUR" && x.Year == endYear).Count();

                                if (endDate.Month <= 8)
                                    missingEur = 1 - eur;
                                else
                                    missingEur = 2 - eur;
                            }

                            cnt = (missingEur < 0) ? cnt : cnt + missingEur;
                        }
                    }
                    else
                    {
                        for (int i = startYear; i <= endYear; i++)
                        {
                            if (i < endYear || (i == endYear && endDate.Month >= 3 && endDate.Day >= 10))
                            {
                                if (project.FederalFunds)
                                {
                                    var prevPerf = files.FirstOrDefault(x => x.DocumentType == "PrevPerf" && x.Year == i);
                                    if (prevPerf == null)
                                        cnt++;
                                }

                                var nonSeg = files.FirstOrDefault(x => x.DocumentType == "NonSeg" && x.Year == i);
                                if (nonSeg == null)
                                    cnt++;

                                var gfe = files.FirstOrDefault(x => x.DocumentType == "GFE" && x.Year == i);
                                if (gfe == null)
                                    cnt++;

                                var listSub = files.FirstOrDefault(x => x.DocumentType == "ListSub" && x.Year == i && x.ProjectID == id);
                                if (listSub == null)
                                    cnt++;

                                var ntceEEO = files.FirstOrDefault(x => x.DocumentType == "NtceEEO" && x.Year == i && x.ProjectID == id);
                                if (ntceEEO == null)
                                    cnt++;

                                var wiba = files.FirstOrDefault(x => x.DocumentType == "WIBA" && x.Year == i);
                                if (wiba == null)
                                    cnt++;

                                int eur;
                                int missingEur;
                                if (i == startYear)
                                {
                                    if (startDate.Month < 3 || (startDate.Month == 3 && startDate.Day < 10))
                                    {
                                        eur = files.Where(x => x.DocumentType == "EUR" && x.Year == i).Count();
                                        missingEur = 2 - eur;
                                    }
                                    else
                                    {
                                        eur = files.Where(x => x.DocumentType == "EUR" && x.Year == i
                                                            && x.Month >= 3).Count();

                                        if (startDate.Month <= 8)
                                            missingEur = 1 - eur;
                                        else
                                            missingEur = 0;
                                    }
                                }
                                else if (i == endYear)
                                {
                                    if (endDate.Month < 3 || (endDate.Month == 3 && endDate.Day < 10))
                                        missingEur = 0;
                                    else
                                    {
                                        eur = files.Where(x => x.DocumentType == "EUR" && x.Year == i
                                                && x.Month >= 3).Count();

                                        if (endDate.Month <= 8)
                                        {
                                            missingEur = 1 - eur;
                                        }
                                        else
                                        {
                                            missingEur = 2 - eur;
                                        }
                                    }
                                }
                                else
                                {
                                    eur = files.Where(x => x.DocumentType == "EUR" && x.Year == i).Count();

                                    missingEur = 2 - eur;
                                }

                                cnt = (missingEur < 0) ? cnt : cnt + missingEur;
                            }
                        }
                    }
                }

                return cnt;
            }
        }

        public static DateTime GetLastDocumentDate(int id)
        {
            using (ContractContext db = new ContractContext())
            {
                var query = from c1 in db.Contractors
                            join ct in db.Contracts on c1.ContractorID equals ct.ContractorID
                            where ct.ProjectID == id
                            select new ContractorListModel
                            {
                                ContractID = ct.ContractID,
                                ContractorID = ct.ContractorID,
                                StartDate = ct.StartDate,
                                EndDate = ct.EndDate
                            };

                var list = query.ToList();

                DateTime dt = DateTime.MinValue;
                foreach (var l in list)
                {
                    DateTime dt1 = (l.StartDate == null) ? DateTime.Today : (DateTime)l.StartDate;
                    DateTime dt2 = (l.EndDate == null) ? DateTime.Parse("12/31/" + DateTime.Today.Year) : (DateTime)l.EndDate;
                    int startYear = dt1.Year;
                    DateTime startDate = dt1;
                    int endYear = dt2.Year;
                    DateTime endDate = dt2;

                    var list1 = db.DocumentFiles.Where(x => x.ContractorID == l.ContractorID
                            && x.DocumentType != "ListSub" && x.DocumentType != "NtceEEO");
                    var list2 = db.DocumentFiles.Where(x => x.ContractorID == l.ContractorID && x.ProjectID == id
                            && (x.DocumentType == "ListSub" || x.DocumentType == "NtceEEO"));
                    var files = list1.Union(list2).OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

                    foreach (var file in files)
                    {
                        if (file.DateApproved > dt)
                            dt = file.DateApproved.Value;
                    }
                }

                return dt;
            }
        }

        public static int GetCrExceptionsCount()
        {
            return GetCrExceptions().Count;
        }

        public static List<ClearanceRequestExceptionListModel> GetCrExceptions(bool refreshCache = false)
        {
            string key = CR_EXCEPTION_LIST_KEY;
            List<ClearanceRequestExceptionListModel> model = new List<ClearanceRequestExceptionListModel>();

            if (!CacheHelper.Exists(key) || refreshCache)
            {
                using (ContractContext db = new ContractContext())
                {
                    var list = (from cr in db.ClearanceRequests
                                join p in db.Projects on cr.ProjectID equals p.ProjectID
                                where cr.CurrentStatus.Contains("Department") && p.DateClosed != null
                                select cr).ToList();
                    list = (from l in list
                            where ((DateTime)l.DateModified - l.DateRequested).TotalDays > 7
                            select l).ToList();

                    foreach (var cr in list)
                    {
                        DateTime lastDocDate = CommonHelper.GetLastDocumentDate(cr.ProjectID);
                        int totalDays = CommonHelper.GetBusinessDays(lastDocDate, (DateTime)cr.DateModified);

                        if (totalDays > 7)
                        {
                            var logs = db.ClearanceRequestLogs.Where(x => x.ClearanceRequestID == cr.ID).ToList();

                            DateTime dcoDate = logs.FirstOrDefault(x => x.Activity.Contains("to DCO")).Date;
                            DateTime startDate = (lastDocDate > dcoDate) ? lastDocDate : dcoDate;

                            DateTime smDate = logs.Where(x => x.Activity.Contains("to Manager")).Max(x => x.Date);
                            DateTime approvedDate = logs.FirstOrDefault(x => x.Activity.Contains("Approved")).Date;
                            DateTime deptDate = logs.FirstOrDefault(x => x.Activity.Contains("Department")).Date;

                            int dcoDays = CommonHelper.GetBusinessDays(startDate, smDate);
                            int smDays = CommonHelper.GetBusinessDays(smDate, approvedDate);
                            int deptDays = CommonHelper.GetBusinessDays(approvedDate, deptDate);

                            if (dcoDays > 5 || smDays > 2 || deptDays > 0)
                            {
                                var project = (from p in db.Projects
                                               join c in db.Contractors on p.PrimeContractorID equals c.ContractorID
                                               where p.ProjectID == cr.ProjectID
                                               select new
                                               {
                                                   DCO = p.DCO,
                                                   JOC = p.JOC,
                                                   ProjectName = p.ProjectName,
                                                   PrimeContractorId = p.PrimeContractorID,
                                                   PrimeContractor = c.CompanyName
                                               }).FirstOrDefault();

                                var comments = db.ClearanceRequestExceptions.Where(x => x.ClearanceRequestID == cr.ID).ToList();

                                ClearanceRequestExceptionListModel m = new ClearanceRequestExceptionListModel
                                {
                                    ClearanceRequestID = cr.ID,
                                    DCO = project.DCO,
                                    DateLastDoc = lastDocDate,
                                    DateRequested = cr.DateRequested,
                                    ProjectID = cr.ProjectID,
                                    JOC = project.JOC,
                                    ProjectName = project.ProjectName,
                                    PrimeContractorId = project.PrimeContractorId,
                                    PrimeContractor = project.PrimeContractor,
                                    DaysDCO = dcoDays,
                                    DaysSM = smDays,
                                    DaysDEPT = deptDays,
                                    Comments = comments
                                };

                                model.Add(m);
                            }
                        }
                    }

                    CacheHelper.Add<List<ClearanceRequestExceptionListModel>>(model, key, 60.0);
                }
            }
            else
            {
                CacheHelper.Get<List<ClearanceRequestExceptionListModel>>(key, out model);
            }

            return model;
        }

        public static int GetBusinessDays(DateTime from, DateTime to)
        {
            int days = (to - from).Days;

            for (DateTime dt = from; dt <= to; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    days--;
                }
            }

            return days;
        }
    }
}
