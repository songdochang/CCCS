using Autofac;
using CCCS.Core.Data;
using CCCS.Data;
using System.Configuration;

namespace CCCS.Web.Framework
{
    public class DependencyRegistrar
    {
        public virtual void Register()
        {
            var builder = new ContainerBuilder();

            //if (HttpContext.Current != null)
            //    builder.Register(c => new HttpContextWrapper(HttpContext.Current) as HttpContextBase).As<HttpContextBase>().InstancePerLifetimeScope();

            //controllers
            //builder.RegisterControllers(typeof(MvcApplication).Assembly);

            string cnnString = ConfigurationManager.ConnectionStrings["ContractContext"].ConnectionString;
            builder.Register<IDbContext>(c => new CccsObjectContext(cnnString)).InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(EfRepository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
            //builder.RegisterType<MemoryCacheManager>().As<ICacheManager>().SingleInstance();

            //builder.RegisterType<VehicleService>().As<IVehicleService>().InstancePerLifetimeScope();

            //var container = builder.Build();

            //DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            //GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver((IContainer)container);
        }
    }
}
