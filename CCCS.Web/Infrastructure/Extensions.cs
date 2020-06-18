using CCCS.Infrastructure;
using CCCS.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Microsoft.AspNet.Identity.Owin;
using System.Web.Routing;
using CCCS.Data;

namespace CCCS
{
    public static class Extensions
    {
        public static MvcHtmlString DepartmentDropDownList(this HtmlHelper helper, string id, object htmlAttributes)
        {
            using (ContractContext db = new ContractContext())
            {
                List<SelectListItem> list = db.Departments.OrderBy(x => x.DepartmentName)
                    .Select(x => new SelectListItem
                    {
                        Text = x.DepartmentName,
                        Value = x.DepartmentId
                    }).ToList();

                list.Insert(0, new SelectListItem { Text = "- Select Department - ", Value = "" });

                return helper.DropDownList(id, list, htmlAttributes);
            }
        }

        public static MvcHtmlString DepartmentDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> helper, 
            Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            using (ContractContext db = new ContractContext())
            {
                List<SelectListItem> list = db.Departments.OrderBy(x => x.DepartmentName)
                .Select(x => new SelectListItem
                {
                    Text = x.DepartmentName,
                    Value = x.DepartmentId
                }).ToList();

                list.Insert(0, new SelectListItem { Text = "- Select Department -", Value = "" });

                return helper.DropDownListFor(expression, list, htmlAttributes);
            }
        }

        public static MvcHtmlString DcoDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> helper,
            Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            using (ContractContext db = new ContractContext())
            {
                ApplicationRole role = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO");
                string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();

                List<SelectListItem> list = db.UserProfiles
                                            .Where(x => x.IsActive && memberIDs.Any(y => y == x.UserID))
                                            .OrderBy(x => x.FullName)
                                            .Select(x => new SelectListItem
                                            {
                                                Text = x.FullName,
                                                Value = x.UserInitial
                                            }).ToList();

                list.Insert(0, new SelectListItem { Text = "- Select DCO -", Value = "" });

                return helper.DropDownListFor(expression, list, htmlAttributes);
            }
        }

        public static MvcHtmlString DcoDropDownList(this HtmlHelper helper, string id, bool selectDCO, string selectedValue, object htmlAttributes)
        {

            using (ContractContext db = new ContractContext())
            {
                ApplicationRole role = RoleManager.Roles.FirstOrDefault(x => x.Name == "DCO");
                string[] memberIDs = role.Users.Select(x => x.UserId).ToArray();

                List<SelectListItem> list = db.UserProfiles
                                            .Where(x => x.IsActive && memberIDs.Any(y => y == x.UserID))
                                            .OrderBy(x => x.FullName)
                                            .Select(x => new SelectListItem
                                            {
                                                Text = x.FullName,
                                                Value = x.UserInitial
                                            }).ToList();

                if (selectDCO)
                {
                    list.Insert(0, new SelectListItem { Text = "- Select CO -", Value = "" });
                }
                else
                {
                    list.Insert(0, new SelectListItem { Text = "- All COs -", Value = "" });
                }

                if (!string.IsNullOrEmpty(selectedValue))
                {
                    var selected = list.Where(x => x.Value == selectedValue).First();
                    selected.Selected = true;
                }

                return helper.DropDownList(id, list, htmlAttributes);
            }
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            string imgUrl = UrlHelper.GenerateUrl(null, actionName, controllerName, routeValues, helper.RouteCollection, helper.ViewContext.RequestContext, false);

            var builder = new TagBuilder("img");
            builder.MergeAttributes<string, object>(htmlAttributes);
            builder.MergeAttribute("src", imgUrl);

            return MvcHtmlString.Create(builder.ToString());
        }

        public static string RemoveSpecialCharacters(this string str)
        {
            char[] specialCharacters = { '\'', ',', '-', ' ', '.' };

            foreach(var c in specialCharacters)
                str = str.Replace(c, '_');

            return str;
        }

        private static ApplicationRoleManager RoleManager
        {
            get
            {
                return HttpContext.Current.GetOwinContext().GetUserManager<ApplicationRoleManager>();
            }
        }

        public static MvcHtmlString ShortString(this HtmlHelper helper, string input, int length)
        {
            if (String.IsNullOrEmpty(input))
                input = "";

            if (input.Length > length)
            {
                string substring = input.Substring(0, length);
                var builder = new TagBuilder("span");
                builder.MergeAttribute("data-toggle", "tooltip");
                builder.MergeAttribute("data-placement", "top");
                builder.MergeAttribute("title", input);

                builder.InnerHtml = substring + "<text>...</text>";

                return MvcHtmlString.Create(builder.ToString());
            }
            else
            {
                return MvcHtmlString.Create(input);
            }
        }
    }
}