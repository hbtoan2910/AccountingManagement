using System;
using System.Windows;
using System.Windows.Media;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    // TODO: It's my ignorance. Based on functionality, this class should be a ViewModel
    public class PayrollYearEndRecordModel : ViewModelBase
    {
        public PayrollYearEndRecord PayrollYearEndRecord { get; set; }

        public Business Business => PayrollAccount.Business;

        public PayrollAccount PayrollAccount => PayrollYearEndRecord.PayrollAccount;

        public string YearEndPeriod => PayrollYearEndRecord.YearEndPeriod;


        public Visibility T4BlockVisibility => PayrollAccount.IsRunT4 ? Visibility.Visible : Visibility.Hidden; 

        private string _t4ConfirmationText;
        public string T4ConfirmationText
        {
            get { return _t4ConfirmationText; }
            set
            {
                if (SetProperty(ref _t4ConfirmationText, value))
                {
                    PayrollYearEndRecord.T4Confirmation = _t4ConfirmationText;

                    // RaisePropertyChanged(nameof(T4ConfirmTextColor));
                    RaisePropertyChanged(nameof(EnableT4FilingButton));
                }
            }
        }

        public bool EnableT4FilingButton => PayrollYearEndRecord.T4Reconciliation
            && PayrollYearEndRecord.T4FormReady
            && string.IsNullOrWhiteSpace(PayrollYearEndRecord.T4Confirmation) == false;

        public SolidColorBrush T4ReconciliationColor => PayrollYearEndRecord.T4Reconciliation
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush T4FormReadyColor => PayrollYearEndRecord.T4FormReady
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush T4StatusColor => PayrollYearEndRecord.T4Status == PayrollStatus.Done
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);


        public Visibility T4ABlockVisibility => PayrollAccount.IsRunT4A ? Visibility.Visible : Visibility.Hidden;

        private string _t4AConfirmationText;
        public string T4AConfirmationText
        {
            get { return _t4AConfirmationText; }
            set
            {
                if (SetProperty(ref _t4AConfirmationText, value))
                {
                    PayrollYearEndRecord.T4AConfirmation = _t4AConfirmationText;

                    // RaisePropertyChanged(nameof(T4AConfirmTextColor));
                    RaisePropertyChanged(nameof(EnableT4AFilingButton));
                }
            }
        }

        public bool EnableT4AFilingButton => PayrollYearEndRecord.T4AReconciliation
            && string.IsNullOrWhiteSpace(PayrollYearEndRecord.T4AConfirmation) == false;

        public SolidColorBrush T4AReconciliationColor => PayrollYearEndRecord.T4AReconciliation
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush T4AStatusColor => PayrollYearEndRecord.T4AStatus == PayrollStatus.Done
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);


        public Visibility T5BlockVisibility => PayrollAccount.IsRunT5 ? Visibility.Visible : Visibility.Hidden;

        private string _t5ConfirmationText;
        public string T5ConfirmationText
        {
            get { return _t5ConfirmationText; }
            set
            {
                if (SetProperty(ref _t5ConfirmationText, value))
                {
                    PayrollYearEndRecord.T5Confirmation = _t5ConfirmationText;

                    // RaisePropertyChanged(nameof(T5ConfirmTextColor));
                    RaisePropertyChanged(nameof(EnableT5FilingButton));
                }
            }
        }

        public bool EnableT5FilingButton => PayrollYearEndRecord.T5Reconciliation
            && string.IsNullOrWhiteSpace(PayrollYearEndRecord.T5Confirmation) == false;

        public SolidColorBrush T5ReconciliationColor => PayrollYearEndRecord.T5Reconciliation
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush T5StatusColor => PayrollYearEndRecord.T5Status == PayrollStatus.Done
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);


        public PayrollYearEndRecordModel(PayrollYearEndRecord payrollYearEndRecord)
        {
            PayrollYearEndRecord = payrollYearEndRecord;

            _t4ConfirmationText = payrollYearEndRecord.T4Confirmation;
            _t4AConfirmationText = payrollYearEndRecord.T4AConfirmation;
            _t5ConfirmationText = payrollYearEndRecord.T5Confirmation;
        }
    }
}
