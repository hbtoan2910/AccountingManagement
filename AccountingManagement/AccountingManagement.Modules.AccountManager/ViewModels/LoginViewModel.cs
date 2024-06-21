using System;
using System.Text;
using System.Threading;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using AccountingManagement.Core;
using AccountingManagement.Core.Authentication;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.Core.Events;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        #region Bindings & Commands
        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        private string _usernameInput;
        public string UsernameInput
        {
            get { return _usernameInput; }
            set { SetProperty(ref _usernameInput, value); }
        }

        public DelegateCommand<object> LoginCommand { get; private set; }
        public DelegateCommand ForgotPasswordCommand { get; private set; }
        #endregion

        private const int ErrorMessageTimeoutDuration = 5000;

        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IAuthenticationService _authenticationService;
        private readonly IGlobalService _globalService;

        private Timer _errMessageTimer = null;

        public LoginViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
            IAuthenticationService authenticationService, IGlobalService globalService)
            : base()
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
            _authenticationService = authenticationService;
            _globalService = globalService;

            LoginCommand = new DelegateCommand<object>(Login);
            ForgotPasswordCommand = new DelegateCommand(ForgotPassword);

            Message = "Login";
        }

        private void ForgotPassword()
        {
            // ErrorMessage = "ForgotPassword clicked";
        }

        private void Login(object parameter)
        {
            var message = new StringBuilder();

            if (parameter is System.Windows.Controls.PasswordBox passwordInput)
            {
                var result = _authenticationService.Login(UsernameInput, passwordInput.Password);

                switch (result.ResultCode)
                {
                    case LoginResultCode.Success:
                        _globalService.SetCurrentSession(result.UserAccountId, result.Username, result.DisplayName, result.Role);

                        _regionManager.RequestNavigate(RegionNames.MainWindowRegion, ViewRegKeys.AccountManagerMainView);
                        _regionManager.RequestNavigate(RegionNames.TopBarMenuRegion, ViewRegKeys.MenuTopBar);
                        _regionManager.RequestNavigate(RegionNames.StatusBarRegion, ViewRegKeys.StatusBar);
                        _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.BusinessOverview);

                        // Publish event after views/viewmodels are initialized for event subscriptions to work
                        var args = new LoggedInEventArgs(result);
                        _eventAggregator.GetEvent<LoggedInEvent>().Publish(args);
                        break;

                    case LoginResultCode.UsernameOrPasswordEmpty:
                        message.AppendLine("Username or password is empty.");
                        break;

                    case LoginResultCode.IncorrectUsernameOrPassword:
                        message.AppendLine("Incorrect username or password.");
                        break;

                    case LoginResultCode.AccountLocked:
                        message.AppendLine("Account locked.");
                        break;

                    case LoginResultCode.DatabaseUnreachable:
                        message.AppendLine("Cannot connect to Database. Please contact admin for support.");
                        break;

                    default:
                        message.AppendLine("Cannot login. Please contact admin for support.");
                        break;
                }

                ErrorMessage = message.ToString();

                SetTimerToClearErrorMessage();
            }
            else
            {
                ErrorMessage = "Unknown error. Please contact admin for support.";
            }
        }

        private void SetTimerToClearErrorMessage()
        {
            if (_errMessageTimer != null)
            {
                _errMessageTimer.Dispose();
            }

            _errMessageTimer = new Timer(x => { ErrorMessage = string.Empty; }, null, ErrorMessageTimeoutDuration, Timeout.Infinite);
        }

    }
}
