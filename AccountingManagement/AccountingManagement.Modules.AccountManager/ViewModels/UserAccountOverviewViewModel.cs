using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class UserAccountOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView UserAccountView => CollectionViewSource.View;

        private ObservableCollection<UserAccount> _userAccounts;
        public ObservableCollection<UserAccount> UserAccounts
        {
            get { return _userAccounts; }
            set { SetProperty(ref _userAccounts, value); }
        }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IUserAccountService _userAccountService;

        public DelegateCommand<UserAccount> OpenEditUserAccountDialogCommand { get; private set; }

        public UserAccountOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator,
            IUserAccountService userAccountService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _userAccountService = userAccountService;

            Initialize();
        }

        private void Initialize()
        {
            OpenEditUserAccountDialogCommand = new DelegateCommand<UserAccount>(OpenEditUserAccountDialog);

            _eventAggregator.GetEvent<Events.UserAccountUpsertedEvent>().Subscribe(HandleUserAccountUpsertedEvent);

            UserAccounts = new ObservableCollection<UserAccount>(_userAccountService.GetUserAccounts());

            CollectionViewSource.Source = UserAccounts;
            CollectionViewSource.SortDescriptions.Add(new SortDescription("Username", ListSortDirection.Ascending));
        }

        private void OpenEditUserAccountDialog(UserAccount userAccount)
        {
            if (userAccount == null)
            {
                return;
            }

            var parameters = new DialogParameters($"UserAccountId={userAccount.Id}");

            _dialogService.ShowDialog(nameof(Views.UserAccountDetails), parameters, p => 
            {
                if (p.Result == ButtonResult.OK)
                {
                    // View refreshed by UserAccountUpsertedEvent
                }
            });
        }

        private void HandleUserAccountUpsertedEvent(Guid userAccountId)
        {
            RefreshUserAccountView();
        }

        private void RefreshUserAccountView()
        {
            UserAccounts = new ObservableCollection<UserAccount>(_userAccountService.GetUserAccounts());

            CollectionViewSource.Source = UserAccounts;
            RaisePropertyChanged("UserAccountView");
        }

    }
}
