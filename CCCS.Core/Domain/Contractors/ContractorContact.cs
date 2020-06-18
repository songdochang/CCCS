using System;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Contractors
{
    public class ContractorContact: BaseEntity
    {
        public int ContractorId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        public string Extension { get; set; }
        public DateTime? DateModified { get; set; }

    }
}
