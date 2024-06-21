using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class PayrollPeriodLookup
    {
        public int Id { get; set; }

        public string PayrollPeriod { get; set; }

        public string PreviousPayrollPeriod { get; set; }

        public DateTime? MonthlyDueDate { get; set; }

        public DateTime? SemiMonthlyDueDate1 { get; set; }

        public DateTime? SemiMonthlyDueDate2 { get; set; }

        public DateTime? BiWeeklyDueDate1 { get; set; }

        public DateTime? BiWeeklyDueDate2 { get; set; }

        public DateTime? BiWeeklyDueDate3 { get; set; }

        public DateTime? MonthlyPayoutDueDate { get; set; }

        public DateTime? BiWeeklyPayoutDueDate1 { get; set; }

        public DateTime? BiWeeklyPayoutDueDate2 { get; set; }

        public DateTime? BiWeeklyPayoutDueDate3 { get; set; }

        public DateTime? PD7AQuarterlyDueDate { get; set; }

        public DateTime? PD7AMonthlyDueDate { get; set; }
    }
}
