using System;
using System.Windows;
using AccountingManagement.Services;
using AccountingManagement.Services.Email;
using AccountingManagement.Services.ErrorHandling;
using AccountingManagement.Views;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Serilog;

namespace AccountingManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Prism.Unity.PrismApplication //RYAN: add this Prism.Unity.PrismApplication to fix all errors :)
    {
        // Methods in this section are arranged in the order they are called when the application starts
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SetupExceptionHandling();
        }

        protected override void Initialize()
        {
            base.Initialize();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/accounting-manager.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Application starts");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry
                .RegisterSingleton<IMessageService, MessageService>()
                .RegisterSingleton<IAuthenticationService, AuthenticationService>()
                .RegisterSingleton<IGlobalService, GlobalService>()

                .RegisterSingleton<IDataProvider, DataProvider>()
                .RegisterScoped<IBusinessOwnerService, BusinessOwnerService>()
                .RegisterScoped<IClientPaymentService, ClientPaymentService>()
                .RegisterScoped<IPayrollService, PayrollService>()
                .RegisterScoped<ITaxAccountService, TaxAccountService>()
                .RegisterScoped<IUserAccountService, UserAccountService>()
                .RegisterScoped<IWorkTaskService, WorkTaskService>()

                .RegisterScoped<IEmailTemplateQueryService, EmailTemplateQueryService>()
                .RegisterScoped<IEmailSenderQueryService, EmailSenderQueryService>()
                .RegisterScoped<IEmailService, EmailService>()

                .RegisterSingleton<IPayrollProvider, PayrollProvider>()
                .RegisterSingleton<IFilingHandler, FilingHandler>()
                .RegisterSingleton<IPaymentHandler, PaymentHandler>()
                .RegisterSingleton<IPersonalTaxFilingHandler, PersonalTaxFilingHandler>()
                .RegisterSingleton<IEntityParser, EntityParser>()
                .RegisterSingleton<IErrorReportingService, ErrorReportingService>()

                .Register<IPayrollTool, PayrollTool>()
                .Register<IDbBackupTool, DbBackupTool>();

            containerRegistry
                .RegisterDialog<Core.Common.Views.MessageDialog, Core.Common.ViewModels.MessageDialogViewModel>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<Modules.AccountManager.AccountManagerModule>();
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);

            // regionAdapterMappings.RegisterMapping(typeof(StackPanel), Container.Resolve<StackPanelRegionAdapter>());
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {

        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => 
            {
                var exception = e.ExceptionObject as Exception;

                Dispatcher.Invoke(() => ErrorReporting.ErrorReportingClient.ReportException(exception, "AppDomain.CurrentDomain.UnhandledException"));

                LogUnhandledException(exception, "AppDomain.CurrentDomain.UnhandledException");

                MessageBox.Show("An unexpected error happened, the application is shutting down. If the problem persists, please contact Administrator.",
                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                Application.Current.Shutdown();
            };

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception ex, string source)
        {
            Log.Error(ex, source);
        }
    }
}
