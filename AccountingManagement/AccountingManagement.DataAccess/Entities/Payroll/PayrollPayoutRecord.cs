using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class PayrollPayoutRecord
    {
        public long Id { get; set; }

        public Guid PayrollAccountId { get; set; }

        public PayrollAccount PayrollAccount { get; set; }

        public string PayrollPeriod { get; set; }

        public DateTime? PayrollPayout1DueDate { get; set; }

        public PayrollStatus PayrollPayout1Status { get; set; }

        public string PayrollPayout1UpdatedBy { get; set; }

        public DateTime? PayrollPayout2DueDate { get; set; }

        public PayrollStatus PayrollPayout2Status { get; set; }

        public string PayrollPayout2UpdatedBy { get; set; }

        public DateTime? PayrollPayout3DueDate { get; set; }

        public PayrollStatus PayrollPayout3Status { get; set; }

        public string PayrollPayout3UpdatedBy { get; set; }

        public string Notes { get; set; }
    }
}
