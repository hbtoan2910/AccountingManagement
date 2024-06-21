using System;
using Prism.Events;
using AccountingManagement.Core.Events;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        #region Bindings & Commands
        private string _loggedInDisplayName;
        public string LoggedInDisplayName
        {
            get { return _loggedInDisplayName; }
            set { SetProperty(ref _loggedInDisplayName, value); }
        }

        private string _versionText;
        public string VersionText
        { 
            get { return _versionText; } 
            set { SetProperty(ref _versionText, value); }
        }
        #endregion

        private readonly IEventAggregator _eventAggregator;
        private readonly IGlobalService _globalService;

        public StatusBarViewModel(IEventAggregator eventAggregator, IGlobalService globalService)
        {
            _eventAggregator = eventAggregator;
            _globalService = globalService;

            SubscribeToEvents();
            Initialize();
        }

        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<LoggedInEvent>().Subscribe(HandleLoggedInEvent);
        }

        private void Initialize()
        {
            // TODO: Update version
            VersionText = "v4.r0213";
        }

        private void HandleLoggedInEvent(LoggedInEventArgs args)
        {
            var result = args.LoginResult;

            LoggedInDisplayName = result.DisplayName;
        }
    }
}

