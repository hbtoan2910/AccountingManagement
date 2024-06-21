using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class TaxAccountWithInstalment
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public TaxAccountType AccountType { get; set; }

        public string AccountNumber { get; set; }

        public FilingCycle Cycle { get; set; }

        public DateTime EndingPeriod { get; set; }

        public DateTime DueDate { get; set; }

        public string Notes { get; set; }

        public bool IsActive { get; set; }

        public UserAccount UserAccount { get; set; }

        public string ProgressNotes { get; set; }

        public Guid? UserAccountId { get; set; }

        public bool InstalmentRequired { get; set; }

        public DateTime? InstalmentDueDate { get; set; }

        public decimal InstalmentAmount { get; set; }

        public TaxAccountWithInstalment()
        {
            IsActive = true;
            InstalmentRequired = false;
        }
    }
}
