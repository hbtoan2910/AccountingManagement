using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using Serilog;
using System.Linq;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxAccountWithInstalmentDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private TaxAccountWithInstalment _taxAccount;
        public TaxAccountWithInstalment TaxAccount
        {
            get { return _taxAccount; }
            set { SetProperty(ref _taxAccount, value); }
        }

        private Business _business;
        public Business Business
        {
            get { return _business; }
            set { SetProperty(ref _business, value); }
        }

        public List<FilingCycle> FilingCycles
        {
            get
            {
                return new List<FilingCycle>
                {
                    FilingCycle.Annually,
                    FilingCycle.Quarterly,
                    FilingCycle.Monthly,
                };
            }
        }

        private List<UserAccount> _assignableUserAccounts;
        public List<UserAccount> AssignableUserAccounts
        {
            get { return _assignableUserAccounts; }
        }

        public DelegateCommand SaveTaxAccountCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }
        #endregion

        private bool _isNew = false;
        private readonly ITaxAccountService _taxAccountService;
        private readonly IGlobalService _globalService;
        private readonly IUserAccountService _userAccountService;

        public TaxAccountWithInstalmentDetailsViewModel(ITaxAccountService taxAccountService,
            IGlobalService globalService, IUserAccountService userAccountService)
        {
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _userAccountService = userAccountService ?? throw new ArgumentNullException(nameof(userAccountService));

            Initialize();
        }

        private void Initialize()
        {
            SaveTaxAccountCommand = new DelegateCommand(SaveTaxAccount);
            CloseDialogCommand = new DelegateCommand(CloseDialog);

            _assignableUserAccounts = _userAccountService.GetUserAccounts();
        }

        public void SaveTaxAccount()
        {
            try
            {
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

        #region IDialogAware
        public string Title => "Tax Account Details";

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
            if (Guid.TryParse(parameters.GetValue<string>("TaxAccountId"), out Guid taxAccountId))
            {
                _isNew = false;

                TaxAccount = _taxAccountService.GetTaxAccountWithInstalmentById(taxAccountId);
                Business = TaxAccount.Business;
            }
            else if (Guid.TryParse(parameters.GetValue<string>("BusinessId"), out Guid businessId))
            {
                _isNew = true;
            }
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
        #endregion
    }
}
