using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class PayrollEditViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private PayrollAccountRecord _payrollRecord;
        public PayrollAccountRecord PayrollRecord
        {
            get { return _payrollRecord; }
            set { SetProperty(ref _payrollRecord, value); }
        }

        private Business _business;
        public Business Business
        {
            get { return _business; }
            set { SetProperty(ref _business, value); }
        }

        public List<PayrollStatus> PayrollStatuses
        {
            get
            {
                return new List<PayrollStatus>
                {
                    PayrollStatus.None,
                    // PayrollStatus.InProgress,
                    PayrollStatus.Done,
                };
            }
        }
        // RYAN START: applying new logic:
        // if user choose Payroll1DueDate > Payroll2DueDate = Payroll1DueDate + 2 weeks > Payroll3DueDate = Payroll2DueDate + 2 weeks
        private DateTime? _payroll1DueDate;
        public DateTime? Payroll1DueDate
        {
            get => _payroll1DueDate;
            set
            {
                if (_payroll1DueDate != value)
                {
                    _payroll1DueDate = value;
                    RaisePropertyChanged(nameof(Payroll1DueDate));
                    if (PayrollRecord.PayrollAccount.PayrollCycle == FilingCycle.BiWeekly)
                    {
                        UpdatePayroll2DueDate();
                    }

                }
            }
        }

        private DateTime? _payroll2DueDate;
        public DateTime? Payroll2DueDate
        {
            get => _payroll2DueDate;
            set
            {
                if (_payroll2DueDate != value)
                {
                    _payroll2DueDate = value;
                    RaisePropertyChanged(nameof(Payroll2DueDate));
                    if (PayrollRecord.PayrollAccount.PayrollCycle == FilingCycle.BiWeekly)
                    {
                        UpdatePayroll3DueDate();
                    }

                }
            }
        }

        private DateTime? _payroll3DueDate;
        public DateTime? Payroll3DueDate
        {
            get => _payroll3DueDate;
            set
            {
                if (_payroll3DueDate != value)
                {
                    _payroll3DueDate = value;
                    RaisePropertyChanged(nameof(Payroll3DueDate));
                }
            }
        }

        private void UpdatePayroll2DueDate()
        {
            if (Payroll1DueDate.HasValue)
            {
                Payroll2DueDate = Payroll1DueDate.Value.AddDays(14);
            }
            else
            {
                Payroll2DueDate = null;
            }
        }
        private void UpdatePayroll3DueDate()
        {
            if (Payroll2DueDate.HasValue)
            {
                Payroll3DueDate = Payroll2DueDate.Value.AddDays(14);
            }
            else
            {
                Payroll3DueDate = null;
            }
        }
        // RYAN END: applying new logic

        public DelegateCommand SavePayrollRecordCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        private readonly IPayrollService _payrollService;
        private readonly IGlobalService _globalService;

        public PayrollEditViewModel(IPayrollService payrollService, IGlobalService globalService)
        {
            _payrollService = payrollService ?? throw new ArgumentNullException(nameof(payrollService));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));

            Initialize();
        }

        private void Initialize()
        {
            SavePayrollRecordCommand = new DelegateCommand(SavePayrollRecord);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        public void SavePayrollRecord()
        {
            var currentUser = _globalService.CurrentSession.UserDisplayName;
            var updatedText = $"[{currentUser}] on {DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss")}";

            PayrollRecord.Payroll1UpdatedBy = updatedText;
            PayrollRecord.Payroll2UpdatedBy = updatedText;
            PayrollRecord.Payroll3UpdatedBy = updatedText;

            PayrollRecord.Payroll1DueDate = Payroll1DueDate;
            PayrollRecord.Payroll2DueDate = Payroll2DueDate;
            PayrollRecord.Payroll3DueDate = Payroll3DueDate;

            _payrollService.UpsertPayrollRecord(PayrollRecord);

            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware implementation
        public string Title => "Business Payroll Details";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Nothing to do yet
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var payrollRecordId = parameters.GetValue<int>("PayrollRecordId");

            PayrollRecord = _payrollService.GetPayrollRecordById(payrollRecordId);

            Payroll1DueDate = PayrollRecord.Payroll1DueDate;
            Payroll2DueDate = PayrollRecord.Payroll2DueDate;
            Payroll3DueDate = PayrollRecord.Payroll3DueDate;

            Business = PayrollRecord.PayrollAccount.Business;
        }
        #endregion
    }
}
