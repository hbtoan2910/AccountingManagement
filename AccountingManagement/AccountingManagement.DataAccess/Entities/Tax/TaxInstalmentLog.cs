using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class TaxInstalmentLog
    {
        public int Id { get; set; }

        public Guid TaxAccountId { get; set; }

        public Guid BusinessId { get; set; }

        public FilingCycle Cycle { get; set; }

        public DateTime InstalmentDueDate { get; set; }

        public string ConfirmationNotes { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid UserAccountId { get; set; }

        public TaxInstalmentLog()
        { }
    }
}
