using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CCCS.Startup))]
namespace CCCS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
