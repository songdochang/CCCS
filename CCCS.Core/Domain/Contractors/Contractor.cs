using CCCS.Core.Domain.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CCCS.Core.Domain.Contractors
{
    public class Contractor: BaseEntity
    {
        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Display(Name = "Address 1")]
        public string Address1 { get; set; }
        [Display(Name = "Address 2")]
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }

        [Display(Name = "Date Registered")]
        public DateTime DateRegistered { get; set; }
        public string DCO { get; set; }
        [Display(Name = "Alternate DCO")]
        public string AlternateDCO { get; set; }
        public string TaxId { get; set; }

        public virtual ICollection<Contract> Contracts { get; set; }
        public virtual ICollection<ContractorContact> ContractorContacts { get; set; }
        public virtual ICollection<Document> Documents { get; set; }

        public Contractor()
        {
        }
    }

}
