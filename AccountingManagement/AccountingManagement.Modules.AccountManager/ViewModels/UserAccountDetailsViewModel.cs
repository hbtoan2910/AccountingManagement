using System;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class UserAccountDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings & Commands
        private UserAccount _userAccount;
        public UserAccount UserAccount
        {
            get { return _userAccount; }
            set { SetProperty(ref _userAccount, value); }
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { SetProperty(ref _isAdmin, value); }
        }

        private bool _isNewAccount;
        public bool IsNewAccount
        { 
            get { return _isNewAccount; } 
            set { SetProperty(ref _isNewAccount, value); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public DelegateCommand<object> SaveUserAccountCommand { get; private set; }
        public DelegateCommand<UserAccount> DeleteUserAccountCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IUserAccountService _userAccountProvider;
        private readonly IGlobalService _globalService;

        public UserAccountDetailsViewModel(IDialogService dialogService, IEventAggregator eventAggregator,
            IGlobalService globalService, IUserAccountService userAccountProvider)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _globalService = globalService;
            _userAccountProvider = userAccountProvider;

            SaveUserAccountCommand = new DelegateCommand<object>(SaveUserAccount);
            DeleteUserAccountCommand = new DelegateCommand<UserAccount>(DeleteUserAccount);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        private void DeleteUserAccount(UserAccount userAccount)
        {
            if (_dialogService.ShowConfirmation("Confirm Delete User", "Do you want to delete this User Account?"))
            {
                try
                {
                    _userAccountProvider.DeleteUserAccount(userAccount.Id);

                    _eventAggregator.GetEvent<Events.UserAccountUpsertedEvent>().Publish(UserAccount.Id);

                    CloseDialog();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Unexpected error deleting User Account:[{userAccount.Id}|{userAccount.Username}]", ex);
                }
            }
        }

        private void SaveUserAccount(object parameter)
        {
            if (parameter is Helpers.PasswordBoxList pwList)
            {
                if (string.IsNullOrEmpty(pwList.OldPassword.Password) == false || string.IsNullOrEmpty(pwList.NewPassword.Password) == false)
                {
                    // Trigger change password process
                    if (IsNewAccount == false)
                    {
                        if (Core.Utility.Hash.VerifyHash(pwList.OldPassword.Password, UserAccount.Password) == false)
                        {
                            ShowErrorMessage("Incorrect password!");
                            return;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(pwList.NewPassword.Password))
                    {
                        ShowErrorMessage("New password cannot be blank!");
                        return;
                    }

                    if (pwList.NewPassword.Password != pwList.ConfirmNewPassword.Password)
                    {
                        ShowErrorMessage("New passwords don't match!");
                        return;
                    }

                    UserAccount.Password = Core.Utility.Hash.GetHash(pwList.NewPassword.Password);
                }

                _userAccountProvider.UpsertUserAccount(UserAccount);

                _eventAggregator.GetEvent<Events.UserAccountUpsertedEvent>().Publish(UserAccount.Id);

                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            else
            {
                ShowErrorMessage("Invalid input parameters.");
            }
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        private void ShowErrorMessage(string message, Exception ex = null)
        {
            ErrorMessage = ex != null
                ? $"ERROR: {message}. Exception: {ex.Message}"
                : $"ERROR: {message}";
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            IsAdmin = _globalService.CurrentSession.Role == Core.Authentication.AccountRole.Administator;

            if (Guid.TryParse(parameters.GetValue<string>("UserAccountId"), out Guid userId))
            {
                UserAccount = _userAccountProvider.GetUserAccountById(userId);
            }
            else
            {
                IsNewAccount = true;

                UserAccount = new UserAccount
                {
                    Password = Core.Utility.Hash.GetHash("01234"),
                };
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
            // Do nothing;
        }
        #endregion
    }
}
