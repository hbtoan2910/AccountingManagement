using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class HSTAccount
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public string HSTNumber { get; set; }

        public FilingCycle HSTCycle { get; set; }

        public DateTime HSTEndingPeriod { get; set; }

        public DateTime HSTDueDate { get; set; }

        public string HSTAmount { get; set; }

        public bool InstalmentRequired { get; set; }

        public DateTime InstalmentDueDate { get; set; }

        public decimal InstalmentAmount { get; set; }

        public string Notes { get; set; }

        public bool IsRunHST { get; set; }

        public bool IsActive { get; set; }

        public HSTAccount()
        {
            IsRunHST = true;
            IsActive = true;
        }
    }
}
