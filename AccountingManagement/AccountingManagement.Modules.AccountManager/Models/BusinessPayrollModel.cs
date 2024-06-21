using System.Linq;
using System.Windows;
using System.Windows.Media;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class BusinessPayrollModel : ViewModelBase
    {
        public Business Business { get; set; }

        public string PayrollPeriod { get; set; }

        public PayrollAccount PayrollAccount { get; set; }

        public PayrollAccountRecord PayrollRecord { get; set; }

        public SolidColorBrush PD7AReminderColor => PayrollRecord.PD7AReminder == EmailReminder.Sent
            ? new SolidColorBrush(Colors.DodgerBlue)
            : new SolidColorBrush(Colors.LightGray);

        public Visibility Payroll1Visibility => PayrollRecord.Payroll1DueDate == null
            ? Visibility.Hidden
            : Visibility.Visible;

        public Visibility Payroll2Visibility => PayrollRecord.Payroll2DueDate == null
            ? Visibility.Hidden
            : Visibility.Visible;

        public Visibility Payroll3Visibility => PayrollRecord.Payroll3DueDate == null
            ? Visibility.Hidden
            : Visibility.Visible;

        public Visibility PD7AVisibility
        {
            get
            {
                if (PayrollAccount.IsPayPD7A && PayrollRecord.PD7ADueDate != null)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }
        }

        public BusinessPayrollModel(Business business, string payrollPeriod)
        {
            Business = business;
            PayrollPeriod = payrollPeriod;
            PayrollAccount = business.PayrollAccount;
            PayrollRecord = business.PayrollAccount.PayrollAccountRecords.First();
        }
    }
}
