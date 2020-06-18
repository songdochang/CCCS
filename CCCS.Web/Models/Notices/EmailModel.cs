using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CCCS.Web.Models.Notices
{
    public class EmailModel
    {
        public string Title { get; set; }
        public string Action { get; set; }
        public int ProjectID { get; set; }
        public int InspectionID { get; set; }
        public string UserID { get; set; }
        public int DocumentID { get; set; }
        public string Department { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Body { get; set; }
        public string AlternateBody { get; set; }
        public string Month { get; set; }
        public string FundOrg { get; set; }
        public string ReturnUrl { get; set; }
        public bool AttachmentRequired { get; set; }
        public string Attached { get; set; }
        public string CompanyName { get; set; }

        public List<SelectListItem> Recipients { get; set; }
        public List<SelectListItem> ContractorContacts { get; set; }
        public List<SelectListItem> ProjectContacts { get; set; }
    }
}
