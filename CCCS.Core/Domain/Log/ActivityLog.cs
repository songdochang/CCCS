using System;

namespace CCCS.Core.Domain.Log
{
    public class ActivityLog: BaseEntity
    {
        public DateTime ActivityDate { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public string Entity { get; set; }
        public string ExecutedBy { get; set; }
        public int? ProjectId { get; set; }
    }
}
