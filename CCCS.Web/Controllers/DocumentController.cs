using CCCS.Core.Domain.Documents;
using CCCS.Core.Domain.Inspection;
using CCCS.Core.Domain.Notices;
using CCCS.Core.Domain.Projects;
using CCCS.Web.Models;
using CCCS.Web.AdService;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using CCCS.Web.Models.Contractors;
using CCCS.Web.Models.Documents;
using CCCS.Infrastructure;
using System.Text;

namespace CCCS.Controllers
{
    [Authorize]
    public class DocumentController : BaseController
    {
        // GET: Document
        public ActionResult Index()
        {
            return View();
        }

        // GET: Document/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // POST: Document/SetDates
        [HttpPost]
        public ActionResult SetDates(FormCollection collection, int id)
        {
            try
            {
                string documentName = collection["DocumentName"];
                int cId = int.Parse(collection["ContractorID"]);

                Document document = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.ContractorID == cId && x.DocumentName == documentName);
                if (document == null)
                {
                    document = new Document
                    {
                        ProjectID = id,
                        ContractorID = cId,
                        DocumentName = documentName,
                        HardCopy = (collection["HardCopy"] != "false"),
                        Electronic = (collection["Electronic"] != "false")
                    };

                    if (!string.IsNullOrEmpty(collection["DateRequested"]))
                    {
                        document.DateRequested = DateTime.Parse(collection["DateRequested"]);
                    }

                    if (!string.IsNullOrEmpty(collection["DateReceived"]))
                    {
                        document.DateReceived = DateTime.Parse(collection["DateReceived"]);
                    }

                    db.Documents.Add(document);
                }
                else
                {
                    if (!string.IsNullOrEmpty(collection["DateRequested"]))
                        document.DateRequested = DateTime.Parse(collection["DateRequested"]);
                    else
                        document.DateRequested = null;

                    if (!string.IsNullOrEmpty(collection["DateReceived"]))
                        document.DateReceived = DateTime.Parse(collection["DateReceived"]);
                    else
                        document.DateReceived = null;

                    document.HardCopy = (collection["HardCopy"] != "false");
                    document.Electronic = (collection["Electronic"] != "false");
                }

                db.SaveChanges();

            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
            }

            return Redirect(Request.UrlReferrer.PathAndQuery);
        }

        [HttpPost]
        public ActionResult SetDatesCommon(FormCollection collection, int id)
        {
            try
            {
                string documentName = collection["DocumentName"];

                Document document = db.Documents.FirstOrDefault(x => x.ProjectID == 0 && x.ContractorID == id && x.DocumentName == documentName);
                if (document == null)
                {
                    document = new Document
                    {
                        ContractorID = id,
                        DocumentName = documentName,
                        HardCopy = (collection["HardCopy"] != "false"),
                        Electronic = (collection["Electronic"] != "false")
                    };

                    if (!string.IsNullOrEmpty(collection["DateRequested"]))
                    {
                        document.DateRequested = DateTime.Parse(collection["DateRequested"]);
                    }

                    if (!string.IsNullOrEmpty(collection["DateReceived"]))
                    {
                        document.DateReceived = DateTime.Parse(collection["DateReceived"]);
                    }

                    db.Documents.Add(document);
                }
                else
                {
                    if (!string.IsNullOrEmpty(collection["DateRequested"]))
                        document.DateRequested = DateTime.Parse(collection["DateRequested"]);
                    else
                        document.DateRequested = null;

                    if (!string.IsNullOrEmpty(collection["DateReceived"]))
                        document.DateReceived = DateTime.Parse(collection["DateReceived"]);
                    else
                        document.DateReceived = null;

                    document.HardCopy = (collection["HardCopy"] != "false");
                    document.Electronic = (collection["Electronic"] != "false");

                    db.Entry(document).State = EntityState.Modified;
                }

                db.SaveChanges();

            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
            }

            return Redirect(Request.UrlReferrer.PathAndQuery);
        }

