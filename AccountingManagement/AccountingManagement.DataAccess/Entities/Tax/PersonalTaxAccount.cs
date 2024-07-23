using System;
using System.Collections.Generic;

namespace AccountingManagement.DataAccess.Entities
{
    public class PersonalTaxAccount
    {
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        public Owner Owner { get; set; }

        public PersonalTaxType TaxType { get; set; }

        public string TaxNumber { get; set; }

        public string TaxYear { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        public PersonalTaxFilingProgress TaxFilingProgress { get; set; }

        public bool Step1Completed { get; set; }

        public bool Step2Completed { get; set; }

        public bool Step3Completed { get; set; }

        public bool Step4Completed { get; set; }

        public bool IsHighPriority { get; set; }
        public bool IsInProgress { get; set; } //RYAN: new column added
        public bool IsActive { get; set; }

        public bool InstalmentRequired { get; set; } //RYAN: new column added
        public decimal InstalmentAmount { get; set; } //RYAN: new column added
        public DateTime? InstalmentDueDate { get; set; } //RYAN: new column added


        public List<PersonalTaxAccountLog> PersonalTaxAccountLogs { get; set; }

        public PersonalTaxAccount()
        {
            TaxFilingProgress = PersonalTaxFilingProgress.None;
            Step1Completed = false;
            Step2Completed = false;
            Step3Completed = false;
            Step4Completed = false;
            IsHighPriority = false;
            IsInProgress = false;
            InstalmentRequired = false; //RYAN: need this ???
        }
    }
}
