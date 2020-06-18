using CCCS.Web.Models;
using System.Web.Mvc;
using System.Linq;
using iTextSharp.text.pdf;
using System;
using System.Data.Entity;
using System.IO;
using System.Collections.Generic;
using System.Web;
using System.Net.Mail;
using System.Data;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using CCCS.Infrastructure;
using System.Drawing;
using OfficeOpenXml.Style;
using CCCS.Core.Domain.Documents;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Projects;
using CCCS.Core.Domain.Inspection;
using CCCS.Core.Domain.Notices;
using CCCS.Core.Domain.ClearanceRequests;
using CCCS.Web.AdService;
using CCCS.Web.Models.Documents;
using CCCS.Web.Models.Notices;
using CCCS.Core.Domain.Users;
using CCCS.Core.Data;

namespace CCCS.Controllers
{
    [Authorize]
    public class NoticeController : BaseController
    {
        const string EMAIL_CCCS_EEO = "CCCS_EEO@isd.lacounty.gov";

        private readonly IRepository<DocumentFile> _documentFileRepository;
        private readonly IRepository<Project> _projectRepository;
        private readonly IRepository<PublicProfile> _publicProfileRepository;

        public NoticeController(
            IRepository<DocumentFile> documentFileRepository,
            IRepository<Project> projectRepository,
            IRepository<PublicProfile> publicProfileRepository)
        {
            this._documentFileRepository = documentFileRepository;
            this._projectRepository = projectRepository;
            this._publicProfileRepository = publicProfileRepository;
        }

        #region Service Request
        // GET: Notice
        public ActionResult ServiceRequest(int id, string returnUrl)
        {
            Document model = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == "SR");
            ViewBag.ProjectId = id;

            if (model == null)
            {
                CreateServiceRequest(id);
            }

            return Redirect("~/Files/Service_Request/ServiceRequest_" + id.ToString() + ".pdf?id=" + id.ToString() + "&returnUrl=" + returnUrl);
        }

        private void CreateServiceRequest(int id)
        {
            string path = Server.MapPath("~/Files/Service_Request/");
            string template = "Template/ServiceRequest.pdf";
            string fileName = "ServiceRequest_" + id.ToString() + ".pdf";

            string documentName = "SR";

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            fields.SetField("REQUEST DATE", DateTime.Today.ToShortDateString());
            fields.SetField("chkNew", "Yes");

            int from = (DateTime.Today.Month < 7) ? DateTime.Today.Year - 1 : DateTime.Today.Year;
            int to = (DateTime.Today.Month < 7) ? DateTime.Today.Year : DateTime.Today.Year + 1;
            fields.SetField("FISCAL YEAR FROM", from.ToString());
            fields.SetField("FISCAL YEAR TO", to.ToString());

            string name = db.Settings.FirstOrDefault(x => x.Key == "Manager.Name").Value;
            string phoneNumber = db.Settings.FirstOrDefault(x => x.Key == "Manager.PhoneNumber").Value;
            string email = db.Settings.FirstOrDefault(x => x.Key == "Manager.Email").Value;
            fields.SetField("CONTACT NAME", name);
            fields.SetField("CONTACT PHONE NUMBER", phoneNumber);
            fields.SetField("CONTACT EMAIL", email);

            string name1 = db.Settings.FirstOrDefault(x => x.Key == "Notice.FiscalContact.Name").Value;
            string phoneNumber1 = db.Settings.FirstOrDefault(x => x.Key == "Notice.FiscalContact.PhoneNumber").Value;
            string email1 = db.Settings.FirstOrDefault(x => x.Key == "Notice.FiscalContact.Email").Value;
            fields.SetField("FISCAL CONTACT NAME", name1);
            fields.SetField("FISCAL CONTACT PHONE NUMBER", phoneNumber1);
            fields.SetField("FISCAL CONTACT EMAIL", email1);

            pdfStamper.Close();
            pdfReader.Close();

            db.Documents.Add(new Document
            {
                ProjectID = id,
                Electronic = true,
                DocumentName = documentName,
                FileName = fileName,
                Path = path,
                DateUploaded = DateTime.Now
            });

            db.SaveChanges();
        }

        public ActionResult CloseSR()
        {
            string returnUrl = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["returnUrl"];
            int id = int.Parse(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["id"]);

            returnUrl = (string.IsNullOrEmpty(returnUrl)) ? "~/" : returnUrl;

            //Workaround for PDF bug
            returnUrl = FixReturnUrl(returnUrl, id);

            string path = Server.MapPath("~/Files/Service_Request/");
            string fileName = "ServiceRequest_" + id + ".PDF";

            try
            {
                Request.SaveAs(path + fileName, false);
                // Remove buttons in the original
                string[] args = { "btnClose" };
                string copyPath = GetEmailCopy(path + fileName, args);
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was error saving 'Service Request'.  Please try at a later time.";
            }

            return Redirect(returnUrl);
        }

