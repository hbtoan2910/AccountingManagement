using System;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class BankAccountDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings and Commands
        private bool _isNew = false;
        public bool IsNew
        {
            get { return _isNew; }
            set { SetProperty(ref _isNew, value); }
        }

        private BusinessInfo _businessInfo;
        public BusinessInfo BusinessInfo
        {
            get { return _businessInfo; }
            set { SetProperty(ref _businessInfo, value); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public DelegateCommand SaveBusinessInfoCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        private readonly IGlobalService _globalService;
        private readonly IBusinessOwnerService _businessOwnerService;

        public BankAccountDetailsViewModel(IGlobalService globalService, IBusinessOwnerService businessOwnerService)
        {
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));

            Initialize();
        }

        private void Initialize()
        {
            SaveBusinessInfoCommand = new DelegateCommand(SaveBusinessInfo);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        private void SaveBusinessInfo()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BusinessInfo.Title) && string.IsNullOrWhiteSpace(BusinessInfo.Content))
                {
                    ErrorMessage = "Must enter a Title or Content.";
                    return;
                }

                BusinessInfo.ModifiedTime = DateTime.Now;
                BusinessInfo.LastUpdated = $"Last updated by {_globalService.CurrentSession.UserDisplayName} at {BusinessInfo.ModifiedTime:MM-dd-yyyy HH:mm:ss}";

                if (_businessOwnerService.UpsertBusinessInfo(BusinessInfo))
                {
                    RaiseRequestClose(new DialogResult(ButtonResult.OK));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"ERROR: {ex.Message}!";
                Log.Error(ex, $"Error while saving Business Info. {ErrorMessage}");
            }
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (int.TryParse(parameters.GetValue<string>("BusinessInfoId"), out var businessInfoId))
            {
                BusinessInfo = _businessOwnerService.GetBusinessInfoById(businessInfoId);
            }
            else
            {
                IsNew = true;

                var businessId = Guid.Parse(parameters.GetValue<string>("BusinessId"));
                BusinessInfo = new BusinessInfo { BusinessId = businessId };
            }
        }

        public event Action<IDialogResult> RequestClose;

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Nothing to do, for now
        }
        #endregion
    }
}
