using CCCS.Controllers;
using CCCS.Core.Domain.Reference;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CCCS.Web.Controllers
{
    [Authorize]
    public class ManageController : BaseController
    {
        // GET: Manage
        public ActionResult Index()
        {
            var model = db.Activities.ToList();

            return View(model);
        }

        // GET: Manage/Create
        public ActionResult CreateActivity()
        {
            return View();
        }

        // POST: Manage/Create
        [HttpPost]
        public ActionResult CreateActivity(Activity model)
        {
            try
            {
                db.Activities.Add(model);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Manage/Edit/5
        public ActionResult EditActivity(int id)
        {
            var model = db.Activities.FirstOrDefault(x => x.ID == id);

            return View(model);
        }

        // POST: Manage/Edit/5
        [HttpPost]
        public ActionResult EditActivity(int id, FormCollection form)
        {
            try
            {
                var activity = db.Activities.FirstOrDefault(x => x.ID == id);
                activity.Description = form["Description"];
                activity.FundOrg = form["FundOrg"];
                activity.ActivityCode = form["ActivityCode"];
                activity.IsActive = (form["IsActive"].Contains("true")) ? true : false;

                db.Entry(activity).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;

                return View();
            }
        }

        // GET: Manage/Delete/5
        public ActionResult DeleteActivity(int id)
        {
            try
            {
                var activity = db.Activities.FirstOrDefault(x => x.ID == id);
                db.Activities.Remove(activity);

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
