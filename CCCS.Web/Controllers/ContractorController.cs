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
using CCCS.Infrastructure;
using CCCS.Core.Domain.Contractors;
using CCCS.Core.Domain.Documents;
using CCCS.Web.Models.Contractors;
using CCCS.Web.Models.Documents;
using CCCS.Web.Models.Projects;
using CCCS.Core.Domain.Common;

namespace CCCS.Controllers
{
    [Authorize]
    public class ContractorController : BaseController
    {
        const int PAGE_SIZE = 12;
        protected static string CONTRACTOR__PROJECT_LIST_KEY = "CCCS_CONTRACTOR_PROJECT_LIST_{0}_{1}";

        //[OutputCache(Duration = 1200, VaryByParam = "*")]
        public ActionResult Index1(string sortOrder, string dco, int? page, string startsWith = "")
        {
            List<ContractorModel> model = new List<ContractorModel>();

            if (!CacheHelper.Get<List<ContractorModel>>(CONTRACTOR_LIST_KEY, out model))
            {
                model = GetContractorListModel();
                CacheHelper.Add<List<ContractorModel>>(model, CONTRACTOR_LIST_KEY, 60.0);
            }

            if (!String.IsNullOrEmpty(dco))
            {
                model = model.Where(x => x.DCO == dco).ToList();
                ViewBag.CurrentDCO = dco;
            }

            if (page == null)
            {
                ViewBag.CompanyNameSortParm = (string.IsNullOrEmpty(sortOrder) || sortOrder == "CompanyName_desc") ? "CompanyName" : "CompanyName_desc";
            }

            model = GetStartsWith(model, startsWith);

            ViewBag.SortOrder = sortOrder;

            switch (sortOrder)
            {
                case "CompanyName":
                    model = model.OrderBy(c => c.CompanyName).ToList();
                    break;
                case "CompanyName_desc":
                    model = model.OrderByDescending(c => c.CompanyName).ToList();
                    break;
                default:
                    model = model.OrderBy(c => c.CompanyName).ToList();
                    break;
            }

            ViewBag.COs = GetCOs();
            ViewBag.Total = model.Count;

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Index1(FormCollection form)
        {
            if (String.IsNullOrEmpty(form["submit"]))
                return RedirectToAction("Index1");
            else
                return RedirectToAction("Index1", new { startsWith = form["submit"] });
        }

        private List<ContractorModel> GetStartsWith(List<ContractorModel> model, string startsWith)
        {
            if (!String.IsNullOrEmpty(startsWith) && startsWith != "All")
            {
                List<ContractorModel> temp = new List<ContractorModel>();

                if (startsWith != "1-9")
                {
                    string[] ch = startsWith.Split('-');
                    int start = (int)ch[0][0];
                    int end = (ch.Length == 1) ? start : (int)ch[1][0];

                    for (int i = start; i <= end; i++)
                    {
                        string c = ((char)i).ToString();
                        var t = model.Where(x => x.CompanyName.StartsWith(c)).ToList();
                        temp.AddRange(t);
                    }
                }
                else
                {
                    for (int i = 1; i <= 9; i++)
                    {
                        var t = model.Where(x => x.CompanyName.StartsWith(i.ToString())).ToList();
                        temp.AddRange(t);
                    }
                }

                ViewBag.StartsWith = startsWith;
                model = temp;
            }

            return model;
        }

        public ActionResult Index2(string sortOrder, int? page, string startsWith = "")
        {
            int[] activeIDs = db.Projects.Where(x => x.DateClosed == null).Select(x => x.PrimeContractorID).ToArray();
            var model = db.Contractors.Where(x => activeIDs.Any(y => y == x.Id))
                .Select(x => new ContractorModel
                {
                    ContractorID = x.Id,
                    CompanyName = x.CompanyName,
                    DCO = x.DCO
                }).ToList();

            model = GetExtraContractorInfo(model).OrderBy(x => x.CompanyName).ToList();

            model = GetStartsWith(model, startsWith);

            if (page == null)
            {
                ViewBag.CompanyNameSortParm = (string.IsNullOrEmpty(sortOrder) || sortOrder == "CompanyName_desc") ? "CompanyName" : "CompanyName_desc";
            }

            ViewBag.SortOrder = sortOrder;

            switch (sortOrder)
            {
                case "CompanyName":
                    model = model.OrderBy(c => c.CompanyName).ToList();
                    break;
                case "CompanyName_desc":
                    model = model.OrderByDescending(c => c.CompanyName).ToList();
                    break;
                default:
                    model = model.OrderBy(c => c.CompanyName).ToList();
                    break;
            }

            ViewBag.COs = GetCOs();
            ViewBag.Total = model.Count;

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Index2(FormCollection form)
        {
            if (String.IsNullOrEmpty(form["submit"]))
                return RedirectToAction("Index2");
            else
                return RedirectToAction("Index2", new { startsWith = form["submit"] });
        }

        public ActionResult Index3(string sortOrder, int? page, string startsWith = "")
        {
            int[] activeIDs = db.Projects.Where(x => x.DateClosed == null).Select(x => x.PrimeContractorID).Distinct().ToArray();
            int[] primeContractorIDs = db.Projects.Select(x => x.PrimeContractorID).Distinct().ToArray();
            var model = db.Contractors.Where(x => primeContractorIDs.Any(y => y == x.Id) && !activeIDs.Any(z => z == x.Id))
                .Select(x => new ContractorModel
                {
                    ContractorID = x.Id,
                    CompanyName = x.CompanyName,
                    DCO = x.DCO
                }).ToList();

            model = GetExtraContractorInfo(model).OrderBy(x => x.CompanyName).ToList();

            model = GetStartsWith(model, startsWith);

            if (page == null)
            {
                ViewBag.CompanyNameSortParm = (string.IsNullOrEmpty(sortOrder) || sortOrder == "CompanyName_desc") ? "CompanyName" : "CompanyName_desc";
            }

            ViewBag.SortOrder = sortOrder;

            switch (sortOrder)
            {
                case "CompanyName":
                    model = model.OrderBy(c => c.CompanyName).ToList();
                    break;
                case "CompanyName_desc":
                    model = model.OrderByDescending(c => c.CompanyName).ToList();
                    break;
                default:
                    model = model.OrderBy(c => c.CompanyName).ToList();
                    break;
            }

            ViewBag.COs = GetCOs();
            ViewBag.Total = model.Count;

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Index3(FormCollection form)
        {
            if (String.IsNullOrEmpty(form["submit"]))
                return RedirectToAction("Index3");
            else
                return RedirectToAction("Index3", new { startsWith = form["submit"] });
        }

        public ActionResult Index4(string searchString, int? page)
        {
            var model = SearchContractor(searchString);

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }


        private List<ContractorModel> SearchContractor(string searchString)
        {
            searchString = searchString.ToLower();

            var model = db.Contractors.Where(x => x.CompanyName.ToLower().Contains(searchString) || x.TaxId.ToLower().Contains(searchString))
                        .Select(c => new ContractorModel
                        {
                            ContractorID = c.Id,
                            CompanyName = c.CompanyName,
                            DCO = c.DCO,
                            AlternateDCO = c.AlternateDCO,
                            TaxId = c.TaxId
                        }).ToList();

            model = GetExtraContractorInfo(model);

            return model;
        }



        // GET: Contractor/Details/5
        public ActionResult Details1(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contractor contractor = db.Contractors.Find(id);
            if (contractor == null)
            {
                return HttpNotFound();
            }
            ViewBag.ContactInfos = db.ContractorContacts.Where(x => x.ContractorId == id).ToList();

            return View(contractor);
        }

        public PartialViewResult ContractorStats(int id)
        {
            Contractor contractor = db.Contractors.Find(id);

            var projects = from p in db.Projects
                             join c in db.Contracts on p.Id equals c.ProjectId
                             where c.ContractorId == id && p.DateClosed == null
                             select p.Id;

            int mo = DateTime.Today.Month;
            int yyyy = (mo >= 7) ? DateTime.Today.Year : DateTime.Today.Year - 1;
            DateTime dt1 = DateTime.Parse("07/01/" + yyyy);
            DateTime dt2 = dt1.AddYears(1).AddDays(-1);

            var visits = db.Inspections.Where(x => x.DateOfVisit >= dt1 && x.DateOfVisit <= dt2 && x.DateSiteVisitCompletion != null && x.ContractorId == id);

            ContractorStats model = new ContractorStats
            {
                DCO = contractor.DCO,
                AlternateDCO = contractor.AlternateDCO,
                NumberProjects = projects.Count(),
                FySiteVisits = visits.Count(),
                DateRegistered = (contractor.DateRegistered != null) ? contractor.DateRegistered.ToString("MM-dd-yyyy") : ""
            };

            return PartialView("_ContractorStats", model);
        }

        // GET: Contractor/Details2/5
        public ActionResult Details2(int? id, int? page, string message = "")
        {
            var model = (from n in db.NonCompliances
                         join p in db.Projects on n.ProjectID equals p.Id
                         join c in db.Contractors on n.ContractorID equals c.Id
                         where c.Id == id
                         orderby p.StartDate, n.DocumentType, p.ProjectName, n.Year, n.Month
                         select new NonComplianceModel
                         {
                             ID = n.ID,
                             ProjectID = p.Id,
                             JOC = p.JOC,
                             ProjectName = p.ProjectName,
                             StartDate = p.StartDate,
                             EndDate = p.EndDate,
                             Year = n.Year,
                             Month = n.Month,
                             DocumentType = n.DocumentType,
                             DateRequired = n.DateRequired,
                             DateReceived = n.DateReceived
                         }).ToList();

            foreach (var m in model)
            {
                DateTime asof = (m.DateReceived == null) ? DateTime.Now : (DateTime)m.DateReceived;
                if (m.DateRequired != null)
                {
                    var dt = (DateTime)m.DateRequired.Value;
                    m.PastDueDays = (asof - dt).Days;
                    m.PastDueMonths = m.PastDueDays / (365 / 12);
                }
            }

            ViewBag.CompanyName = db.Contractors.Find(id).CompanyName;
            ViewBag.TotalPastDue = model.Where(x => x.DateReceived == null).Count();

            int pageSize = PAGE_SIZE;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Details2(int id)
        {
            string dateReceived = Request.Form["DateReceived"];

            int projectId = int.Parse(Request.Form["ProjectID"]);
            string column = Request.Form["Column"];
            int year = int.Parse(Request.Form["Year"]);

            var row = db.DocumentRows.FirstOrDefault(x => x.ProjectID == projectId && x.ContractorID == id && x.Year == year);

            if (row == null && !string.IsNullOrEmpty(dateReceived))
            {
                DocumentRow dr = new DocumentRow
                {
                    ProjectID = projectId,
                    ContractorID = id,
                    Year = year
                };

                dr.GetType().GetProperty(column).SetValue(dr, DateTime.Parse(dateReceived));

                db.DocumentRows.Add(dr);
            }
            else
            {
                if (string.IsNullOrEmpty(dateReceived))
                {
                    row.GetType().GetProperty(column).SetValue(row, null);
                }
                else
                {
                    row.GetType().GetProperty(column).SetValue(row, DateTime.Parse(dateReceived));
                }

                db.Entry(row).State = EntityState.Modified;
            }

            db.SaveChanges();

            return RedirectToAction("Details2");
        }

        public PartialViewResult Details3(string sortOrder, int id, string searchString, int? page)
        {
            List<ProjectModel> model;
            ProjectModel pm = new ProjectModel();

            string key = String.Format(CONTRACTOR__PROJECT_LIST_KEY, id, "OPEN");
            if (!CacheHelper.Get<List<ProjectModel>>(key, out model))
            {
                model = pm.GetProjectModelByContractor(id,false);
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                string str = searchString.ToLower();
                model = model.Where(x => x.JOC.ToLower().Contains(str)).ToList();
                ViewBag.SearchString = searchString;
            }

            ViewBag.ContractorID = id;
            ViewBag.CompanyName = db.Contractors.Find(id).CompanyName;
            ViewBag.Total = model.Count;

            model = SortModel(model, sortOrder, page);

            int pageNumber = (page ?? 1);

            var pagedModel = model.ToPagedList(pageNumber, PAGE_SIZE);
            pagedModel = CommonHelper.GetExtraInfo(pagedModel);

            return PartialView(pagedModel);
        }

        public ActionResult ResetDetails3(FormCollection form, int id)
        {
            return RedirectToAction("Details3", new { id = id });
        }

        public PartialViewResult Details4(string sortOrder, int id, string searchString, int? page)
        {
            List<ProjectModel> model;
            ProjectModel pm = new ProjectModel();

            string key = String.Format(CONTRACTOR__PROJECT_LIST_KEY, id, "CLOSED");
            if (!CacheHelper.Get<List<ProjectModel>>(key, out model))
            {
                model = pm.GetProjectModelByContractor(id, true);
                CacheHelper.Add<List<ProjectModel>>(model, key, 60.0);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                string str = searchString.ToLower();
                model = model.Where(x => x.JOC.ToLower().Contains(str)).ToList();
                ViewBag.SearchString = searchString;
            }

            ViewBag.ContractorID = id;
            ViewBag.CompanyName = db.Contractors.Find(id).CompanyName;
            ViewBag.Total = model.Count;

            model = SortModel(model, sortOrder, page);

            int pageNumber = (page ?? 1);

            var pagedModel = model.ToPagedList(pageNumber, PAGE_SIZE);
            pagedModel = CommonHelper.GetExtraInfo(pagedModel);

            return PartialView(pagedModel);
        }

        public ActionResult ResetDetails4(FormCollection form, int id)
        {
            return RedirectToAction("Details4", new { id = id });
        }

        public ActionResult Details5(int id)
        {
            var contractor = db.Contractors.Find(id);

            int regYear = contractor.DateRegistered.Year;
            int endYear = DateTime.Today.Year;
            int startYear = (regYear > endYear - 5) ? regYear : endYear - 5;

            // new contractor may have old projects
            var firstProject = db.Contracts.Where(x => x.ContractorId == id).OrderBy(x=> x.StartDate).FirstOrDefault();
            if (firstProject != null && firstProject.StartDate != null)
            {
                startYear = (firstProject.StartDate.Value.Year < startYear) ? firstProject.StartDate.Value.Year : startYear;
                startYear = (startYear < endYear - 5) ? endYear - 5 : startYear;
            }

            DocumentRowModel model = new DocumentRowModel
            {
                ContractorID = id,
                CompanyName = contractor.CompanyName,
                StartYear = startYear,
                EndYear = endYear,
                DocumentRows = new List<DocumentRow>()
            };

            var files = db.DocumentFiles.Where(x => x.ContractorID == id && x.DocumentType != "ListSub").ToList();

            for (int i = 0; i <= endYear - startYear; i++)
            {
                int year = startYear + i;

                DocumentRow row = new DocumentRow
                {
                    ContractorID = id,
                    Year = year
                };

                foreach (var f in files)
                {
                    if (f.Year == year)
                    {
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

                                DocumentCell cell = new DocumentCell { Name = p.Name, Status = status };
                                row.GetType().GetProperty(p.Name).SetValue(row, cell);
                            }
                        }
                    }
                }

                model.DocumentRows.Add(row);
            }

            ViewBag.CompanyName = contractor.CompanyName;

            return View(model);
        }

        [HttpPost]
        public ActionResult Details5(int id, string submit)
        {
            string dateReceived = (string.IsNullOrEmpty(Request.Form["DateReceived"])) ? DateTime.Today.ToShortDateString() : Request.Form["DateReceived"];

            string type = Request.Form["Column"];
            int year = int.Parse(Request.Form["Year"]);
            int month = (string.IsNullOrEmpty(Request.Form["Month"])) ? 0 : int.Parse(Request.Form["Month"]);

            if (submit == "Reset")
            {
                var docFiles = db.DocumentFiles.Where(x => x.ContractorID == id
                        && x.Year == year && x.Month == month && x.DocumentType == type);

                foreach (var df in docFiles)
                {
                    db.DocumentFiles.Remove(df);
                }
                db.SaveChanges();

                return RedirectToAction("Details4", new { id = id });
            }

            var existingDoc = db.DocumentFiles.Where(x => x.DocumentType == type && x.ContractorID == id
                && x.Year == year && x.DateApproved == null && x.DateRejected == null).ToList();
            if (type == "EUR" || type == "WIBA")
            {
                existingDoc = existingDoc.Where(x => x.Month == month).ToList();
            }
            if (existingDoc.Count > 0)
            {
                db.DocumentFiles.Remove(existingDoc.First());
                db.SaveChanges();
            }

            DocumentFile newFile = new DocumentFile
            {
                DateReceived = dateReceived,
                ContractorID = id,
                Year = year,
                DocumentType = type
            };

            if (type == "EUR" || type == "WIBA")
            {
                newFile.Month = month;
            }

            db.DocumentFiles.Add(newFile);
            db.SaveChanges();

            string message = "";
            if (Request.Files.Count > 0)
            {
                var upload = Request.Files[0];

                if (upload != null && upload.ContentLength > 0)
                {
                    try
                    {
                        var fileName = string.Concat(id.ToString(), "_", id, "_", Path.GetFileName(upload.FileName));
                        string relativePath = string.Concat("~/Files/", GetPath(type));
                        var pathName = string.Concat(Server.MapPath("~/Files/"), GetPath(type));
                        var path = Path.Combine(pathName, fileName);
                        upload.SaveAs(path);

                        DocumentFile file = db.DocumentFiles.FirstOrDefault(x => x.Id == newFile.Id);

                        file.FileName = fileName;
                        file.FilePath = relativePath + fileName;
                        file.DateUploaded = DateTime.Now;

                        db.Entry(file).State = EntityState.Modified;
                        db.SaveChanges();

                        TempData["Message"] = "File was uploaded successfully.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = "Error: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Details5", new { id = id });
        }

        public ActionResult Details6(int id)
        {
            int[] Ids = db.Contracts.Where(x => x.SubTo == id).Select(x => x.ContractorId).ToArray();
            var model = db.Contractors.Where(x => Ids.Any(y => y == x.Id)).OrderBy(x => x.CompanyName).ToList();

            ViewBag.ContractorID = id;
            ViewBag.CompanyName = db.Contractors.Find(id).CompanyName;

            return View(model);
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


        // GET: Contractor/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Contractor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Contractor contractor, string returnUrl = "")
        {
            var c = db.Contractors.FirstOrDefault(x => x.CompanyName == contractor.CompanyName);
            if (c != null)
            {
                TempData["Message"] = "Error: Another contractor with the entered company name exists.  Please enter a distinct name.";
                return View(contractor);
            }

            if (ModelState.IsValid)
            {
                contractor.DateRegistered = DateTime.Now;

                db.Contractors.Add(contractor);
                db.SaveChanges();

                List<ContractorModel> model = GetContractorListModel();
                CacheHelper.Clear(CONTRACTOR_LIST_KEY);
                CacheHelper.Add<List<ContractorModel>>(model, CONTRACTOR_LIST_KEY, 60.0);

                if (string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction("Index1");
                }
                else
                {
                    return Redirect(returnUrl);
                }
            }

            return View(contractor);
        }

        [HttpPost]
        public JsonResult CreateSubcontractor(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                Contractor contractor = new Contractor
                {
                    CompanyName = form["CompanyName"],
                    Address1 = form["Address1"],
                    Address2 = form["Address2"],
                    City = form["City"],
                    State = form["State"],
                    Zip = form["Zip"],
                    DateRegistered = DateTime.Now
                };

                db.Contractors.Add(contractor);
                db.SaveChanges();

                int contractorId = contractor.Id;
                string phone = form["Phone"];
                db.ContractorContacts.Add(new ContractorContact
                {
                    ContractorId = contractorId,
                    Name = contractor.CompanyName,
                    PhoneNumber = phone
                });
                db.SaveChanges();

                object newContractor = new
                {
                    ContractorID = contractorId,
                    CompanyName = contractor.CompanyName,
                    Address1 = contractor.Address1,
                    Address2 = contractor.Address2,
                    City = contractor.City,
                    State = contractor.State,
                    Zip = contractor.Zip,
                    Phone = phone
                };

                return Json(newContractor);
            }

            return null;
        }

        // GET: Contractor/Edit/5
        public ActionResult Edit(int? id, string returnUrl = "")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contractor contractor = db.Contractors.Find(id);
            if (contractor == null)
            {
                return HttpNotFound();
            }

            ViewBag.COs = GetCOs();

            ViewBag.ReturnUrl = returnUrl;

            return View(contractor);
        }

        // POST: Contractor/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FormCollection form, Contractor contractor, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                contractor.DCO = form["DCO"];

                db.Entry(contractor).State = EntityState.Modified;
                db.SaveChanges();

                if (CacheHelper.Exists(CONTRACTOR_LIST_KEY))
                {
                    List<ContractorModel> model;
                    CacheHelper.Get<List<ContractorModel>>(CONTRACTOR_LIST_KEY, out model);

                    var ct = model.FirstOrDefault(x => x.ContractorID == contractor.Id);
                    if (ct != null)
                    {
                        ct.CompanyName = contractor.CompanyName;
                    }
                }

                if (string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction("Details1", new { id = contractor.Id });
                }
                else
                {
                    return Redirect(returnUrl);
                }
            }

            return View(contractor);
        }

        // GET: Contractor/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contractor contractor = db.Contractors.Find(id);
            if (contractor == null)
            {
                return HttpNotFound();
            }
            return View(contractor);
        }

        // POST: Contractor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Contractor contractor = db.Contractors.Find(id);
            db.Contractors.Remove(contractor);
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

        private void GetViewBags()
        {
            ViewBag.COs = GetCOs();
        }

        #region Contractor Contacts

        public ActionResult CreateContact(int id)
        {
            ContractorContact model = new ContractorContact
            {
                ContractorId = id,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateContact(ContractorContact model)
        {
            if (ModelState.IsValid)
            {
                db.ContractorContacts.Add(model);
                db.SaveChanges();

                return RedirectToAction("Details1", "Contractor", new { id = model.ContractorId });
            }
            else
            {
                return View(model);
            }
        }

        public ActionResult DeleteContact(int id, string returnUrl = "")
        {
            ContractorContact model = db.ContractorContacts.Find(id);
            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        [HttpPost]
        public ActionResult DeleteContact(int id)
        {
            ContractorContact ci = db.ContractorContacts.Find(id);

            db.ContractorContacts.Remove(ci);
            db.SaveChanges();

            return RedirectToAction("Details1", "Contractor", new { id = ci.ContractorId });
        }

        public ActionResult EditContact(int id, string returnUrl = "")
        {
            ContractorContact model = db.ContractorContacts.Find(id);
            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        [HttpPost]
        public ActionResult EditContact(FormCollection form, ContractorContact model)
        {
            if (ModelState.IsValid)
            {
                model.DateModified = DateTime.Now;

                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details1", "Contractor", new { id = model.ContractorId });
            }
            else
            {
                return View(model);
            }
        }

        public ActionResult AddSubcontractor(int id)
        {
            Contract model = new Contract();

            var list = new List<ListItem>();
            list.Add(new ListItem { Text = "- Select a contractor -", Value = "" });

            foreach (var c in db.Contractors.OrderBy(x => x.CompanyName))
            {
                list.Add(new ListItem { Text = c.CompanyName, Value = c.Id.ToString() });
            }
            ViewBag.Contractors = list;
            ViewBag.ContractorID = id;

            return View(model);
        }

        [HttpPost]
        public ActionResult AddSubcontractor(FormCollection form, int id)
        {
            string contId = form["Contractors"];

            if (!string.IsNullOrEmpty(contId))
            {
                Contract contract = new Contract
                {
                    ContractorId = int.Parse(contId),
                    SubTo = id,
                    DateRegistered = DateTime.Now
                };

                db.Contracts.Add(contract);
                db.SaveChanges();
            }

            return RedirectToAction("Details5", new { id = id });
        }

        #endregion

        #region Comments

        public PartialViewResult Comments(int id)
        {
            var model = db.Comments.Where(x => x.Category == "Contractor" && x.EntityId == id).ToList();

            ViewBag.ContractorId = id;
            ViewBag.User = UserProfile.UserInitial;

            return PartialView("_Comments", model);
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

            return RedirectToAction("Details1", "Contractor", new { id = id });
        }

        [HttpPost]
        public ActionResult EditComment(int id, string category = "")
        {
            int commentId = int.Parse(Request.Form["CommentID"]);

            Comment comment = db.Comments.Find(commentId);
            comment.CommentText = Request.Form["EditComment"];
            db.Entry(comment).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Details1", "Contractor", new { id = id });
        }

        public ActionResult DeleteComment(int id, int cId)
        {
            var comment = db.Comments.Find(cId);
            db.Comments.Remove(comment);
            db.SaveChanges();

            return RedirectToAction("Details1", "Contractor", new { id = id });
        }

        #endregion
    }
}
