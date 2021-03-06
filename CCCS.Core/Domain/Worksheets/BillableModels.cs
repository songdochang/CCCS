﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Worksheets
{
    public class BillableListModel
    {
        public int WorksheetID { get; set; }
        [Display(Name = "Work Date")]
        public DateTime WorkDate { get; set; }
        public string DCO { get; set; }
        [Display(Name = "Activity Code")]
        public string ActivityCode { get; set; }
        public string ActivityDescription { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public string FormattedHours { get; set; }
        public string Comment { get; set; }
    }

    public class ActivityModel
    {
        public string Code { get; set; }
        public string ActivityName { get; set; }
    }
}
