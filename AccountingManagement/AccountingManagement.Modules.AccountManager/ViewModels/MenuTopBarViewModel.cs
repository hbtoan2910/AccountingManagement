using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using AccountingManagement.Core;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;
using AccountingManagement.Services.ErrorHandling;
using Org.BouncyCastle.Crypto.Tls;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class MenuTopBarViewModel : ViewModelBase
    {
        #region Properties and Commands
        private Visibility _userAccountManagementVisibility = Visibility.Collapsed;
        public Visibility UserAccountManagementVisibility
        {
            get { return _userAccountManagementVisibility; }
            set { SetProperty(ref _userAccountManagementVisibility, value); }
        }

        public DelegateCommand GoToBusinessOverviewCommand { get; private set; }
        public DelegateCommand GoToClientPaymentOverviewCommand { get; private set; }
        public DelegateCommand GoToEmailTemplateOverviewCommand { get; private set; }
        public DelegateCommand GoToHSTFilingCommand { get; private set; }
        public DelegateCommand GoToOwnerOverviewCommand { get; private set; }
        public DelegateCommand GoToPayrollOverviewCommand { get; private set; }
        public DelegateCommand GoToPayrollYearEndOverviewCommand { get; private set; }
        public DelegateCommand GoToPayrollPayoutOverviewCommand { get; private set; }
        public DelegateCommand GoToPersonalTaxAccountOverviewCommand { get; private set; }
        public DelegateCommand GoToSettingsViewCommand { get; private set; }
        public DelegateCommand GoToTaskOverviewCommand { get; private set; }
        public DelegateCommand GoToTaskHistoryCommand { get; private set; }
        public DelegateCommand GoToTaxAccountOverviewCommand { get; private set; }
        public DelegateCommand GoToTaxAccountWithInstalmentOverviewCommand { get; private set; }
        public DelegateCommand GoToTaxInstalmentOverviewCommand { get; private set; }
        public DelegateCommand GoToUserAccountOverviewCommand { get; private set; }

        public DelegateCommand OpenNewBusinessDialogCommand { get; private set; }
        public DelegateCommand OpenNewUserAccountDialogCommand { get; private set; }
        public DelegateCommand OpenUserAccountDetailsDialogCommand { get; private set; }

        public DelegateCommand SendErrorLogCommand { get; private set; }
        #endregion

        private static readonly Color DefaultSelectedViewColor = Colors.LightSkyBlue;

        private readonly IDialogService _dialogService;
        private readonly IErrorReportingService _errorReportingService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IGlobalService _globalService;
        private readonly IRegionManager _regionManager;

        public MenuTopBarViewModel(IDialogService dialogService, IErrorReportingService errorReportingService,
            IEventAggregator eventAggregator, IRegionManager regionManager, IGlobalService globalService)
        {
            _dialogService = dialogService;
            _errorReportingService = errorReportingService;
            _eventAggregator = eventAggregator;
            _globalService = globalService;
            _regionManager = regionManager;

            GoToBusinessOverviewCommand = new DelegateCommand(GoToBusinessOverview);
            GoToClientPaymentOverviewCommand = new DelegateCommand(GoToClientPaymentOverview);
            GoToEmailTemplateOverviewCommand = new DelegateCommand(GoToEmailTemplateOverview);
            GoToHSTFilingCommand = new DelegateCommand(GoToHSTFiling);
            GoToOwnerOverviewCommand = new DelegateCommand(GoToOwnerOverview);
            GoToPayrollOverviewCommand = new DelegateCommand(GoToPayrollOverview);
            GoToPayrollYearEndOverviewCommand = new DelegateCommand(GoToPayrollYearEndOverview);
            GoToPayrollPayoutOverviewCommand = new DelegateCommand(GoToPayrollPayoutOverview);
            GoToPersonalTaxAccountOverviewCommand = new DelegateCommand(GoToPersonalTaxAccountOverview);
            GoToSettingsViewCommand = new DelegateCommand(GoToSettingsView);
            GoToTaskOverviewCommand = new DelegateCommand(GoToTaskOverview);
            GoToTaskHistoryCommand = new DelegateCommand(GoToTaskHistory);
            GoToTaxAccountOverviewCommand = new DelegateCommand(GoToTaxAccountOverview);
            GoToTaxAccountWithInstalmentOverviewCommand = new DelegateCommand(GoToTaxAccountWithInstalmentOverview);
            GoToTaxInstalmentOverviewCommand = new DelegateCommand(GoToTaxInstalmentOverview);
            GoToUserAccountOverviewCommand = new DelegateCommand(GoToUserAccountOverview);

            OpenNewBusinessDialogCommand = new DelegateCommand(OpenNewBusinessDialog);
            OpenNewUserAccountDialogCommand = new DelegateCommand(OpenNewUserAccountDialog);
            OpenUserAccountDetailsDialogCommand = new DelegateCommand(OpenUserAccountDetailsDialog);

            SendErrorLogCommand = new DelegateCommand(SendErrorLog);

            Initialize();
        }

        private void Initialize()
        {
            var role = _globalService.CurrentSession.Role;
            if (role == Core.Authentication.AccountRole.Administator)
            {
                UserAccountManagementVisibility = Visibility.Visible;
            }
        }

        private void GoToBusinessOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.BusinessOverview);
        }

        private void GoToClientPaymentOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.ClientPaymentOverview);
        }

        private void GoToEmailTemplateOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.EmailTemplateOverview);
        }

        private void GoToHSTFiling()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.HSTFiling);
        }

        private void GoToOwnerOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.OwnerOverview);
        }

        private void GoToPayrollOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.PayrollOverview);
        }

        private void GoToPayrollYearEndOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.PayrollYearEndOverview);
        }

        private void GoToPayrollPayoutOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.PayrollPayoutOverview);
        }

        private void GoToPersonalTaxAccountOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.PersonalTaxAccountOverview);
        }

        private void GoToTaskOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaskOverview);
        }

        private void GoToTaskHistory()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaskHistory);
        }

        private void GoToTaxAccountOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaxAccountOverview);
        }

        private void GoToTaxAccountWithInstalmentOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaxAccountWithInstalmentOverview);
        }

        private void GoToTaxInstalmentOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaxInstalmentOverview);
        }

        private void GoToUserAccountOverview()
        {
            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.UserAccountOverview);
        }

        private void GoToSettingsView()
        { }

        private void OpenNewBusinessDialog()
        {
            var parameters = new DialogParameters($"");

            _dialogService.ShowDialog(nameof(Views.BusinessDetails), parameters, p =>
            {

            });
        }

        private void OpenNewUserAccountDialog()
        {
            var parameters = new DialogParameters($"");

            _dialogService.ShowDialog(nameof(Views.UserAccountDetails), parameters, p => 
            { 
                if (p.Result == ButtonResult.OK)
                {
                    // Event is published by UserAccountDetailsViewModel
                }
            });
        }

        private void OpenUserAccountDetailsDialog()
        {
            var userId = _globalService.CurrentSession.UserAccountId;

            var parameters = new DialogParameters($"UserAccountId={userId}");

            _dialogService.ShowDialog(nameof(Views.UserAccountDetails), parameters, p => 
            {
                if (p.Result == ButtonResult.OK)
                {
                    // Event is published by UserAccountDetailsViewModel
                }
            });
        }

        private void SendErrorLog()
        {
            var userDisplayName = _globalService.CurrentSession.UserDisplayName;

            try
            {
                // TODO: The Log file is being used
                // _errorReportingService.UploadLatestLogFile(userDisplayName);

                _dialogService.ShowInformation("Error Log sent", "Latest log has been uploaded successfully");
            }
            catch (FileNotFoundException)
            {
                _dialogService.ShowInformation("Error Log not sent", "No local Error Log found");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Cannot send Error Log", ex.Message);
            }
        }
    }
}
