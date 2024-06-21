using AccountingManagement.Core;
using AccountingManagement.Core.Events;
using AccountingManagement.Modules.AccountManager.Views;
using AccountingManagement.Modules.AccountManager.ViewModels;
using AccountingManagement.Services;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace AccountingManagement.Modules.AccountManager
{
    public class AccountManagerModule : IModule
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;

        public AccountManagerModule(IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            _globalService = globalService;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RequestNavigate(RegionNames.MainWindowRegion, ViewRegKeys.LoginView);

            // DEBUG: Go straight to AccountManagerMainView
            //_regionManager.RequestNavigate(RegionNames.MainWindowRegion, ViewRegKeys.AccountManagerMainView);
            //_regionManager.RequestNavigate(RegionNames.TopBarMenuRegion, ViewRegKeys.MenuTopBar);
            //_regionManager.RequestNavigate(RegionNames.StatusBarRegion, ViewRegKeys.StatusBar);

            SubscribeToEvents();

            #region View discovery approach
            // _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(Login));
            #endregion

            #region View injection approach
            //var region = _regionManager.Regions[RegionNames.ContentRegion];

            //var changePasswordView = containerProvider.Resolve<ChangePassword>();
            //region.Add(changePasswordView);
            //region.Activate(changePasswordView);
            #endregion
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Login>(ViewRegKeys.LoginView);
            containerRegistry.RegisterForNavigation<MenuTopBar>(ViewRegKeys.MenuTopBar);
            containerRegistry.RegisterForNavigation<StatusBar>(ViewRegKeys.StatusBar);
            containerRegistry.RegisterForNavigation<AccountManagerMainView>(ViewRegKeys.AccountManagerMainView);

            containerRegistry.RegisterForNavigation<BusinessOverview>(ViewRegKeys.BusinessOverview);
            containerRegistry.RegisterForNavigation<ClientPaymentOverview>(ViewRegKeys.ClientPaymentOverview);
            containerRegistry.RegisterForNavigation<ClientPaymentHistory>(ViewRegKeys.ClientPaymentHistory);
            containerRegistry.RegisterForNavigation<EmailTemplateOverview>(ViewRegKeys.EmailTemplateOverview);
            containerRegistry.RegisterForNavigation<HSTFiling>(ViewRegKeys.HSTFiling);
            containerRegistry.RegisterForNavigation<OwnerOverview>(ViewRegKeys.OwnerOverview);
            containerRegistry.RegisterForNavigation<PayrollOverview>(ViewRegKeys.PayrollOverview);
            containerRegistry.RegisterForNavigation<PayrollYearEndOverview>(ViewRegKeys.PayrollYearEndOverview);
            containerRegistry.RegisterForNavigation<PayrollPayoutOverview>(ViewRegKeys.PayrollPayoutOverview);
            containerRegistry.RegisterForNavigation<PersonalTaxAccountOverview>(ViewRegKeys.PersonalTaxAccountOverview);
            containerRegistry.RegisterForNavigation<PersonalTaxAccountHistory>(ViewRegKeys.PersonalTaxAccountHistory);
            containerRegistry.RegisterForNavigation<TaxAccountOverview>(ViewRegKeys.TaxAccountOverview);
            containerRegistry.RegisterForNavigation<TaxAccountWithInstalmentOverview>(ViewRegKeys.TaxAccountWithInstalmentOverview);
            containerRegistry.RegisterForNavigation<TaxAccountHistory>(ViewRegKeys.TaxAccountHistory);
            containerRegistry.RegisterForNavigation<TaxInstalmentOverview>(ViewRegKeys.TaxInstalmentOverview);
            containerRegistry.RegisterForNavigation<TaskOverview>(ViewRegKeys.TaskOverview);
            containerRegistry.RegisterForNavigation<TaskHistory>(ViewRegKeys.TaskHistory);
            containerRegistry.RegisterForNavigation<UserAccountOverview>(ViewRegKeys.UserAccountOverview);

            containerRegistry.RegisterDialog<BankAccountDetails, BankAccountDetailsViewModel>();
            containerRegistry.RegisterDialog<BusinessDetails, BusinessDetailsViewModel>();
            containerRegistry.RegisterDialog<HSTDetails, HSTDetailsViewModel>();
            containerRegistry.RegisterDialog<NoteDetails, NoteDetailsViewModel>();
            containerRegistry.RegisterDialog<OwnerDetails, OwnerDetailsViewModel>();
            containerRegistry.RegisterDialog<PayrollEdit, PayrollEditViewModel>();
            containerRegistry.RegisterDialog<PayrollPayoutEdit, PayrollPayoutEditViewModel>();
            containerRegistry.RegisterDialog<PayrollPeriodGenerator, PayrollPeriodGeneratorViewModel>();
            containerRegistry.RegisterDialog<TaskDetails, TaskDetailsViewModel>();
            containerRegistry.RegisterDialog<TaxAccountDetails, TaxAccountDetailsViewModel>();
            containerRegistry.RegisterDialog<TaxAccountWithInstalmentDetails, TaxAccountWithInstalmentDetailsViewModel>();
            containerRegistry.RegisterDialog<UserAccountDetails, UserAccountDetailsViewModel>();

            containerRegistry.RegisterDialog<SimpleInputDialog, SimpleInputDialogViewModel>();
            containerRegistry.RegisterDialog<SimpleErrorDialog, SimpleErrorDialogViewModel>();
        }

        private void SubscribeToEvents()
        {
            // _eventAggregator.GetEvent<LoggedInEvent>().Subscribe(LoggedInEventHandler);
        }

        private void LoggedInEventHandler(LoggedInEventArgs args)
        {
            // _regionManager.RequestNavigate(RegionNames.MainWindowRegion, ViewRegKeys.WorkManager);
        }
    }
}