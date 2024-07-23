using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxAccountWithInstalmentBriefViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private TaxAccountWithInstalment _taxAccountWithInstalment;
        public TaxAccountWithInstalment TaxAccountWithInstalment
        {
            get { return _taxAccountWithInstalment; }
            set { SetProperty(ref _taxAccountWithInstalment, value); }
        }

        private Business _business;
        public Business Business
        {
            get { return _business; }
            set { SetProperty(ref _business, value); }
        }
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        //RYAN: for display purpose on dialogue ONLY
        private bool _instalmentRequired;
        public bool InstalmentRequired
        {
            get { return _instalmentRequired; }
            set { SetProperty(ref _instalmentRequired, value); }
        }

        private bool _isNew = false;               
        public DelegateCommand SaveTaxAccountCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        #region Services
        private readonly ITaxAccountService _taxAccountService;
        private readonly IFilingHandler _filingHandler;
        #endregion

        #region IDialogAware
        public string Title => "Tax Account: Instalment Details";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Nothing to do yet
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            //RYAN: this dialog is triggered from Federal > Confirm Filing
            if (parameters.ContainsKey("TaxAccountId") && Guid.TryParse(parameters.GetValue<string>("TaxAccountId"), out Guid taxAccountId))
            {
                _isNew = false;

                TaxAccountWithInstalment = _taxAccountService.GetTaxAccountWithInstalmentById(taxAccountId);

                if (TaxAccountWithInstalment != null && TaxAccountWithInstalment.AccountType == TaxAccountType.HST)
                {
                    TaxAccountWithInstalment.InstalmentRequired = true; //RYAN: always set to true, because if "needed=true" in the previous step b4 popup this dialog
                    InstalmentRequired = TaxAccountWithInstalment.InstalmentRequired;
                }
                if (TaxAccountWithInstalment != null && TaxAccountWithInstalment.AccountType == TaxAccountType.Corporation)
                {
                    TaxAccountWithInstalment.InstalmentRequired = true; //RYAN: always set to true, because if "needed=true" in the previous step b4 popup this dialog
                    InstalmentRequired = TaxAccountWithInstalment.InstalmentRequired;
                }
                
                Business = TaxAccountWithInstalment.Business;
            }
            else if (parameters.ContainsKey("BusinessId") &&  Guid.TryParse(parameters.GetValue<string>("BusinessId"), out Guid businessId))
            {
                _isNew = true;
            }
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
        #endregion

        public TaxAccountWithInstalmentBriefViewModel(ITaxAccountService taxAccountService, IFilingHandler filingHandler)

        {
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _filingHandler = filingHandler ?? throw new ArgumentNullException(nameof(filingHandler));

            Initialize();
        }
        private void Initialize()
        {
            SaveTaxAccountCommand = new DelegateCommand(SaveTaxAccount);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        public void SaveTaxAccount()
        {
            try
            {
                DateTime newInstalmentDueDate = _filingHandler.CalculateNextInstalmentDueDateNew(TaxAccountWithInstalment.EndingPeriod);
                TaxAccountWithInstalment.InstalmentDueDate = newInstalmentDueDate;

                if (TaxAccountWithInstalment.InstalmentAmount != 0)
                {
                    _taxAccountService.UpsertTaxAccountWithInstalment(TaxAccountWithInstalment);

                    RaiseRequestClose(new DialogResult(ButtonResult.OK));

                } else
                {
                    MessageBox.Show("Instalment Amount cannot be empty", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error while saving Tax Account. {ex.Message}";
                Log.Error($"Error saving {TaxAccountWithInstalment.AccountType} Tax Account."
                    + $" AccountId:{TaxAccountWithInstalment.Id}, BusinessId:{Business.Id}, IsNew:{_isNew}. {ex}");
            }
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }
    }
}
