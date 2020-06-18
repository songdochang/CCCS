
using CCCS.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CCCS.Web.Models.Projects
{
    public class ProjectByFiscalYearModel
    {
        public int ProjectId { get; set; }
        public string JOC { get; set; }
        public string ProjectName { get; set; }
        [Display(Name = "Date Received")]
        public DateTime? DateReceived { get; set; }
        public string Phase { get; set; }
        public string Unit { get; set; }
        public string DCO { get; set; }
        public Decimal? ContractAmount { get; set; }
        public int PrimeContractorId { get; set; }
        public string PrimeContractorName { get; set; }
        public DateTime? DateClosed { get; set; }
        public string FiscalYear { get; set; }


        private ContractContext db = new ContractContext();

        public List<ProjectByFiscalYearModel> GetProjectsByFiscalYear(string fiscalYear)
        {
            var list = from p in db.ViewProjects
                       join f in db.ProjectFiscalYears on p.Id equals f.ProjectId into pf
                       from x in pf.DefaultIfEmpty()
                       select new ProjectByFiscalYearModel
                       {
                           ProjectId = p.Id,
                           JOC = p.JOC,
                           ProjectName = p.ProjectName,
                           DateReceived = p.DateReceived,
                           DateClosed = p.DateClosed,
                           Phase = p.Phase,
                           Unit = p.Unit,
                           DCO = p.DCO,
                           PrimeContractorId = p.PrimeContractorID,
                           PrimeContractorName = p.CompanyName,
                           ContractAmount = p.ContractAmount,
                           FiscalYear = x.FiscalYear
                       };

            var model = list.Where(x => x.FiscalYear == fiscalYear).ToList(); 

            return model;
        }

    }
}
