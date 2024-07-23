using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class PayrollAccountRecord
    {
        public long Id { get; set; }

        public Guid PayrollAccountId { get; set; }

        public PayrollAccount PayrollAccount { get; set; }

        public string PayrollPeriod { get; set; }

        public DateTime? Payroll1DueDate { get; set; }

        public PayrollStatus Payroll1Status { get; set; }

        public string Payroll1UpdatedBy { get; set; }

        public DateTime? Payroll2DueDate { get; set; }

        public PayrollStatus Payroll2Status { get; set; }

        public string Payroll2UpdatedBy { get; set; }

        public DateTime? Payroll3DueDate { get; set; }

        public PayrollStatus Payroll3Status { get; set; }

        public string Payroll3UpdatedBy { get; set; }

        public bool PD7APrinted { get; set; }

        public DateTime? PD7ADueDate { get; set; }

        public string PD7AConfirmation { get; set; }

        public EmailReminder PD7AReminder { get; set; }

        public PayrollStatus PD7AStatus { get; set; }

        public string PD7AUpdatedBy { get; set; }

        public string Notes { get; set; }
    }

    public enum PayrollStatus : byte
    {        
        None = 0,
        InProgress = 1,
        Done = 2,
        All = 3,//Ryan: to display all
    }

    public enum EmailReminder : byte
    {
        None = 0,
        Sent = 1
    }
}
