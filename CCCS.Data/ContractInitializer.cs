using System;

namespace CCCS.Data
{
    public class ContractInitializer : System.Data.Entity.CreateDatabaseIfNotExists<ContractContext>
    {
        protected override void Seed(ContractContext context)
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", System.IO.Directory.GetCurrentDirectory());

            //var contractors = new List<Contractor>
            //{
            //    new Contractor{CompanyName="California Professional Engineering, Inc.",ContactName="Alexander",
            //        DateRegistered =DateTime.Parse("2015-05-22"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Dalke and Sons Construction, Inc.",ContactName="Alonso",
            //        DateRegistered =DateTime.Parse("2012-09-25"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Flatiron Electric Group, Inc.",ContactName="Anand",
            //        DateRegistered =DateTime.Parse("2013-08-08"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Martinez Landscaping Co., Inc.",ContactName="Barzdukas",
            //        DateRegistered =DateTime.Parse("2012-11-25"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Mowbray's Tree Service, Inc.",ContactName="Li",
            //        DateRegistered =DateTime.Parse("2012-10-02"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="MTM Construction, Inc.",ContactName="Justice",
            //        DateRegistered =DateTime.Parse("2011-09-01"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Select Electric, Inc.",ContactName="Ali Dana",
            //        DateRegistered =DateTime.Parse("2015-11-2"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Southwest Pipeline & Trenchless Corp.",ContactName="Ali Dana",
            //        DateRegistered =DateTime.Parse("2015-11-2"),DCO="DG",IsPrime=true},
            //    new Contractor{CompanyName="Trautwein Construction, Inc",ContactName="Gil Garcia",
            //        DateRegistered =DateTime.Parse("2015-04-13"),DCO="DG",IsPrime=true}
            //};

            //contractors.ForEach(s => context.Contractors.Add(s));
            //context.SaveChanges();

            //var projects = new List<Project>
            //{
            //    new Project{DCO="DG",JOC="1304AED-080.00",ProjectName="Downtown Mental Health Center Phase 1",Unit="7758090851",Phase=Phase.B,
            //        DateReceived =DateTime.Parse("2015-10-01"),PrimeContractorID=1},
            //    new Project{JOC="4022",Unit="Microeconomics",Phase=Phase.B,DateReceived=DateTime.Parse("2012-12-02")},
            //    new Project{JOC="4041",Unit="Macroeconomics",Phase=Phase.C,DateReceived=DateTime.Parse("2015-09-23")},
            //    new Project{JOC="1045",Unit="Calculus",Phase=null,DateReceived=DateTime.Parse("2015-09-01")},
            //    new Project{JOC="3141",Unit="Trigonometry",Phase=null,DateReceived=DateTime.Parse("2012-04-11")},
            //    new Project{JOC="2021",Unit="Composition",Phase=Phase.A,DateReceived=DateTime.Parse("2014-06-17")},
            //    new Project{DCO="GB",JOC="FCC0001241",ProjectName="San Gabriel Dam",Unit="4900090854",Phase=null,
            //        DateReceived =DateTime.Parse("2012-11-21"),PrimeContractorID=2}
            //};
            //projects.ForEach(s => context.Projects.Add(s));
            //context.SaveChanges();
        }
    }
}
