using System;

namespace CCCS.Core.Domain.Common
{
    public class Cache: BaseEntity
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
