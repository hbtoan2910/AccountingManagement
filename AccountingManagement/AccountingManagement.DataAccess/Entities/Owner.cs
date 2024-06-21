using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingManagement.DataAccess.Entities
{
    public class Owner
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string SIN { get; set; }

        public DateTime? DOB { get; set; }

        public string Address { get; set; }

        public string Address2 { get; set; }

        public string PhoneNumber { get; set; }

        public string PhoneNumber2 { get; set; }

        public string Email { get; set; }

        public string Notes { get; set; }

        public List<BusinessOwner> BusinessOwners { get; set; }

        public List<PersonalTaxAccount> PersonalTaxAccounts { get; set; }

        public PersonalTaxAccount T1Account =>
            PersonalTaxAccounts?.FirstOrDefault(x => x.TaxType == PersonalTaxType.T1);

        public bool HasT1Account => T1Account?.IsActive == true;

        public bool IsDeleted { get; set; }

        public Owner()
        {
            IsDeleted = false;
        }
    }
}
