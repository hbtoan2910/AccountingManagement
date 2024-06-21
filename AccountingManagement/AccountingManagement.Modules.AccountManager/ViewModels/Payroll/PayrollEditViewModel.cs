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
            Business = PayrollRecord.PayrollAccount.Business;
        }
        #endregion
    }
}
