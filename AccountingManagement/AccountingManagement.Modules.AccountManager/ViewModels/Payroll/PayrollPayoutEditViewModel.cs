using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class PayrollPayoutEditViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private PayrollPayoutRecord _payoutRecord;
        public PayrollPayoutRecord PayoutRecord
        {
            get { return _payoutRecord; }
            set { SetProperty(ref _payoutRecord, value); }
        }

        private Business _business;
        public Business Business
        {
            get { return _business; }
            set { SetProperty(ref _business, value); }
        }

        public List<PayrollStatus> PayoutStatuses
        {
            get
            {
                return new List<PayrollStatus>
                {
                    PayrollStatus.None,
                    PayrollStatus.Done,
                };
            }
        }

        public DelegateCommand SavePayoutRecordCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        private readonly IPayrollService _payrollService;
        private readonly IGlobalService _globalService;

        public PayrollPayoutEditViewModel(IPayrollService payrollService, IGlobalService globalService)
        {
            _payrollService = payrollService ?? throw new ArgumentNullException(nameof(payrollService));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));

            Initialize();
        }

        private void Initialize()
        {
            SavePayoutRecordCommand = new DelegateCommand(SavePayoutRecord);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        public void SavePayoutRecord()
        {
            var currentUser = _globalService.CurrentSession.UserDisplayName;
            var updatedText = $"[{currentUser}] on {DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss")}";

            PayoutRecord.PayrollPayout1UpdatedBy = updatedText;
            PayoutRecord.PayrollPayout2UpdatedBy = updatedText;
            PayoutRecord.PayrollPayout3UpdatedBy = updatedText;

            _payrollService.UpsertPayrollPayoutRecord(PayoutRecord);

            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware implementation
        public string Title => "Business Payout Details";

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
            var payoutRecordId = parameters.GetValue<int>("PayoutRecordId");

            PayoutRecord = _payrollService.GetPayrollPayoutRecordById(payoutRecordId);
            Business = PayoutRecord.PayrollAccount.Business;
        }
        #endregion
    }
}
