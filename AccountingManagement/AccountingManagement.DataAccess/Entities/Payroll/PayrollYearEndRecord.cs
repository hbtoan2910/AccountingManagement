using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class PayrollYearEndRecord
    {
        public int Id { get; set; }

        public PayrollAccount PayrollAccount { get; set; }

        public Guid PayrollAccountId { get; set; }

        public string YearEndPeriod { get; set; }

        public bool T4Reconciliation { get; set; }

        public bool T4FormReady { get; set; }

        public string T4Confirmation { get; set; }

        public PayrollStatus T4Status { get; set; }

        public string T4UpdatedBy { get; set; }

        public bool T4AReconciliation { get; set; }

        public string T4AConfirmation { get; set; }

        public PayrollStatus T4AStatus { get; set; }

        public string T4AUpdatedBy { get; set; }

        public bool T5Reconciliation { get; set; }

        public string T5Confirmation { get; set; }

        public PayrollStatus T5Status { get; set; }

        public string T5UpdatedBy { get; set; }

        public string Notes { get; set; }

    }
}
