using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Services.Dialogs;
using Serilog;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class PayrollPeriodGeneratorViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        public List<string> _payrollPeriodList = new List<string>();
        public List<string> PayrollPeriodList
        {
            get { return _payrollPeriodList; }
            set { SetProperty(ref _payrollPeriodList, value); }
        }

        private string _selectedPayrollPeriod;
        public string SelectedPayrollPeriod
        {
            get { return _selectedPayrollPeriod; }
            set { SetProperty(ref _selectedPayrollPeriod, value); }
        }

        private bool _isOverwriteExistingData = false;
        public bool IsOverwriteExistingData
        {
            get { return _isOverwriteExistingData; }
            set { SetProperty(ref _isOverwriteExistingData, value); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public DelegateCommand GeneratePayrollCommand { get; private set; }
        #endregion

        private readonly IPayrollService _payrollService;
        private readonly IPayrollTool _payrollTool;

        public PayrollPeriodGeneratorViewModel(IPayrollService payrollService, IPayrollTool payrollTool)
        {
            _payrollService = payrollService ?? throw new ArgumentNullException(nameof(payrollService));
            _payrollTool = payrollTool;

            Initialize();
        }

        private void Initialize()
        {
            GeneratePayrollCommand = new DelegateCommand(GeneratePayroll);

            var latestPayrollPeriod = _payrollService.GetLatestPayrollPeriodLookups(1)?.FirstOrDefault();
            if (latestPayrollPeriod != null)
            {
                var currentPeriod = latestPayrollPeriod.PayrollPeriod;

                var payrollPeriods = _payrollTool.GetNextPayrollPeriods(currentPeriod, 1).OrderByDescending(x => x).ToList();
                payrollPeriods.Add(currentPeriod);
                payrollPeriods.AddRange(_payrollTool.GetPreviousPayrollPeriods(currentPeriod, 6).OrderByDescending(x => x));

                PayrollPeriodList = payrollPeriods;

                SelectedPayrollPeriod = PayrollPeriodList.First();
            }
        }

        private void GeneratePayroll()
        {
            try
            {
                _payrollTool.GeneratePayrollRecords(SelectedPayrollPeriod, IsOverwriteExistingData);

                ErrorMessage = $"Payroll records for period: {SelectedPayrollPeriod} generated!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"ERROR while generating payroll period: {SelectedPayrollPeriod}. {ex.Message}";
                Log.Error(ex, ErrorMessage);
            }
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Do nothing
        }
        #endregion
    }
}
