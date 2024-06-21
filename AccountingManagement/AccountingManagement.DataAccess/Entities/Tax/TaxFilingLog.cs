using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class TaxFilingLog
    {
        public int Id { get; set; }

        public Guid TaxAccountId { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public TaxAccountType AccountType { get; set; }

        public string AccountNumber { get; set; }

        public FilingCycle Cycle { get; set; }

        public DateTime EndingPeriod { get; set; }

        public DateTime DueDate { get; set; }

        public string Notes { get; set; }

        public string ConfirmationNotes { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid UserAccountId { get; set; }

        public UserAccount UserAccount { get; set; }

        public TaxFilingLog()
        { }
    }
}
