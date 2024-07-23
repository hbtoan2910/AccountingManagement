using System;
using System.Collections.Generic;

namespace AccountingManagement.DataAccess.Entities
{
    public class PayrollAccount
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public string PayrollNumber { get; set; }

        public FilingCycle PayrollCycle { get; set; }

        public FilingCycle PD7ACycle { get; set; }

        public bool IsRunPayroll { get; set; }

        public bool IsRunPayout { get; set; }

        public bool IsPayPD7A { get; set; }

        public string Notes { get; set; }

        public string PayoutNotes { get; set; }

        public bool IsRunT4 { get; set; }

        public bool IsRunT4A { get; set; }

        public bool IsRunT5 { get; set; }

        public bool Timesheet { get; set; }

        public string YearEndNotes { get; set; }

        public bool IsActive { get; set; }

        public List<PayrollAccountRecord> PayrollAccountRecords { get; set; }

        public List<PayrollPayoutRecord> PayrollPayoutRecords { get; set; }

        public PayrollAccount()
        {
            IsActive = true;
        }
    }
}
