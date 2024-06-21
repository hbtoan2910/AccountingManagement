using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class HSTDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private HSTAccount _HSTAccount;
        public HSTAccount HSTAccount
        {
            get { return _HSTAccount; }
            set { SetProperty(ref _HSTAccount, value); }
        }

        private Business _business;
        public Business Business
        {
            get { return _business; }
            set { SetProperty(ref _business, value); }
        }

        public List<FilingCycle> HSTCycles
        {
            get
            {
                return new List<FilingCycle>
                {
                    FilingCycle.Monthly,
                    FilingCycle.Quarterly,
                    FilingCycle.Annually
                };
            }
        }

        public DelegateCommand SaveHstAccountCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }
        #endregion

        private bool _isNew = false;
        private readonly IDataProvider _dataProvider;
        private readonly IGlobalService _globalService;

        public HSTDetailsViewModel(IDataProvider dataProvider, IGlobalService globalService)
        {
            _dataProvider = dataProvider;
            _globalService = globalService;

            Initialize();
        }

        private void Initialize()
        {
            SaveHstAccountCommand = new DelegateCommand(SaveHstAccount);
            CloseDialogCommand = new DelegateCommand(CloseDialog);

        }

        public void SaveHstAccount()
        {
            try
            {
                // _userAccountProvider.UpsertHSTAccount(HSTAccount);

                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error while saving HST Account. {ex.Message}";
                Log.Error($"HSTAccountId:{HSTAccount.Id}, BusinessId:{Business.Id}, IsNew:{_isNew}. Error while saving HST Account. {ex}");
            }
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware
        public string Title => "HST Account Details";

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
            if (Guid.TryParse(parameters.GetValue<string>("HSTAccountId"), out Guid hstAccountId))
            {
                _isNew = false;

                // HSTAccount = _userAccountProvider.GetHSTAccountById(hstAccountId);
                Business = HSTAccount.Business;
            }
            else if (Guid.TryParse(parameters.GetValue<string>("BusinessId"), out Guid businessId))
            {
                // TODO: 
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
