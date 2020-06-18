using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Web.Models.Worksheets
{
    public class ActivityModel
    {
        public string Code { get; set; }
        public string ActivityName { get; set; }
    }
}
