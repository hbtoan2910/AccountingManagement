using System.Linq;
using System.Windows;
using System.Windows.Media;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class BusinessPayoutModel : ViewModelBase
    {
        public Business Business { get; set; }

        public string PayrollPeriod { get; set; }

        public PayrollAccount PayrollAccount { get; set; }

        public PayrollPayoutRecord PayoutRecord { get; set; }

        public Visibility Payout1Visibility => PayoutRecord.PayrollPayout1DueDate == null
            ? Visibility.Hidden
            : Visibility.Visible;

        public Visibility Payout2Visibility => PayoutRecord.PayrollPayout2DueDate == null
            ? Visibility.Hidden
            : Visibility.Visible;

        public Visibility Payout3Visibility => PayoutRecord.PayrollPayout3DueDate == null
            ? Visibility.Hidden
            : Visibility.Visible;

        public BusinessPayoutModel(Business business, string payrollPeriod)
        {
            Business = business;
            PayrollPeriod = payrollPeriod;
            PayrollAccount = business.PayrollAccount;
            PayoutRecord = business.PayrollAccount.PayrollPayoutRecords.First();
        }
    }
}
