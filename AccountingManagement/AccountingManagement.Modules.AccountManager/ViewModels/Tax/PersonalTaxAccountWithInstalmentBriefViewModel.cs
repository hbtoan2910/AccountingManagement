using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using Prism.Commands;
using Prism.Services.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class PersonalTaxAccountWithInstalmentBriefViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private PersonalTaxAccount _personalTaxAccount;
        public PersonalTaxAccount PersonalTaxAccount
        {
            get { return _personalTaxAccount; }
            set { SetProperty(ref _personalTaxAccount, value); }
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

        public DelegateCommand SavePersonalTaxAccountCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        #region Services
        private readonly ITaxAccountService _taxAccountService;
        private readonly IFilingHandler _filingHandler;
        #endregion

        #region IDialogAware
        public string Title => "Personal Tax / T1: Instalment Details";

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
            //RYAN: this dialog is triggered from T1 > Confirm Filing            
            if (parameters.ContainsKey("OwnerId") && Guid.TryParse(parameters.GetValue<string>("OwnerId"), out Guid OwnerId))
            {
                PersonalTaxAccount = _taxAccountService.GetPersonalTaxAccountByOwnerId(OwnerId);
                if (PersonalTaxAccount != null && PersonalTaxAccount.TaxType == PersonalTaxType.T1)
                {
                    PersonalTaxAccount.InstalmentRequired = true; //RYAN: always set to true, because if "needed=true" in the previous step b4 popup this dialog
                    InstalmentRequired = PersonalTaxAccount.InstalmentRequired;
                }

            }
            
        }
        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
        #endregion

        public PersonalTaxAccountWithInstalmentBriefViewModel(ITaxAccountService taxAccountService, IFilingHandler filingHandler)

        {
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _filingHandler = filingHandler ?? throw new ArgumentNullException(nameof(filingHandler));

            Initialize();
        }

        private void Initialize()
        {
            SavePersonalTaxAccountCommand = new DelegateCommand(SavePersonalTaxAccount);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }
        public void SavePersonalTaxAccount()
        {
            try
            {
                DateTime newInstalmentDueDate = _filingHandler.CalculateNextPersonalInstalmentDueDate();
                PersonalTaxAccount.InstalmentDueDate = newInstalmentDueDate;

                if (PersonalTaxAccount.InstalmentAmount != 0)
                {
                    _taxAccountService.UpsertPersonalTaxAccount(PersonalTaxAccount);

                    RaiseRequestClose(new DialogResult(ButtonResult.OK));

                } else
                {
                    MessageBox.Show("Instalment Amount cannot be empty", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error while saving Personal Tax Account. {ex.Message}";
                Log.Error($"Error saving Personal Tax Account."
                    + $" PersonalAccountId:{PersonalTaxAccount.Id}, OwnerId:{PersonalTaxAccount.OwnerId}");
            }
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }
    }
}
