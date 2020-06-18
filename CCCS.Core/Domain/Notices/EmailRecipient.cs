using System;

namespace CCCS.Core.Domain.Notices
{
    public class EmailRecipient: BaseEntity
    {
        public EmailRecipient()
        {
            this.DateRegistered = DateTime.Now;
        }

        public string DepartmentId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public DateTime DateRegistered { get; set; }

        public string Requirements { get; set; }

    }
}
