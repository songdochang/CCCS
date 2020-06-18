using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CCCS.Infrastructure
{
    public static class IdentityHelpers
    {
        public static MvcHtmlString GetUserName(this HtmlHelper html, string id)
        {
            ApplicationUserManager mgr = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();

            if (mgr.FindByIdAsync(id).Result != null)
                return new MvcHtmlString(mgr.FindByIdAsync(id).Result.UserName);
            else
                return new MvcHtmlString("");
        }
    }
}