        public ActionResult EmailSR(int id, string message = "")
        {
            var project = db.Projects.Find(id);
            string deptId = project.DepartmentID;
            string joc = project.JOC;

            EmailModel model = new EmailModel
            {
                Action = "SendSR",
                Title = (String.IsNullOrEmpty(joc)) ? "Service Request" : "Service Request: " + joc,
                ProjectID = id,
                To = db.Settings.FirstOrDefault(x => x.Key == "Notice.FiscalContact.Email").Value,
                Cc = db.Settings.FirstOrDefault(x => x.Key == "Manager.Email").Value,
                ReturnUrl = Request.UrlReferrer.PathAndQuery,
                Recipients = GetEmailRecipients(deptId, true, true, true),
                AttachmentRequired = true
            };

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendSR(FormCollection form)
        {
            int id = int.Parse(form["ProjectID"]);
            string dstPath = Server.MapPath("~/Files/Service_Request/");
            string filePath = dstPath + "ServiceRequest_" + id.ToString() + "e.pdf";

            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            SendEmail(to, cc, subject, body, filePath, "Service Request");

            return RedirectToAction("Details2", "Project", new { id = id });
        }

        #endregion


        #region "Contractor Notification"

        public ActionResult ContractorNotification(int id, string returnUrl)
        {
            Document model = db.Documents.FirstOrDefault(x => x.InspectionID == id && x.DocumentName == "Contractor Notification");
            ViewBag.InspectionId = id;

            if (model == null)
            {
                CreateContractorNotification(id);
            }

            return Redirect("~/Files/Site_Inspection/Forms/ContractorNotification/ContractorNotification_" + id.ToString() + ".pdf?id=" + id.ToString() + "&returnUrl=" + returnUrl);
        }

        private void CreateContractorNotification(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/ContractorNotification/");
            string template = "Template/ContractorNotification.pdf";
            string fileName = "ContractorNotification_" + id.ToString() + ".pdf";

            Project project = (from p in db.Projects
                               join i in db.Inspections on p.Id equals i.ProjectId
                               where i.Id == id
                               select p).First();
            Contractor contractor = db.Contractors.FirstOrDefault(x => x.Id == project.PrimeContractorID);
            Inspection inspection = db.Inspections.Find(id);
            var contactInfos = db.ContractorContacts.Where(x => x.ContractorId == id).ToList();

            var dco = UserManager.Users.FirstOrDefault(x => x.UserName == User.Identity.Name);

            string documentName = "Contractor Notification";

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            if (contactInfos.Count > 0)
            {
                fields.SetField("Contractor Representative", contactInfos[0].Name);
            }

            fields.SetField("Project Number", project.JOC);
            fields.SetField("Project Description", project.ProjectName);

            string dateVisit = DateTime.Parse(inspection.DateOfVisit.ToString()).ToShortDateString();
            fields.SetField("Date of Site Visit", dateVisit);
            fields.SetField("Project Location", inspection.Address + ",  " + inspection.City);

            fields.SetField("DCO Name", dco.UserName);
            string contactInfo = dco.Email + "; " + dco.PhoneNumber;
            fields.SetField("DCO Contact Information", contactInfo);

            fields.SetField("chkSiteInspection", "X");

            pdfStamper.Close();
            pdfReader.Close();

            Document notification = db.Documents.Where(x => x.InspectionID == id && x.DocumentName == documentName).FirstOrDefault();

            if (notification == null)
            {
                db.Documents.Add(new Document
                {
                    ProjectID = project.Id,
                    ContractorID = inspection.ContractorId,
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

        public ActionResult CloseCN()
        {
            int id = int.Parse(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["id"]);

            try
            {
                string path = Server.MapPath("~/Files/Site_Inspection/Forms/ContractorNotification/");
                string fileName = "ContractorNotification_" + id + ".pdf";

                Request.SaveAs(path + fileName, false);
                // Remove buttons in the original
                string[] args = { "btnClose" };
                string copyPath = GetEmailCopy(path + fileName, args);
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Details", "Inspection", new { id = id });
        }

        public ActionResult EmailCN(int id, string message = "")
        {
            var project = (from ins in db.Inspections
                           join p in db.Projects on ins.ProjectId equals p.Id
                           where ins.Id == id
                           select p).FirstOrDefault();
            string deptId = project.DepartmentID;
            string joc = project.JOC;

            EmailModel model = new EmailModel
            {
                Action = "SendCN",
                Title = (String.IsNullOrEmpty(joc)) ? "Contractor Notification" : "Contractor Notification: " + joc,
                InspectionID = id,
                To = GetContractorEmails(id),
                Cc = GetEmailRecipients(true, true),
                ReturnUrl = Request.UrlReferrer.PathAndQuery,
                Recipients = GetEmailRecipients(deptId, true, true, true),
                AttachmentRequired = true
            };

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendCN(FormCollection form)
        {
            int id = int.Parse(form["InspectionID"]);
            string dstPath = Server.MapPath("~/Files/Site_Inspection/Forms/ContractorNotification/");
            string filePath = dstPath + "ContractorNotification_" + id.ToString() + ".PDF";

            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            try
            {
                if (to.Count == 0)
                {
                    TempData["Message"] = "Error: Please enter at least one mail recipient.";

                    return RedirectToAction("EmailCN", "Notice", new { id = id });
                }

                SendEmail(to, cc, subject, body, filePath, "Contractor Notification");

                string activity = "CN email notification";
                DateTime now = DateTime.Now;
                var inspection = db.Inspections.Find(id);
                inspection.DateContractorNotification = now;
                inspection.DateLastUpdated = now;
                inspection.Status = activity;
                db.Entry(inspection).State = EntityState.Modified;

                InspectionLog log = new InspectionLog
                {
                    InspectionID = id,
                    Date = now,
                    Activity = activity
                };
                db.InspectionLogs.Add(log);

                db.SaveChanges();

                TempData["Message"] = "'Contractor Notification' was sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error while sending 'Contractor Notification'.";
            }

            return RedirectToAction("Details", "Inspection", new { id = id });
        }

        #endregion


        #region Site Inspection

        public ActionResult CloseSI()
        {
            int id = int.Parse(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["id"]);
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/SiteInspection/");
            string fileName = "SiteVisitCompletion_" + id + ".pdf";
            string fullPath = path + fileName;

            Request.SaveAs(fullPath, false);

            return RedirectToAction("Details", "Inspection", new { id = id });
        }



        #endregion


        #region Site Visit Completion

        public ActionResult SiteVisitCompletion(int id, string returnUrl)
        {
            Document model = db.Documents.FirstOrDefault(x => x.InspectionID == id && x.DocumentName == "Site Visit Completion");
            ViewBag.InspectionId = id;

            if (model == null)
            {
                CreateSiteVisitCompletion(id);
            }

            return Redirect("~/Files/Site_Inspection/Forms/SiteVisitCompletion/SiteVisitCompletion_" + id.ToString() + ".pdf?id=" + id.ToString() + "&returnUrl=" + returnUrl);
        }

        public void CreateSiteVisitCompletion(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/");
            string template = "Template/SiteVisitCompletion.pdf";
            string fileName = "SiteVisitCompletion_" + id.ToString() + ".pdf";

            Project project = (from p in db.Projects
                               join i in db.Inspections on p.Id equals i.ProjectId
                               where i.Id == id
                               select p).First();
            Contractor contractor = db.Contractors.FirstOrDefault(x => x.Id == project.PrimeContractorID);
            var dco = UserManager.Users.FirstOrDefault(x => x.UserName == User.Identity.Name);
            Inspection inspection = db.Inspections.FirstOrDefault(x => x.Id == id);

            string documentName = "Site Visit Completion";

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            var department = db.Departments.FirstOrDefault(x => x.DepartmentId == project.DepartmentID);
            if (department != null)
            {
                fields.SetField("Department", department.DepartmentName);
            }

            fields.SetField("Todays Date", DateTime.Today.ToShortDateString());
            var dc = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == project.Id);
            if (dc != null)
            {
                fields.SetField("Department Contact", dc.DeptContact + ", " + dc.DeptContactPhoneNumber + " " + dc.DeptContactExtension);
            }

            fields.SetField("Project Number", project.JOC);
            fields.SetField("Project Description", project.ProjectName);

            fields.SetField("Project Address", inspection.Address + ", " + inspection.City);
            fields.SetField("DCO", User.Identity.Name);
            string contactInfo = dco.Email + "; " + dco.PhoneNumber;
            fields.SetField("Contact Info", contactInfo);

            fields.SetField("Site Visit Date", ((DateTime)inspection.DateOfVisit).ToShortDateString());

            if (inspection.Violations)
            {
                fields.SetField("chkViolations", "X");
            }

            pdfStamper.Close();
            pdfReader.Close();

            Document notification = db.Documents.Where(x => x.InspectionID == id && x.DocumentName == documentName).FirstOrDefault();

            if (notification == null)
            {
                db.Documents.Add(new Document
                {
                    ProjectID = project.Id,
                    ContractorID = inspection.ContractorId,
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

        public ActionResult CloseSVC()
        {
            int id = int.Parse(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["id"]);
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/");
            string fileName = "SiteVisitCompletion_" + id + ".pdf";
            string fullPath = path + fileName;

            Request.SaveAs(fullPath, false);

            PdfReader pdfReader = new PdfReader(fullPath);
            AcroFields fields = pdfReader.AcroFields;

            string postings = fields.GetField("EEO Postings");
            string graffiti = fields.GetField("Graffiti");
            string seg = fields.GetField("Segregated Facilities");
            string referral = fields.GetField("Referrals to OFCCADFEHDHR");
            if (postings == "On" || graffiti == "On" || seg == "On" || referral == "On")
            {
                var inspection = db.Inspections.Find(id);
                inspection.Violations = true;
                db.Entry(inspection).State = EntityState.Modified;
                db.SaveChanges();
            }

            pdfReader.Close();

            return RedirectToAction("Details", "Inspection", new { id = id });
        }

        public ActionResult EmailSVC(int id, string message = "")
        {
            var project = (from ins in db.Inspections
                           join p in db.Projects on ins.ProjectId equals p.Id
                           where ins.Id == id
                           select p).FirstOrDefault();
            string deptId = project.DepartmentID;
            string joc = project.JOC;

            EmailModel model = new EmailModel
            {
                Action = "SendSVC",
                Title = (String.IsNullOrEmpty(joc)) ? "Site Visit Completion" : "Site Visit Completion: " + joc,
                InspectionID = id,
                To = GetContractorEmails(id),
                Cc = GetEmailRecipients(true, true),
                ReturnUrl = Request.UrlReferrer.PathAndQuery,
                AttachmentRequired = true,
                Recipients = GetEmailRecipients(deptId, true, true, true),
                ProjectContacts = GetProjectContacts(project.Id)
            };

            DateTime now = DateTime.Now;
            string activity = "SVC email notification";
            var inspection = db.Inspections.Find(id);
            inspection.DateSiteVisitCompletion = now;
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

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendSVC(FormCollection form)
        {
            int id = int.Parse(form["InspectionID"]);
            string dstPath = Server.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/");
            string filePath = dstPath + "SiteVisitCompletion_" + id.ToString() + ".pdf";

            string msg = "";

            try
            {
                List<MailAddress> to = FetchEmails(form["to"]);
                if (to.Count == 0)
                {
                    TempData["Message"] = "Error: Please enter at least one mail recipient.";

                    return RedirectToAction("EmailSVC", "Notice", new { id = id });
                }

                List<MailAddress> cc = FetchEmails(form["Cc"]);
                string subject = form["subject"];
                string body = form["body"];

                SendEmail(to, cc, subject, body, filePath, "Site Visit Completion");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error sending 'Site Visit Completion'.";

                return RedirectToAction("EmailSVC", "Notice", new { id = id, message = msg });
            }

            TempData["Message"] = "'Site Visit Completion' was sent successfully.";

            return RedirectToAction("Details", "Inspection", new { id = id, message = msg });
        }

        #endregion


        #region "Violation Correction"

        public ActionResult ViolationCorrection(int id, string returnUrl)
        {
            Document model = db.Documents.FirstOrDefault(x => x.InspectionID == id && x.DocumentName == "Violation Correction");
            ViewBag.InspectionId = id;

            if (model == null)
            {
                CreateViolationCorrection(id);
            }

            return Redirect("~/Files/Site_Inspection/Forms/ViolationCorrection/ViolationCorrection_" + id.ToString() + ".pdf?id=" + id.ToString() + "&returnUrl=" + returnUrl);
        }

        private void CreateViolationCorrection(int id)
        {
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/ViolationCorrection/");
            string template = "Template/ViolationCorrection.pdf";
            string fileName = "ViolationCorrection_" + id.ToString() + ".pdf";

            string documentName = "Violation Correction";

            PdfReader pdfReader = new PdfReader(path + template);
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(path + fileName, FileMode.Create));
            AcroFields fields = pdfStamper.AcroFields;

            Project project = (from p in db.Projects
                               join i in db.Inspections on p.Id equals i.ProjectId
                               where i.Id == id
                               select p).First();

            var contactInfos = db.ContractorContacts.Where(x => x.ContractorId == id).ToList();
            var dco = UserManager.Users.FirstOrDefault(x => x.UserName == User.Identity.Name);
            Inspection inspection = db.Inspections.Find(id);

            var department = db.Departments.FirstOrDefault(x => x.DepartmentId == project.DepartmentID);
            if (department != null)
            {
                fields.SetField("Department", department.DepartmentName);
            }

            fields.SetField("Todays Date", DateTime.Today.ToShortDateString());

            var dc = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == project.Id);
            if (dc != null)
            {
                fields.SetField("Department Contact", dc.DeptContact + ", " + dc.DeptContactPhoneNumber + " " + dc.DeptContactExtension);
            }

            fields.SetField("Project Number", project.JOC);
            fields.SetField("Project Description", project.ProjectName);

            fields.SetField("Project Address", inspection.Address + ", " + inspection.City);

            string contactInfo = User.Identity.Name + ": " + dco.Email + ", " + dco.PhoneNumber;
            fields.SetField("DCO Contact Info", contactInfo);

            fields.SetField("Date of Visit", ((DateTime)inspection.DateOfVisit).ToShortDateString());

            pdfStamper.Close();
            pdfReader.Close();

            db.Documents.Add(new Document
            {
                ProjectID = project.Id,
                ContractorID = inspection.ContractorId,
                InspectionID = id,
                Electronic = true,
                DocumentName = documentName,
                FileName = fileName,
                Path = path,
                DateUploaded = DateTime.Now
            });

            db.SaveChanges();
        }

        public ActionResult CloseVC()
        {
            int id = int.Parse(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["id"]);
            string path = Server.MapPath("~/Files/Site_Inspection/Forms/ViolationCorrection/");
            string fileName = "ViolationCorrection_" + id + ".pdf";
            string fullPath = path + fileName;

            Request.SaveAs(fullPath, false);

            return RedirectToAction("Details", "Inspection", new { id = id });
        }

        public ActionResult EmailVC(int id, string message = "")
        {
            var project = (from i in db.Inspections
                           join p in db.Projects on i.ProjectId equals p.Id
                           where i.Id == id
                           select p).FirstOrDefault();
            string deptId = project.DepartmentID;
            string joc = project.JOC;

            EmailModel model = new EmailModel
            {
                Action = "SendVC",
                Title = (String.IsNullOrEmpty(joc)) ? "Violation Correction" : "Violation Correction: " + joc,
                InspectionID = id,
                To = GetContractorEmails(id),
                Cc = GetEmailRecipients(true, true),
                ReturnUrl = Request.UrlReferrer.PathAndQuery,
                Recipients = GetEmailRecipients(deptId, true, true, true),
                AttachmentRequired = true
            };

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendVC(FormCollection form)
        {
            int id = int.Parse(form["InspectionID"]);
            string dstPath = Server.MapPath("~/Files/Site_Inspection/Forms/ViolationCorrection/");
            string filePath = dstPath + "ViolationCorrection_" + id.ToString() + ".pdf";

            string msg = "'Violation Correction' was sent successfully.";

            try
            {
                List<MailAddress> to = FetchEmails(form["to"]);
                if (to.Count == 0)
                {
                    msg = "Error: Please enter a mail recipient.";
                    throw new Exception(msg);
                }

                List<MailAddress> cc = FetchEmails(form["Cc"]);
                string subject = form["subject"];
                string body = form["body"];

                bool emailSent = SendEmail(to, cc, subject, body, filePath, "Violation Correction");
                if (!emailSent)
                {
                    throw new Exception();
                }

                DateTime now = DateTime.Now;
                string activity = "VC sent to Department";

                var inspection = db.Inspections.Find(id);
                inspection.DateViolationCorrection = now;
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

                TempData["Message"] = "'Violation Correction' was sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error sending 'Violation Correction'.";

                return RedirectToAction("EmailVC", "Notice", new { id = id, message = msg });
            }

            return RedirectToAction("Details", "Inspection", new { id = id, message = msg });
        }

        #endregion


        #region Departmental Billing

        public ActionResult EmailDB(string month, string dept, string fundOrg = "", string returnUrl = "")
        {
            EmailModel model = new EmailModel
            {
                Action = "SendDB",
                Title = "Department billing",
                Month = month,
                Department = dept,
                FundOrg = fundOrg,
                ReturnUrl = returnUrl,
                AttachmentRequired = true
            };

            var userId = User.Identity.GetUserId();
            model.Cc = UserManager.FindById(userId).Email;
            model.Recipients = GetEmailRecipients(dept, true, true, true);

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendDB(FormCollection form, EmailModel model)
        {
            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            string filePath = GetExcelDeptBilling(model.Month, model.Department, model.FundOrg);

            SendEmail(to, cc, subject, body, filePath, model.Title);

            return RedirectToAction("Index6", "Worksheet", new { month = model.Month, dept = model.Department, fundOrg = model.FundOrg });
        }

        private string GetExcelDeptBilling(string month, string dept, string fundOrg)
        {
            string path = Server.MapPath("~/Files/DepartmentalBilling/");
            DirectoryInfo outputDir = new DirectoryInfo(path);
            string fullPath = string.Concat(path, month.Replace("/", ""), "_", dept, "_", fundOrg, ".xlsx");

            FileInfo template = new FileInfo(path + "Template/template.xlsx");
            FileInfo newFile = new FileInfo(fullPath);

            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(fullPath);
            }

            using (ExcelPackage xlPackage = new ExcelPackage(newFile, template))
            {
                ExcelWorksheet worksheet = xlPackage.Workbook.Worksheets[1];

                if (worksheet != null)
                {
                    int row = 1;

                    worksheet.Cells[row, 1].Value = "ISD Billing Account Number";
                    worksheet.Cells[row, 2].Value = "Activity Code";
                    worksheet.Cells[row, 3].Value = "Project ID #";
                    worksheet.Cells[row, 4].Value = "Description of Project";
                    worksheet.Cells[row, 5].Value = "DPW Contact";
                    worksheet.Cells[row, 6].Value = "DPW Contact Phone";
                    worksheet.Cells[row, 7].Value = "Employee Name";
                    worksheet.Cells[row, 8].Value = "Total";
                    worksheet.Cells[row, 9].Value = "Labor Corrections";
                    worksheet.Cells[row, 10].Value = "Adjusted Hours";
                    worksheet.Cells[row, 11].Value = "Comment";

                    var data = CommonHelper.GetDeptBillingModel(month, dept, fundOrg);

                    decimal grandTotal = 0.0m;
                    foreach (var d in data)
                    {
                        row++;

                        worksheet.Cells[row, 1].Value = d.Unit1;
                        worksheet.Cells[row, 2].Value = d.ActivityCode1;
                        worksheet.Cells[row, 3].Value = d.JOC1;
                        worksheet.Cells[row, 4].Value = d.ProjectDescription;
                        worksheet.Cells[row, 5].Value = d.DepartmentContactName;
                        worksheet.Cells[row, 6].Value = d.DepartmentContactPhone;
                        worksheet.Cells[row, 7].Value = d.EmployeeName;
                        worksheet.Cells[row, 8].Value = d.TotalHours;

                        grandTotal += d.TotalHours;
                    }

                    worksheet.Cells[row + 1, 8].Value = grandTotal;
                    using (var range = worksheet.Cells[row + 1, 1, row + 1, 8])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    }

                    xlPackage.Save();
                }
            }

            return fullPath;
        }

        private bool SendEmail(List<MailAddress> to, List<MailAddress> cc, string subject, string body, string filePath, string type,
            string attached = "", string attachmentPath = "")
        {
            try
            {
                MailMessage mail = new MailMessage();
                MailAddress from = (subject.Contains("Rejected Document"))
                    ? new MailAddress("CCCS_Clear@isd.lacounty.gov")
                    : new MailAddress("CCCS_EEO@isd.lacounty.gov"); mail.From = from;

                string emailTo = "";
                foreach (var t in to)
                {
                    mail.To.Add(t);
                    emailTo += t.Address + ";";
                }

                string emailCc = "";
                foreach (var c in cc)
                {
                    mail.CC.Add(c);
                    emailCc += c.Address + ";";
                }

                if (!String.IsNullOrEmpty(filePath))
                    mail.Attachments.Add(new Attachment(filePath));

                if (!String.IsNullOrEmpty(attached))
                {
                    string[] files = attached.Split(';');

                    foreach (var f in files)
                    {
                        if (!String.IsNullOrEmpty(f))
                        {
                            string path = Server.MapPath(attachmentPath) + f;
                            FileInfo fi = new FileInfo(path);

                            if (fi.Exists)
                            {
                                mail.Attachments.Add(new Attachment(path));
                            }
                            else
                            {
                                throw new FileNotFoundException("Attachment file was not uploaded properly.");
                            }
                        }
                    }
                }

                mail.Subject = subject;
                mail.Body = body;

                SmtpClient emailClient = new SmtpClient("mail.co.la.ca.us");
                emailClient.Send(mail);

                EmailLog log = new EmailLog
                {
                    EmailTo = emailTo,
                    EmailCc = emailCc,
                    Subject = subject,
                    EmailBody = body,
                    DateSent = DateTime.Now,
                    NoticeType = type
                };
                db.EmailLogs.Add(log);
                db.SaveChanges();

                TempData["Message"] = type + " was sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error sending '" + type + "'";
                return false;
            }

            return true;
        }

        #endregion


        #region Registration Approval

        public ActionResult ApproveRegistration(string id, string returnUrl)
        {
            EmailModel model = new EmailModel
            {
                UserID = id,
                Action = "SendApprovalNotice",
                Title = "Registration Approval",
                ReturnUrl = returnUrl,
                AttachmentRequired = false
            };

            var userId = User.Identity.GetUserId();
            var profile = _publicProfileRepository.Table.FirstOrDefault(x => x.UserID == id);
            model.To = UserManager.FindById(id).Email;
            model.Cc = UserManager.FindById(userId).Email;

            string body = "Hi " + profile.Name + "," + Environment.NewLine;
            body += Environment.NewLine + "Your access to the website 'http://cccs.isd.lacounty.gov/public' was approved by CCCS." + Environment.NewLine;
            body += Environment.NewLine + "Please use the website to request clearance request for your project.";
            body += "If you have any question or need assistance, please feel free to contact CCCS.  Thank you." + Environment.NewLine;
            body += Environment.NewLine + "Countywide Contract Compliance Section";
            model.Body = body;

            string altBody = "Hi " + profile.Name + "," + Environment.NewLine;
            altBody += Environment.NewLine + "Your access to the website 'http://cccs.isd.lacounty.gov/public' was rejected by CCCS." + Environment.NewLine;
            altBody += Environment.NewLine + "If you have any question or need assistance, please feel free to contact CCCS.  Thank you." + Environment.NewLine;
            altBody += Environment.NewLine + "Countywide Contract Compliance Section";
            model.AlternateBody = altBody;

            model.Recipients = GetEmailRecipients(profile.Department, true, true, true);

            return View("Email", model);
        }

        [ValidateInput(false)]
        public ActionResult SendApprovalNotice(FormCollection form, EmailModel model)
        {
            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            try
            {
                var profile = _publicProfileRepository.Table.FirstOrDefault(x => x.UserID == model.UserID);
                profile.DateModified = DateTime.Now;
                profile.IsActive = true;
                db.Entry(profile).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error while approving registration.";

                return View("Email", model);
            }
            SendEmail(to, cc, subject, body, null, model.Title);

            return Redirect(model.ReturnUrl);
        }


        #endregion


        #region Document Reject

        public ActionResult EmailDocumentRejected(int id, string returnUrl = "")
        {
            DocumentFile doc = db.DocumentFiles.Find(id);

            EmailModel model = new EmailModel
            {
                DocumentID = id,
                Action = "SendDocumentRejected",
                Title = "Rejected Document",
                ReturnUrl = returnUrl,
                AttachmentRequired = true
            };

            var contacts = db.ContractorContacts.Where(x => x.ContractorId == doc.ContractorID).Where(x => x.Email.Contains("@")).Select(x => x.Email).ToArray();
            model.To = String.Join(";", contacts);
            model.Cc = "CCCS_Clear@isd.lacounty.gov;";

            if (contacts.Length == 0)
            {
                TempData["Message"] = "Error: Contractor email is not available.  Please enter email";
            }

            //string body = "Hi," + Environment.NewLine;
            //body += Environment.NewLine + "Your submitted '" + doc.DocumentType + "' has been rejected by CCCS for the following reason:" + Environment.NewLine;
            //body += Environment.NewLine + doc.Comment + Environment.NewLine;
            //body += Environment.NewLine + "If you have any question or need assistance, please feel free to contact CCCS.  Thank you." + Environment.NewLine;
            //body += Environment.NewLine + "Countywide Contract Compliance Section";
            //model.Body = body;

            model.Recipients = GetEmailRecipients(
                projectId: doc.ProjectID,
                includeManager: true,
                includeClerical: true,
                includeDCO: true);

            return View("Email", model);
        }

        [ValidateInput(false)]
        public ActionResult SendDocumentRejected(FormCollection form, EmailModel model)
        {
            List<MailAddress> to = new List<MailAddress>();
            List<MailAddress> cc = new List<MailAddress>();
            try
            {
                to = FetchEmails(form["To"]);
                cc = FetchEmails(form["Cc"]);
            }
            catch(Exception ex)
            {
                DocumentFile doc = db.DocumentFiles.Find(model.DocumentID);
                model.Recipients = GetEmailRecipients(
                    projectId: doc.ProjectID,
                    includeManager: true,
                    includeClerical: true,
                    includeDCO: true);
                TempData["Message"] = ex.Message;

                return View("Email", model);
            }

            string subject = form["Subject"];
            string body = form["Body"];
            string attached = form["Attached"];
            string attachmentPath = form["AttachmentPath"];

            try
            {
                string filePath = "";
                DocumentFile doc = db.DocumentFiles.Find(model.DocumentID);
                if (doc != null && !String.IsNullOrEmpty(doc.FileName))
                {
                    filePath = Server.MapPath(doc.FilePath);
                }

                SendEmail(to, cc, subject, body, filePath, model.Title, attached, attachmentPath);
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error while sending email.";

                return View("Email", model);
            }

            return Redirect(model.ReturnUrl);
        }


        #endregion


        #region Document Past Due

        public ActionResult EmailDPD(string dept, string returnUrl = "")
        {
            EmailModel model = new EmailModel
            {
                Action = "SendDPD",
                Title = "Document Past Due",
                Department = dept,
                ReturnUrl = returnUrl,
                AttachmentRequired = true
            };

            var userId = User.Identity.GetUserId();
            model.Cc = UserManager.FindById(userId).Email;
            model.Recipients = GetEmailRecipients(dept, true, true, true);

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendDPD(FormCollection form, EmailModel model)
        {
            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            string filePath = GetExcelDocumentPastDue(model.Department);

            SendEmail(to, cc, subject, body, filePath, model.Title);

            return Redirect(model.ReturnUrl);
        }

        private string GetExcelDocumentPastDue(string dept)
        {
            string path = Server.MapPath("~/Files/DocumentPastDue/");
            DirectoryInfo outputDir = new DirectoryInfo(path);
            string dt = DateTime.Today.ToString("yyyyMM");
            string fullPath = string.Concat(path, dept, "_", dt, ".xlsx");

            FileInfo template = new FileInfo(path + "Template/template.xlsx");
            FileInfo newFile = new FileInfo(fullPath);

            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(fullPath);
            }

            using (ExcelPackage xlPackage = new ExcelPackage(newFile, template))
            {
                ExcelWorksheet worksheet = xlPackage.Workbook.Worksheets[1];

                if (worksheet != null)
                {
                    int row = 1;

                    worksheet.Cells[row, 1].Value = "Project ID";
                    worksheet.Cells[row, 2].Value = "Project Name";
                    worksheet.Cells[row, 3].Value = "Contractor Name";
                    worksheet.Cells[row, 4].Value = "Project Manager";
                    worksheet.Cells[row, 5].Value = "Start Date";
                    worksheet.Cells[row, 6].Value = "End Date";
                    worksheet.Cells[row, 7].Value = "Year";
                    worksheet.Cells[row, 8].Value = "Month";
                    worksheet.Cells[row, 9].Value = "Document Type";
                    worksheet.Cells[row, 10].Value = "Date Required";

                    var data = CommonHelper.GetPastDueDocumentModel(dept);

                    foreach (var d in data)
                    {
                        row++;

                        worksheet.Cells[row, 1].Value = d.JOC;
                        worksheet.Cells[row, 2].Value = d.ProjectName;
                        worksheet.Cells[row, 3].Value = d.ContractorName;
                        worksheet.Cells[row, 4].Value = d.ProjectManager;
                        worksheet.Cells[row, 5].Value = d.StartDate;
                        worksheet.Cells[row, 6].Value = d.EndDate;
                        worksheet.Cells[row, 7].Value = d.Year;
                        if (d.Month > 0)
                            worksheet.Cells[row, 8].Value = d.Month;
                        worksheet.Cells[row, 9].Value = d.DocumentType;
                        worksheet.Cells[row, 10].Value = d.DateRequired;
                    }

                    xlPackage.Save();
                }
            }

            return fullPath;
        }

        #endregion


        #region Document List

        public ActionResult EmailDocumentList(int id, int cid, string returnUrl = "")
        {
            EmailModel model = new EmailModel
            {
                Action = "SendDocumentList",
                ProjectID = id,
                Title = "Document List",
                ReturnUrl = returnUrl,
                AttachmentRequired = true
            };

            var userId = User.Identity.GetUserId();
            model.Cc = UserManager.FindById(userId).Email;
            model.Recipients = GetEmailRecipients();
            model.ContractorContacts = GetContractorContacts(cid);
            model.CompanyName = db.Contractors.Find(cid).CompanyName;

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendDocumentList(FormCollection form, EmailModel model)
        {
            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            string filePath = GetExcelDocumentList(model.ProjectID);

            SendEmail(to, cc, subject, body, filePath, model.Title);

            return Redirect(model.ReturnUrl);
        }

        public ActionResult ViewDocumentList(int id)
        {
            string filePath = GetExcelDocumentList(id);

            return Redirect("~/Download.ashx?f=" + filePath);
        }

        private string GetExcelDocumentList(int projectId)
        {
            string path = Server.MapPath("~/Files/DocumentList/");
            DirectoryInfo outputDir = new DirectoryInfo(path);
            string fullPath = string.Concat(path, projectId, ".xlsx");

            FileInfo template = new FileInfo(path + "Template\\DocumentList.xlsx");
            FileInfo newFile = new FileInfo(fullPath);

            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(fullPath);
            }

            using (ExcelPackage xlPackage = new ExcelPackage(newFile, template))
            {
                ExcelWorksheet worksheet = xlPackage.Workbook.Worksheets[1];

                if (worksheet != null)
                {
                    int row = 2;

                    var data = new ProjectController().GetDocumentList(projectId);

                    foreach (var d in data)
                    {
                        row++;

                        worksheet.Cells[row, 1].Value = d.CompanyName;

                        for (int y = d.StartYear; y <= d.EndYear; y++)
                        {
                            var dr = d.DocumentRows.FirstOrDefault(x => x.Year == y);

                            if (dr.PrevPerf != null && dr.PrevPerf.Status == "Approved")
                                worksheet.Cells[row, 2].Value = "X";

                            if (dr.NonSeg != null && dr.NonSeg.Status == "Approved")
                                worksheet.Cells[row, 3].Value = "X";

                            if (dr.GFE != null && dr.GFE.Status == "Approved")
                                worksheet.Cells[row, 4].Value = "X";

                            if (dr.ListSub != null && dr.ListSub.Status == "Approved")
                                worksheet.Cells[row, 5].Value = "X";

                            if (dr.NtceEEO != null && dr.NtceEEO.Status == "Approved")
                                worksheet.Cells[row, 6].Value = "X";

                            if (dr.WIBA1 != null && dr.WIBA1.Status == "Approved")
                                worksheet.Cells[row, 7].Value = "X";

                            if (dr.WIBA2 != null && dr.WIBA2.Status == "Approved")
                                worksheet.Cells[row, 8].Value = "X";

                            worksheet.Cells[row, 9].Value = dr.Year;

                            if (dr.EUR1 != null && dr.EUR1.Status == "Approved")
                                worksheet.Cells[row, 10].Value = "X";
                            if (dr.EUR2 != null && dr.EUR2.Status == "Approved")
                                worksheet.Cells[row, 11].Value = "X";
                            if (dr.EUR3 != null && dr.EUR3.Status == "Approved")
                                worksheet.Cells[row, 12].Value = "X";
                            if (dr.EUR4 != null && dr.EUR4.Status == "Approved")
                                worksheet.Cells[row, 13].Value = "X";
                            if (dr.EUR5 != null && dr.EUR5.Status == "Approved")
                                worksheet.Cells[row, 14].Value = "X";
                            if (dr.EUR6 != null && dr.EUR6.Status == "Approved")
                                worksheet.Cells[row, 15].Value = "X";
                            if (dr.EUR7 != null && dr.EUR7.Status == "Approved")
                                worksheet.Cells[row, 16].Value = "X";
                            if (dr.EUR8 != null && dr.EUR8.Status == "Approved")
                                worksheet.Cells[row, 17].Value = "X";
                            if (dr.EUR9 != null && dr.EUR9.Status == "Approved")
                                worksheet.Cells[row, 18].Value = "X";
                            if (dr.EUR10 != null && dr.EUR10.Status == "Approved")
                                worksheet.Cells[row, 19].Value = "X";
                            if (dr.EUR11 != null && dr.EUR11.Status == "Approved")
                                worksheet.Cells[row, 20].Value = "X";
                            if (dr.EUR12 != null && dr.EUR12.Status == "Approved")
                                worksheet.Cells[row, 21].Value = "X";
                        }

                        using (var range = worksheet.Cells[3, 2, row, 8])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Font.Color.SetColor(Color.Green);
                        }

                        using (var range = worksheet.Cells[3, 9, row, 21])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Font.Color.SetColor(Color.Green);
                        }
                    }

                    xlPackage.Save();
                }
            }

            return fullPath;
        }

        #endregion


        #region Clearance Request

        public ActionResult EmailCR(int id, string message = "")
        {
            var project = db.Projects.Find(id);

            EmailModel model = new EmailModel
            {
                ProjectID = id,
                Action = "SendCR",
                Title = "Clearance Request: " + project.JOC,
                To = GetRequestorEmail(id),
                Cc = GetEmailRecipients(false, false, true),
                ReturnUrl = Request.UrlReferrer.PathAndQuery,
                Recipients = GetEmailRecipients(id, true, true, true),
                AttachmentRequired = true
            };

            return View("Email", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendCR(FormCollection form)
        {
            int id = int.Parse(form["ProjectID"]);

            var request = db.ClearanceRequests.FirstOrDefault(x => x.ProjectId == id);
            DateTime? sentToManagerDate = request.DateModified;
            DateTime now = DateTime.Now;
            request.DateModified = now;
            request.ProcessedBy = UserProfile.UserInitial;
            request.CurrentStatus = "Sent to Department";

            ClearanceRequestLog log = new ClearanceRequestLog
            {
                ClearanceRequestId = request.Id,
                Date = now,
                Activity = "Sent to Department"
            };
            db.ClearanceRequestLogs.Add(log);

            var reqForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);
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

            //safety for project date closed
            var project = db.Projects.Find(id);
            if (project != null && project.DateClosed == null)
            {
                project.DateClosed = (sentToManagerDate != null) ? sentToManagerDate: now;
                db.Entry(project).State = EntityState.Modified;
            }

            db.SaveChanges();

            string filePath = Server.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".PDF";

            var crForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);
            if (crForm != null)
            {
                CommonHelper.CreateCR(id);
            }

            List<MailAddress> to = FetchEmails(form["To"]);
            List<MailAddress> cc = FetchEmails(form["Cc"]);

            string subject = form["Subject"];
            string body = form["Body"];

            try
            {
                if (crForm == null)
                {
                    SendEmail(to, cc, subject, body, null, "Clearance Request");
                }
                else
                {
                    SendEmail(to, cc, subject, body, filePath, "Clearance Request");
                }

                TempData["Message"] = "'Clearance Request' was sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: There was an error while sending 'Clearance Request'.";
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion


        #region Utilities

        private List<MailAddress> FetchEmails(string list)
        {
            List<MailAddress> addressList = new List<MailAddress>();

            string[] arr = list.Split(';');

            foreach (string s in arr)
            {
                string item = s.Trim();

                if (item.Contains("@"))
                {
                    MailAddress address = null;
                    try
                    {
                        address = new MailAddress(item);
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        if (address != null)
                        {
                            addressList.Add(address);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(item))
                {
                    var recipient = db.EmailRecipients.FirstOrDefault(x => x.Name == item);

                    if (recipient != null)
                    {
                        addressList.Add(new MailAddress(recipient.Email));
                    }
                    else
                    {
                        ServiceSoapClient ad = new ServiceSoapClient();

                        string[] attrs = new string[1];
                        attrs.SetValue("mail", 0);

                        DataTable dt = ad.UserSearchReturnAttributes(item, attrs).Tables[0];

                        if (dt.Rows.Count == 1)
                        {
                            string mail = dt.Rows[0][0].ToString();

                            EmailRecipient r = new EmailRecipient { Name = item, Email = mail };

                            db.EmailRecipients.Add(r);
                            db.SaveChanges();
                        }
                        else
                        {
                            throw new Exception("Error: Email address of '" + item + "' is not found.");
                        }
                    }
                }
            }

            return addressList;
        }

        private string GetEmailCopy(string filePath, string[] args)
        {
            string newPath = filePath;

            try
            {
                PdfReader pdfReader = new PdfReader(filePath);

                AcroFields fields = pdfReader.AcroFields;

                foreach (string arg in args)
                {
                    fields.RemoveField(arg);
                }

                newPath = filePath.Replace(".PDF", "e.PDF");
                PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(newPath, FileMode.Create));

                pdfStamper.Close();
            }
            catch (Exception ex)
            {
                return filePath;
            }

            return newPath;
        }

        private string GetContractorEmails(int id)
        {
            string emails = "";
            var inspection = db.Inspections.Find(id);

            var list1 = db.ContractorContacts.Where(x => x.ContractorId == inspection.ContractorId && x.Email.Contains("@"))
                .Select(x => x.Email).Distinct().ToArray();

            if (list1.Count() > 0)
                emails = String.Join("; ", list1) + ";";

            var project = db.Projects.Find(inspection.ProjectId);

            if (project.PrimeContractorID != inspection.ContractorId)
            {
                var list2 = db.ContractorContacts.Where(x => x.ContractorId == project.PrimeContractorID && x.Email.Contains("@"))
                    .Select(x => x.Email).Distinct().ToArray();

                if (list2.Count() > 0)
                    emails += String.Join("; ", list2);
            }

            return emails;
        }

        private string GetContractorEmail(int id)
        {
            string emails = "";

            var project = db.Projects.Find(id);

            if (project.PrimeContractorID > 0)
            {
                var list = db.ContractorContacts.Where(x => x.ContractorId == project.PrimeContractorID && x.Email.Contains("@"))
                    .Select(x => x.Email).Distinct().ToArray();

                emails += String.Join("; ", list);
            }

            return emails;
        }

        private string GetRequestorEmail(int id)
        {
            string emails = "";

            var crForm = db.ClearanceRequestForms.FirstOrDefault(x => x.ProjectId == id);

            if (crForm != null)
            {
                emails += crForm.Email;
            }

            return emails;
        }

        private string FixReturnUrl(string returnUrl, int? id = null)
        {
            if (returnUrl.ToLower().Contains("home"))
            {
                returnUrl = "/Home/DCO";
            }
            else if (returnUrl.ToLower().Contains("inspection"))
            {
                returnUrl = "/Inspection/DCO1/" + UserProfile.UserInitial;
            }
            else if (returnUrl.ToLower().Contains("project"))
            {
                returnUrl = "/Project/Details2/" + id.ToString();
            }

            return returnUrl;
        }

        public List<SelectListItem> GetEmailRecipients(string dept = "",
            bool includeManager = false, bool includeClerical = false, bool includeDCO = false)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (!String.IsNullOrEmpty(dept))
            {
                var recipients = (from r in db.EmailRecipients
                                  join d in db.Departments on r.DepartmentId equals d.DepartmentId
                                  where d.DepartmentId == dept
                                  select new { Name = r.Name, Email = r.Email, DepartmentName = d.DepartmentName }).ToList();
                foreach (var r in recipients)
                {
                    list.Add(new SelectListItem { Text = r.Name + " (" + r.DepartmentName + ")", Value = r.Email });
                }
            }

            if (includeManager)
            {
                var admin = RoleManager.Roles.FirstOrDefault(x => x.Name == "Manager").Users.First();
                var manager = UserManager.FindById(admin.UserId);
                list.Add(new SelectListItem { Text = manager.UserName + " (Manager)", Value = manager.Email });
            }

            if (includeClerical)
            {
                var clericals = RoleManager.Roles.FirstOrDefault(x => x.Name == "Clerical" || x.Name == "Clerical2").Users;
                foreach (var c in clericals)
                {
                    var clerical = UserManager.FindById(c.UserId);
                    if (clerical != null)
                        list.Add(new SelectListItem { Text = clerical.UserName + "(Clerical)", Value = clerical.Email });
                }
            }

            if (includeDCO)
            {
                var dcos = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO").Users;
                foreach (var d in dcos)
                {
                    var dco = UserManager.FindById(d.UserId);
                    list.Add(new SelectListItem { Text = dco.UserName + " (DCO)", Value = dco.Email });
                }
            }

            return list.OrderBy(x => x.Text).ToList();
        }

        public string GetEmailRecipients(bool includeManager = false, bool includeUser = false, bool includeCCCS = false)
        {
            string list = "";

            if (includeManager)
            {
                var admin = RoleManager.Roles.FirstOrDefault(x => x.Name == "Manager").Users.First();
                var manager = UserManager.FindById(admin.UserId);
                list += manager.Email;
            }

            if (includeUser)
            {
                var user = UserManager.Users.FirstOrDefault(x => x.UserName == User.Identity.Name);

                if (!list.Contains(user.Email))
                    list = (String.IsNullOrEmpty(list)) ? user.Email : list + "; " + user.Email;
            }

            if (includeCCCS)
            {
                list = (String.IsNullOrEmpty(list)) ? EMAIL_CCCS_EEO : list + "; " + EMAIL_CCCS_EEO;
            }

            return list;
        }

        public List<SelectListItem> GetEmailRecipients(int projectId,
            bool includeManager = false, bool includeClerical = false, bool includeDCO = false)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (projectId > 0)
            {
                var recipients = (from ct in db.Contracts
                                  join c in db.Contractors on ct.ContractorId equals c.Id
                                  join cc in db.ContractorContacts on c.Id equals cc.ContractorId
                                  where ct.ProjectId == projectId
                                  select new { Name = cc.Name, CompanyName = c.CompanyName, Email = cc.Email }).ToList();
                foreach (var r in recipients)
                {
                    list.Add(new SelectListItem { Text = r.Name + " (" + r.CompanyName + ")", Value = r.Email });
                }
            }

            if (includeManager)
            {
                var admin = RoleManager.Roles.FirstOrDefault(x => x.Name == "Manager").Users.First();
                var manager = UserManager.FindById(admin.UserId);
                list.Add(new SelectListItem { Text = manager.UserName + " (Manager)", Value = manager.Email });
            }

            if (includeClerical)
            {
                var clericals = RoleManager.Roles.FirstOrDefault(x => x.Name == "Clerical" || x.Name == "Clerical2").Users;
                foreach (var c in clericals)
                {
                    var clerical = UserManager.FindById(c.UserId);
                    list.Add(new SelectListItem { Text = clerical.UserName + "(Clerical)", Value = clerical.Email });
                }
            }

            if (includeDCO)
            {
                var dcos = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO").Users;
                foreach (var d in dcos)
                {
                    var dco = UserManager.FindById(d.UserId);
                    list.Add(new SelectListItem { Text = dco.UserName + " (DCO)", Value = dco.Email });
                }
            }

            return list.OrderBy(x => x.Text).ToList();
        }

        public List<SelectListItem> GetEmailRecipients()
        {
            List<SelectListItem> list = new List<SelectListItem>();

            var admin = RoleManager.Roles.FirstOrDefault(x => x.Name == "Manager").Users
                .Where(x=> x.UserId != "df4bed6b-9905-475f-a622-a3dfa43d390e" && x.UserId != "870c7cab-baa0-4ec6-9d04-cd8ff7297fe9").First();
            var manager = UserManager.FindById(admin.UserId);
            list.Add(new SelectListItem { Text = manager.UserName + " (Manager)", Value = manager.Email });

            var clericals = RoleManager.Roles.FirstOrDefault(x => x.Name == "Clerical" || x.Name == "Clerical2").Users;
            foreach (var c in clericals)
            {
                var clerical = UserManager.FindById(c.UserId);
                list.Add(new SelectListItem { Text = clerical.UserName + "(Clerical)", Value = clerical.Email });
            }

            var dcos = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO").Users;
            foreach (var d in dcos)
            {
                var dco = UserManager.FindById(d.UserId);
                list.Add(new SelectListItem { Text = dco.UserName + " (DCO)", Value = dco.Email });
            }

            return list.OrderBy(x => x.Text).ToList();
        }


        public List<SelectListItem> GetContractorContacts(int contractorId)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            var recipients = (from c in db.Contractors
                              join cc in db.ContractorContacts on c.Id equals cc.ContractorId
                              where c.Id == contractorId
                              select new { Name = cc.Name, CompanyName = c.CompanyName, Email = cc.Email }).ToList();
            foreach (var r in recipients)
            {
                list.Add(new SelectListItem { Text = r.Name + " <" + r.Email + ">", Value = r.Email });
            }

            return list.OrderBy(x => x.Text).ToList();
        }


        #endregion

        [HttpPost]
        public JsonResult UploadAttachment(FormCollection form)
        {
            FileUploadResult result = new FileUploadResult();

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        string type = form["type"];
                        string id = form["id"];
                        string fileName = form["fileName"];
                        string newPath = "~/Files/Attachments/" + type + "_" + id + "/";

                        if (!Directory.Exists(Server.MapPath(newPath)))
                        {
                            Directory.CreateDirectory(Server.MapPath(newPath));
                        }

                        var filePath = Server.MapPath(newPath + fileName);
                        file.SaveAs(filePath);

                        result.IsSuccess = true;
                        result.Path = newPath;
                        result.Message = "File is uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.Message = ex.Message;
                    }
                }
            }

            return Json(result);
        }

        public List<SelectListItem> GetProjectContacts(int id)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            var contacts = db.ProjectContacts.FirstOrDefault(x => x.ProjectId == id);
            if (contacts != null)
            {
                if (!String.IsNullOrEmpty(contacts.DeptContactEmail))
                    list.Add(new SelectListItem { Text = contacts.DeptContact + " (Department Contact)", Value = contacts.DeptContactEmail });
                if (!String.IsNullOrEmpty(contacts.ProjectManagerEmail))
                    list.Add(new SelectListItem { Text = contacts.ProjectManager + " (Project Manager)", Value = contacts.ProjectManagerEmail });
                if (!String.IsNullOrEmpty(contacts.Analyst))
                    list.Add(new SelectListItem { Text = contacts.Analyst + " (Analyst)", Value = contacts.AnalystEmail });
            }

            return list;
        }
    }
}