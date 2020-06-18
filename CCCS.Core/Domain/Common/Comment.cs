using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Core.Domain.Common
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int EntityId { get; set; }
        public string CommentText { get; set; }
        public string Category { get; set; }
        public string CommentedBy { get; set; }
        public DateTime DateRegistered { get; set; }
    }
}