        // GET: Document/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Document/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Document/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Document/Delete/5
        public ActionResult DeleteDocument(int id, FormCollection collection)
        {
            string message = "";

            try
            {
                Document doc = db.Documents.Find(id);
                db.Documents.Remove(doc);

                if (!String.IsNullOrEmpty(Request.Params["insp"]) && Request.Params["insp"] == "true")
                {
                    InspectionLog log = new InspectionLog
                    {
                        InspectionID = (int)doc.InspectionID,
                        Date = DateTime.Now,
                        Activity = "Inspection document deleted",
                        ProcessedBy = UserProfile.UserInitial
                    };
                    db.InspectionLogs.Add(log);
                }

                db.SaveChanges();

                message = "File was deleted successfully.";
            }
            catch (Exception ex)
            {
                message = "Error: " + "There was an error while deleting file.";
            }

            TempData["Message"] = message;

            return Redirect(HttpContext.Request.UrlReferrer.PathAndQuery);
        }

        public JsonResult GetDocument(int id, string name)
        {
            try
            {
                Document document = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == name);
                if (document != null)
                {
                    var doc = new
                    {
                        DateRequested = (document.DateRequested == null) ? "" : document.DateRequested.Value.ToString("MM/dd/yyyy"),
                        DateReceived = (document.DateReceived == null) ? "" : document.DateReceived.Value.ToString("MM/dd/yyyy"),
                        HardCopy = document.HardCopy,
                        Electronic = document.Electronic
                    };
                    return Json(doc);
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public ActionResult UploadFile(FormCollection collection, int id)
        {
            string message = "";

            if (Request.Files.Count > 0)
            {
                string documentName = collection["UploadName"];
                var file = Request.Files[0];
                int documentId;

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        var fileName = string.Concat(id.ToString(), "_", Path.GetFileName(file.FileName));
                        var path = Path.Combine(Server.MapPath("~/Files/"), fileName);
                        file.SaveAs(path);

                        Document doc = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == documentName);

                        if (doc == null)
                        {
                            doc = new Document
                            {
                                ProjectID = id,
                                DocumentName = documentName,
                                Electronic = true
                            };
                            db.Documents.Add(doc);
                            db.SaveChanges();

                            var d = db.Documents.OrderByDescending(x => x.DocumentID).FirstOrDefault(x => x.ProjectID == id);
                            documentId = d.DocumentID;
                        }
                        else
                        {
                            documentId = doc.DocumentID;
                        }

                        db.SaveChanges();

                        message = "File is uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        message = "Error: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Details2", "Project", new { id = id, message = message });
        }

        [HttpPost]
        public ActionResult UploadFile2(FormCollection collection, int id = 0, string returnUrl = "")
        {
            string message = "";
            string documentName = collection["UploadDocumentName"].Replace("u_", "");
            string contractorId = collection["UploadContractorID"].Replace("u_", "");
            int cId = int.Parse(contractorId);

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        var fileName = string.Concat(id.ToString(), "_", contractorId, "_", Path.GetFileName(file.FileName));
                        var pathName = string.Concat(Server.MapPath("~/Files/"), documentName.Replace(" ", "_"), "/");
                        var path = Path.Combine(pathName, fileName);
                        file.SaveAs(path);

                        Document doc = db.Documents.FirstOrDefault(x => x.ProjectID == id && x.DocumentName == documentName && x.ContractorID == cId);

                        doc.FileName = fileName;
                        doc.Path = path;
                        doc.DateUploaded = DateTime.Now;

                        db.Entry(doc).State = EntityState.Modified;
                        db.SaveChanges();

                        message = "File was uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        message = "Error: " + ex.Message;
                    }
                }
            }
            else
            {
                message = "Error: " + "Request has no file content.";
            }

            TempData["Message"] = message;

