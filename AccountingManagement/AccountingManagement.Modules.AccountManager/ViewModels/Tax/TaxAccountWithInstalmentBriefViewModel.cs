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
using System.Windows.Data;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxAccountWithInstalmentBriefViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private TaxAccountWithInstalment _taxAccount;
        public TaxAccountWithInstalment TaxAccount
        {
            get { return _taxAccount; }
            set { SetProperty(ref _taxAccount, value); }
        }

        //private TaxAccountWithInstalment _hstAccount;
        //public TaxAccountWithInstalment HSTAccount
        //{
        //    get { return _hstAccount; }
        //    set { SetProperty(ref _hstAccount, value); }
        //}

        //private TaxAccountWithInstalment _corporationTaxAccount;
        //public TaxAccountWithInstalment CorporationTaxAccount
        //{
        //    get { return _corporationTaxAccount; }
        //    set { SetProperty(ref _corporationTaxAccount, value); }
        //}

        private bool _instalmentRequired;
        public bool InstalmentRequired
        {
            get { return _instalmentRequired; }
            set { SetProperty(ref _instalmentRequired, value); }
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

        public DelegateCommand SaveTaxAccountCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        private bool _isNew = false;

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
            if (parameters.ContainsKey("TaxAccountId") && Guid.TryParse(parameters.GetValue<string>("TaxAccountId"), out Guid taxAccountId))
            {
                _isNew = false;

                TaxAccount = _taxAccountService.GetTaxAccountWithInstalmentById(taxAccountId);

                if (TaxAccount != null && TaxAccount.AccountType == TaxAccountType.HST)
                {
                    TaxAccount.InstalmentRequired = true; //RYAN: always set to true, because if "needed=true" in the previous step b4 popup this dialog
                    InstalmentRequired = TaxAccount.InstalmentRequired;
                }
                if (TaxAccount != null && TaxAccount.AccountType == TaxAccountType.Corporation)
                {
                    TaxAccount.InstalmentRequired = true; //RYAN: always set to true, because if "needed=true" in the previous step b4 popup this dialog
                    InstalmentRequired = TaxAccount.InstalmentRequired;
                }
                
                Business = TaxAccount.Business;
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
                DateTime newInstalmentDueDate = _filingHandler.CalculateNextInstalmentDueDateNew(TaxAccount.EndingPeriod);
                TaxAccount.InstalmentDueDate = newInstalmentDueDate;

                _taxAccountService.UpsertTaxAccountWithInstalment(TaxAccount);

                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error while saving Tax Account. {ex.Message}";
                Log.Error($"Error saving {TaxAccount.AccountType} Tax Account."
                    + $" AccountId:{TaxAccount.Id}, BusinessId:{Business.Id}, IsNew:{_isNew}. {ex}");
            }
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }
    }
}
