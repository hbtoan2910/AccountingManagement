using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Serilog;
using System.Windows;
using AccountingManagement.Core;
using AccountingManagement.Core.Events;
using AccountingManagement.Services;

namespace AccountingManagement.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Binding properties
        // private string _title = UserMessages.AppTitle;
        private string _title = string.Empty;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private WindowState _winState;
        public WindowState WinState
        {
            get { return _winState; }
            set { SetProperty(ref _winState, value); }
        }
        #endregion

        private IEventAggregator _eventAggregator;
        private IRegionManager _regionManager;
        private IDbBackupTool _backupTool;
        private IGlobalService _globalService;

        public MainWindowViewModel(IRegionManager regionManager, IEventAggregator eventAggregator,
            IGlobalService globalService, IDbBackupTool backupTool)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;

            _globalService = globalService;
            _backupTool = backupTool;

            SubscribeToEvents();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.MainView>(ViewRegKeys.MainView);
        }

        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<LoggedInEvent>().Subscribe(LoggedInEventHandler);
        }

        private void LoggedInEventHandler(LoggedInEventArgs args)
        {
            WinState = WindowState.Maximized;

            System.Threading.Tasks.Task.Run(() => RunHealthCheckProcedure());
        }

        private void RunHealthCheckProcedure()
        {
            if (_globalService.CurrentSession?.Role == Core.Authentication.AccountRole.Administator)
            {
                Log.Information("Running health check procedure");

                _backupTool.BackupMainDatabase();
            }
        }
    }
}