            return Redirect(HttpContext.Request.UrlReferrer.PathAndQuery);
        }

        public ActionResult Download(int id)
        {
            var document = db.Documents.FirstOrDefault(x => x.DocumentID == id);
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = document.FileName,

                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());

            byte[] fileData = System.IO.File.ReadAllBytes(document.Path);
            string contentType = MimeMapping.GetMimeMapping(document.Path);

            return File(fileData, contentType);
        }

        public FileResult DownloadSR(int id)
        {
            var sr = db.ServiceRequests.FirstOrDefault(x => x.ID == id);
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = sr.FileName,

                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());

            string path = Path.Combine(Server.MapPath("~/Files/ServiceRequest/"), sr.FilePath);
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            string contentType = MimeMapping.GetMimeMapping(path);

            return File(fileData, contentType);
        }

        public ViewResult NoticeOfEEO(int id)
        {
            string documentName = "Notice of EEO";

            List<ContractorDocumentModel> model = new List<ContractorDocumentModel>();

            var prime = (from c in db.Contractors
                         join p in db.Projects on c.Id equals p.PrimeContractorID
                         where p.Id == id
                         select c).First();

            ContractorDocumentModel m = new ContractorDocumentModel
            {
                ContractorID = prime.Id,
                CompanyName = prime.CompanyName,
                IsPrime = true
            };

            var list = db.Documents.Where(x => x.ProjectID == id && x.ContractorID == prime.Id && x.DocumentName == documentName).ToList();
            m.Documents = list;

            if (list.Count > 0)
            {
                m.HardCopy = list.ElementAt(0).HardCopy;
                m.Electronic = list.ElementAt(0).Electronic;
            }

            model.Add(m);

            var subs = (from c in db.Contractors
                        join con in db.Contracts on c.Id equals con.ContractorId
                        where con.ProjectId == id
                        select c).ToList();

            foreach (var s in subs)
            {
                ContractorDocumentModel dm = new ContractorDocumentModel
                {
                    ContractorID = s.Id,
                    CompanyName = s.CompanyName
                };

                var list2 = db.Documents.Where(x => x.ProjectID == id && x.ContractorID == s.Id && x.DocumentName == documentName).ToList();
                dm.Documents = list2;

                if (list2.Count > 0)
                {
                    dm.HardCopy = list2.ElementAt(0).HardCopy;
                    dm.Electronic = list2.ElementAt(0).Electronic;
                }

                model.Add(dm);
            }

            ViewBag.Project = db.Projects.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        public ViewResult EUR(int id)
        {
            string documentName = "EUR";

            List<ContractorDocumentModel> model = new List<ContractorDocumentModel>();

            var prime = (from c in db.Contractors
                         join p in db.Projects on c.Id equals p.PrimeContractorID
                         where p.Id == id
                         select c).First();

            ContractorDocumentModel m = new ContractorDocumentModel
            {
                ContractorID = prime.Id,
                CompanyName = prime.CompanyName,
                IsPrime = true
            };

            m.Documents = db.Documents.Where(x => x.ProjectID == id && x.ContractorID == prime.Id && x.DocumentName == documentName).ToList();

            model.Add(m);

            var subs = (from c in db.Contractors
                        join ct in db.Contracts on c.Id equals ct.ContractorId
                        where ct.ProjectId == id
                        select c).ToList();

            foreach (var s in subs)
            {
                ContractorDocumentModel dm = new ContractorDocumentModel
                {
                    ContractorID = s.Id,
                    CompanyName = s.CompanyName
                };

                dm.Documents = db.Documents.Where(x => x.ProjectID == id && x.ContractorID == s.Id && x.DocumentName == documentName).ToList();

                model.Add(dm);
            }

            ViewBag.Project = db.Projects.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        // EUR from Contractor
        public ViewResult EUR2(int id)
        {
            var projects = (from p in db.Projects
                            where p.PrimeContractorID == id
                            select p)
                            .Union
                            (from p in db.Projects
                             join c in db.Contracts on p.Id equals c.ProjectId
                             where c.ContractorId == id
                             select p);

            var list = new List<SelectListItem>();
            list.Add(new SelectListItem { Text = "- Select project -", Value = "" });
            foreach (var p in projects)
            {
                string txt = p.ProjectName + " (" + p.JOC + ")";
                list.Add(new SelectListItem { Text = txt, Value = p.Id.ToString() });
            }
            ViewBag.Projects = list;

            var model = db.Contractors.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        [HttpPost]
        public ViewResult EUR2(FormCollection collection, int id)
        {
            string projectId = collection["Project"];

            var projects = (from p in db.Projects
                            where p.PrimeContractorID == id
                            select p)
                            .Union
                            (from p in db.Projects
                             join c in db.Contracts on p.Id equals c.ProjectId
                             where c.ContractorId == id
                             select p);

            var list = new List<SelectListItem>();
            list.Add(new SelectListItem { Text = "- Select project -", Value = "" });
            foreach (var p in projects)
            {
                string txt = p.ProjectName + " (" + p.JOC + ")";
                list.Add(new SelectListItem { Text = txt, Value = p.Id.ToString() });
            }
            ViewBag.Projects = list;
            ViewBag.CurrentProject = projectId;

            var model = db.Contractors.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        [HttpPost]
        public ActionResult SaveEUR(FormCollection collection, int id)
        {
            int cId = int.Parse(collection["ContractorID"]);
            string documentName = "EUR";

            Document document = new Document
            {
                ProjectID = id,
                ContractorID = cId,
                DocumentName = documentName,
                Title = collection["Title"],
                DateRequested = null,
                DateUploaded = null
            };

            if (!string.IsNullOrEmpty(collection["DateReceived"]))
                document.DateReceived = DateTime.Parse(collection["DateReceived"]);

            db.Documents.Add(document);
            db.SaveChanges();

            return RedirectToAction("EUR", new { id = id });
        }

        [HttpPost]
        public ActionResult UpdateEUR(FormCollection collection, int id)
        {
            int dId = int.Parse(collection["EditDocumentID"]);

            Document document = db.Documents.FirstOrDefault(x => x.DocumentID == dId);

            document.Title = collection["EditTitle"];

            if (!string.IsNullOrEmpty(collection["EditDateReceived"]))
                document.DateReceived = DateTime.Parse(collection["EditDateReceived"]);
            else
                document.DateReceived = null;

            db.SaveChanges();

            return RedirectToAction("EUR", new { id = id });
        }

        public ViewResult SaveDocument(int id, string documentName)
        {
            List<ContractorDocumentModel> model = new List<ContractorDocumentModel>();

            var prime = (from c in db.Contractors
                         join p in db.Projects on c.Id equals p.PrimeContractorID
                         where p.Id == id
                         select c).First();

            ContractorDocumentModel m = new ContractorDocumentModel
            {
                ContractorID = prime.Id,
                CompanyName = prime.CompanyName,
                IsPrime = true
            };

            var list = db.Documents.Where(x => x.ProjectID == 0 && x.ContractorID == prime.Id && x.DocumentName == documentName).ToList();
            m.Documents = list;

            if (list.Count > 0)
            {
                m.HardCopy = list.ElementAt(0).HardCopy;
                m.Electronic = list.ElementAt(0).Electronic;
            }

            model.Add(m);

            var subs = (from c in db.Contractors
                        join ct in db.Contracts on c.Id equals ct.ContractorId
                        where ct.ProjectId == id
                        select c).ToList();

            foreach (var s in subs)
            {
                ContractorDocumentModel dm = new ContractorDocumentModel
                {
                    ContractorID = s.Id,
                    CompanyName = s.CompanyName
                };

                var list2 = db.Documents.Where(x => x.ProjectID == 0 && x.ContractorID == s.Id && x.DocumentName == documentName).ToList();
                dm.Documents = list2;

                if (list2.Count > 0)
                {
                    dm.HardCopy = list2.ElementAt(0).HardCopy;
                    dm.Electronic = list2.ElementAt(0).Electronic;
                }

                model.Add(dm);
            }

            ViewBag.Project = db.Projects.FirstOrDefault(x => x.Id == id);
            ViewBag.DocumentName = documentName;

            return View(model);
        }

        public ViewResult GoodFaith(int id)
        {
            string documentName = "Good Faith";

            List<ContractorDocumentModel> model = new List<ContractorDocumentModel>();

            var prime = (from c in db.Contractors
                         join p in db.Projects on c.Id equals p.PrimeContractorID
                         where p.Id == id
                         select c).First();

            ContractorDocumentModel m = new ContractorDocumentModel
            {
                ContractorID = prime.Id,
                CompanyName = prime.CompanyName,
                IsPrime = true
            };

            var list = db.Documents.Where(x => x.ProjectID == 0 && x.ContractorID == prime.Id && x.DocumentName == documentName).ToList();
            m.Documents = list;

            if (list.Count > 0)
            {
                m.HardCopy = list.ElementAt(0).HardCopy;
                m.Electronic = list.ElementAt(0).Electronic;
            }

            model.Add(m);

            var subs = (from c in db.Contractors
                        join ct in db.Contracts on c.Id equals ct.ContractorId
                        where ct.ProjectId == id
                        select c).ToList();

            foreach (var s in subs)
            {
                ContractorDocumentModel dm = new ContractorDocumentModel
                {
                    ContractorID = s.Id,
                    CompanyName = s.CompanyName
                };

                var list2 = db.Documents.Where(x => x.ProjectID == 0 && x.ContractorID == s.Id && x.DocumentName == documentName).ToList();
                dm.Documents = list2;

                if (list2.Count > 0)
                {
                    dm.HardCopy = list2.ElementAt(0).HardCopy;
                    dm.Electronic = list2.ElementAt(0).Electronic;
                }

                model.Add(dm);
            }

            ViewBag.Project = db.Projects.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        public ViewResult WorkOrder(int id)
        {
            string documentName = "Work Order";

            Document model = db.Documents.Where(x => x.ProjectID == id && x.DocumentName == documentName).FirstOrDefault();

            ViewBag.Project = db.Projects.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        public ViewResult SiteVisitForms(int id)
        {
            var model = db.Projects.FirstOrDefault(x => x.Id == id);

            return View(model);
        }

        private string FetchEmails(string list)
        {
            string newList = "";

            string[] arr = list.Split(';');

            foreach (string s in arr)
            {
                string item = s.Trim();

                if (item.Contains("@"))
                {
                    newList += item + ",";
                }
                else if (!string.IsNullOrEmpty(item))
                {
                    var recipient = db.EmailRecipients.FirstOrDefault(x => x.Name == item);

                    if (recipient != null)
                    {
                        newList += recipient.Email + ",";
                    }
                    else
                    {
                        ServiceSoapClient ad = new ServiceSoapClient();

                        string[] attrs = new string[1];
                        attrs.SetValue("mail", 0);

                        System.Data.DataTable dt = ad.UserSearchReturnAttributes(item, attrs).Tables[0];

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

            return newList;
        }

        private string GetEmailServiceRequest(string filePath)
        {
            PdfReader pdfReader = new PdfReader(filePath);

            AcroFields fields = pdfReader.AcroFields;
            fields.RemoveField("btnSave");
            fields.RemoveField("btnClose");

            string emailPDF = filePath.Replace(".PDF", "_e.PDF");
            PdfStamper pdfStamper = new PdfStamper(pdfReader, new FileStream(emailPDF, FileMode.Create));

            pdfStamper.Close();

            return emailPDF;
        }

        [HttpPost]
        public JsonResult UploadDocument(FormCollection form)
        {
            string type = form["type"];
            FileUploadResult result = new FileUploadResult();

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (!file.FileName.ToLower().EndsWith(".pdf"))
                {
                    result.IsSuccess = false;
                    result.Message = "Only PDF file may be uploaded.";
                }
                else if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        string id = form["id"];
                        var fileName = string.Concat(type + "_", id, ".pdf");
                        string path = "~/Files/" + type + "/" + fileName;
                        var filePath = Server.MapPath(path);
                        file.SaveAs(filePath);

                        int projectId = int.Parse(id);
                        var doc = db.Documents.FirstOrDefault(x => x.ProjectID == projectId && x.DocumentName == type);
                        if (doc != null)
                        {
                            db.Documents.Remove(doc);
                            db.SaveChanges();
                        }

                        Document newDoc = new Document
                        {
                            ProjectID = projectId,
                            DocumentName = type,
                            DateUploaded = DateTime.Now,
                            FileName = fileName,
                            Path = path
                        };
                        db.Documents.Add(newDoc);
                        db.SaveChanges();

                        DateTime dt = (DateTime)newDoc.DateUploaded;
                        result.IsSuccess = true;
                        result.Message = "File is uploaded successfully.";
                        result.DateUploaded = "<label class='mar-right-10'>Upload Date:</label>" + dt.ToShortDateString() + " " + dt.ToShortTimeString();
                        string link = "<div class='pull-right'><a id='reset" + type + "' href='" + Url.Content("/Document/DeleteDocument/" + newDoc.DocumentID);
                        link += "' class='btn btn-default'>Reset</a></div>";
                        link += "<a target='_blank' href='" + Url.Content("/Files/" + type + "/" + fileName) + "' >View Document</a>";
                        result.ViewLink = link;
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

        [HttpPost]
        public ActionResult UploadDocument2(int id, FormCollection form)
        {
            string fileName = form["fileName"];
            string type = "Misc";
            string title = form["title"];
            FileUploadResult result = new FileUploadResult();

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        fileName = string.Concat(type + "_", id, fileName);
                        string path = "~/Files/" + type + "/" + fileName;
                        var filePath = Server.MapPath(path);
                        file.SaveAs(filePath);

                        Document newDoc = new Document
                        {
                            ProjectID = id,
                            Title = title,
                            DocumentName = type,
                            DateUploaded = DateTime.Now,
                            FileName = fileName,
                            Path = path
                        };
                        db.Documents.Add(newDoc);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.Message = ex.Message;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "Error: File can not be uploaded because file or file name is invalid.  ";
                    result.Message += "Please check the file name doesn't include space or special character(s).";
                }
            }

            return RedirectToAction("Details2", "Project", new { id = id });
        }

        [HttpPost]
        public ActionResult UploadSR(FormCollection form)
        {
            string message = "";
            string fiscalyear = form["fiscalYear"];

            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        string folder = fiscalyear + " Service Request\\";
                        DirectoryInfo di = new DirectoryInfo(Server.MapPath("~/Files/ServiceRequest/") + folder);
                        if (!di.Exists)
                        {
                            di.Create();
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        var path = Path.Combine(Server.MapPath("~/Files/ServiceRequest/"), folder, fileName);
                        file.SaveAs(path);

                        ServiceRequest sr = new ServiceRequest
                        {
                            FiscalYear = fiscalyear,
                            FilePath = folder + fileName,
                            FileName = fileName,
                            DateRegistered = DateTime.Now
                        };
                        db.ServiceRequests.Add(sr);
                        db.SaveChanges();

                        message = "Serivice Request was uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        message = "Error: " + ex.Message;
                    }
                }
            }
            else
            {
                message = "Error: " + "Request has no file content.";
            }

            TempData["Message"] = message;

            return RedirectToAction("Index4", "Project");
        }

        public ActionResult DeleteSR(int id)
        {
            var sr = db.ServiceRequests.Find(id);
            db.ServiceRequests.Remove(sr);
            db.SaveChanges();

            return RedirectToAction("Index4", "Project");
        }

        public ActionResult ViewAllDocuments(int id, int cid, int yyyy)
        {
            string mergedPath = "~/Files/MergedDocuments/Merged_" + id.ToString() + "_" + cid.ToString() + "_" + yyyy.ToString() + ".pdf";
            string savePath = Server.MapPath(mergedPath);

            FileInfo fInfo = new FileInfo(savePath);
            if (fInfo.Exists)
            {
                fInfo.Delete();
            }

            var documents = db.DocumentFiles.Where(x => x.ContractorID == cid && x.Year == yyyy && x.DateRejected == null).ToList();
            if (id == 0)
            {
                documents = documents.Where(x => x.ProjectID == 0).ToList();
            }
            else
            {
                documents = documents.Where(x => x.ProjectID == id || x.ProjectID == 0).ToList();
            }

            // Sort by document type
            foreach (var d in documents)
            {
                if (d.DocumentType == "PrevPerf")
                    d.Month = -8;
                else if (d.DocumentType == "NonSeg")
                    d.Month = -7;
                else if (d.DocumentType == "GFE")
                    d.Month = -6;
                else if (d.DocumentType == "ListSub")
                    d.Month = -3;
                else if (d.DocumentType == "NtceEEO")
                    d.Month = -2;
                else if (d.DocumentType.Contains("WIBA"))
                    d.Month = -1;
            }

            //Analysis Close
            if (id > 0)
            {
                string path = "~/Files/AnalysisClose/";
                string serverPath = Server.MapPath(path);
                string analysisFile = "AnalysisClose_" + id.ToString() + ".pdf";
                string fullPath = CommonHelper.CreateAnalysisClose(id);

                if (fullPath.Contains(analysisFile))
                {
                    documents.Add(new DocumentFile
                    {
                        Month = -5,
                        FilePath = path + analysisFile
                    });
                }
            }

            //Clearance Request
            if (id > 0)
            {
                string crPath = Server.MapPath("~/Files/Clearance_Request/Forms/") + "ClearanceRequest_" + id.ToString() + ".PDF";
                FileInfo crInfo = new FileInfo(crPath);
                if (crInfo.Exists)
                {
                    documents.Add(new DocumentFile
                    {
                        Month = -4,
                        FilePath = "~/Files/Clearance_Request/Forms/" + crInfo.Name
                    });
                }
            }

            documents = documents.OrderBy(x => x.Month).ToList();

            using (Stream outputPdfStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                iTextSharp.text.Document doc = new iTextSharp.text.Document();
                PdfCopy copy = new PdfCopy(doc, outputPdfStream);
                //copy.SetMergeFields();
                doc.Open();
                PdfReader reader;
                int totalPageCnt = 0;
                foreach (var d in documents)
                {
                    string file = Server.MapPath(d.FilePath);
                    FileInfo fi = new FileInfo(file);

                    if (fi.Exists && file.ToLower().EndsWith(".pdf"))
                    {
                        reader = new PdfReader(file);
                        totalPageCnt = reader.NumberOfPages;
                        for (int pageCnt = 0; pageCnt < totalPageCnt;)
                        {
                            copy.AddPage(copy.GetImportedPage(reader, ++pageCnt));
                        }
                        copy.FreeReader(reader);
                    }
                }

                if (totalPageCnt > 0)
                {
                    doc.Close();
                }
                else
                {
                    TempData["Message"] = "Error: Documents cannot be merged because documents are not in PDF format.";
                    return Redirect(Request.UrlReferrer.ToString());
                }
            }

            return Redirect(mergedPath);
        }

        public ActionResult ViewAllInspectionDocuments(int id, bool svc = false)
        {
            string mergedPath = "~/Files/MergedDocuments/Merged_" + id.ToString() + ".pdf";
            string savePath = Server.MapPath(mergedPath);

            using (Stream outputPdfStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                iTextSharp.text.Document doc = new iTextSharp.text.Document();
                PdfSmartCopy copy = new PdfSmartCopy(doc, outputPdfStream);
                doc.Open();
                int totalPageCnt = 0;
                PdfReader reader;
                PdfStamper stamper;
                string[] fieldNames;

                if (svc)
                {
                    string filePath = Server.MapPath("~/Files/Site_Inspection/Forms/SiteVisitCompletion/") + "SiteVisitCompletion_" + id.ToString() + ".pdf";
                    FileInfo fi = new FileInfo(filePath);

                    if (fi.Exists)
                    {
                        reader = new PdfReader(filePath);
                        totalPageCnt = reader.NumberOfPages;
                        for (int pageCnt = 0; pageCnt < totalPageCnt;)
                        {
                            //have to create a new reader for each page or PdfStamper will throw error
                            reader = new PdfReader(filePath);
                            stamper = new PdfStamper(reader, outputPdfStream);
                            fieldNames = new string[stamper.AcroFields.Fields.Keys.Count];

                            copy.AddPage(copy.GetImportedPage(reader, ++pageCnt));
                        }
                        copy.FreeReader(reader);
                    }
                }

                var documents = db.Documents.Where(x => x.InspectionID == id && x.DocumentName != "Site Visit Completion")
                    .OrderBy(x => x.DateUploaded).ToList();
                foreach (var d in documents)
                {
                    string file = Server.MapPath(d.Path + d.FileName);
                    FileInfo fi = new FileInfo(file);

                    if (fi.Exists && file.ToLower().EndsWith(".pdf"))
                    {
                        reader = new PdfReader(file);
                        totalPageCnt = reader.NumberOfPages;
                        for (int pageCnt = 0; pageCnt < totalPageCnt;)
                        {
                            //have to create a new reader for each page or PdfStamper will throw error
                            reader = new PdfReader(file);
                            stamper = new PdfStamper(reader, outputPdfStream);
                            fieldNames = new string[stamper.AcroFields.Fields.Keys.Count];

                            copy.AddPage(copy.GetImportedPage(reader, ++pageCnt));
                        }
                        copy.FreeReader(reader);
                    }
                }

                if (totalPageCnt == 0)
                {
                    TempData["Message"] = "Error: Documents cannot be merged because documents are not in PDF format or have been deleted.";
                    return View("Error");

                }
                else
                {
                    doc.Close();
                }
            }

            return Redirect(mergedPath);
        }

        public JsonResult DocumentReceivedByType(FormCollection form)
        {
            DateTime dt1 = DateTime.MinValue;
            DateTime dt2 = DateTime.MaxValue;

            DateTime.TryParse(form["dateFrom"], out dt1);
            DateTime.TryParse(form["dateTo"], out dt2);

            var list = db.DocumentFiles.Where(x => x.DateUploaded >= dt1 && x.DateUploaded <= dt2)
                .GroupBy(x => x.DocumentType).Select(g => new { DocumentType = g.Key, Count = g.Count() }).ToList();

            var documentListHtml = list.Count > 0
                ? GetDocumentTable(list)
                : "<div class='content text-right'><h5>No data is available for selected date range.</h5></div>";

            return Json(new
            {
                success = true,
                listhtml = documentListHtml
            });
        }

        private string GetDocumentTable(IEnumerable<dynamic> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table class='table'>");

            string row1 = "<thead><tr>";
            string row2 = "<tbody><tr>";
            int total = 0;
            foreach (dynamic item in list)
            {
                string documentType = item.DocumentType;
                row1 += "<th class='text-center'>" + documentType + "</th>";
                int count = item.Count;
                row2 += "<td class='text-center'>" + count + "</td>";
                total += count;
            }

            sb.Append(row1 + "<th class='text-right'>Total</th></tr></thead>");
            sb.Append(row2 + "<td class='text-right'>" + total + "</td></tr></tbody>");
            sb.Append("</table>");

            return sb.ToString();
        }
    }
}
