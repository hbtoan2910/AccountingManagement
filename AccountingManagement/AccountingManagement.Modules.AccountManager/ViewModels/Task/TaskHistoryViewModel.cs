using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using Prism.Events;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaskHistoryViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView TaskHistoryView => CollectionViewSource.View;

        private List<Task> _taskList;
        public List<Task> TaskList
        {
            get { return _taskList; }
            set { SetProperty(ref _taskList, value); }
        }

        private List<UserAccount> _userAccounts;
        public List<UserAccount> UserAccounts
        {
            get { return _userAccounts; }
            set { SetProperty(ref _userAccounts, value); }
        }

        private UserAccount _selectedUserAccount = null;
        public UserAccount SelectedUserAccount
        {
            get { return _selectedUserAccount; }
            set { SetProperty(ref _selectedUserAccount, value); }
        }

        public DelegateCommand RefreshViewCommand { get; private set; }

        private string _nameFilterText;
        public string NameFilterText
        {
            get { return _nameFilterText; }
            set
            {
                if (SetProperty(ref _nameFilterText, value))
                {
                    TaskHistoryView.Refresh();
                }
            }
        }

        private string _descriptionFilterText;
        public string DescriptionFilterText
        {
            get { return _descriptionFilterText; }
            set
            {
                if (SetProperty(ref _descriptionFilterText, value))
                {
                    TaskHistoryView.Refresh();
                }
            }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    TaskHistoryView.Refresh();
                }
            }
        }

        private DateTime _lastUpdatedFilter = DateTime.Now.AddMonths(-6);
        public DateTime LastUpdatedFilter
        {
            get { return _lastUpdatedFilter; }
            set
            {
                if (SetProperty(ref _lastUpdatedFilter, value))
                {
                    TaskHistoryView.Refresh();
                }
            }
        }
        #endregion

        private readonly IEventAggregator _eventAggregator;
        private readonly IWorkTaskService _workTaskProvider;
        private readonly IUserAccountService _userAccountProvider;

        public TaskHistoryViewModel(IEventAggregator eventAggregator, IWorkTaskService workTaskProvider,
            IUserAccountService userAccountProvider)
        {
            _eventAggregator = eventAggregator;
            _workTaskProvider = workTaskProvider;
            _userAccountProvider = userAccountProvider;

            Initialize();
        }

        private void Initialize()
        {
            RefreshViewCommand = new DelegateCommand(RefreshView);

            UserAccounts = _userAccountProvider.GetUserAccounts();

            TaskList = _workTaskProvider.GetTasks(DateTime.Now.AddMonths(-12));

            CollectionViewSource.Source = TaskList;
            CollectionViewSource.Filter += (s, e) =>
            {
                if (e.Item is Task task)
                {
                    if (string.IsNullOrWhiteSpace(NameFilterText) == false)
                    {
                        if (task.UserAccount.DisplayName.Contains(NameFilterText, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            e.Accepted = false;
                            return;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(DescriptionFilterText) == false)
                    {
                        if (task.Description.Contains(DescriptionFilterText, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            e.Accepted = false;
                            return;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(BusinessFilterText) == false)
                    {
                        if (task.Work.Description.Contains(BusinessFilterText, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            e.Accepted = false;
                            return;
                        }
                    }

                    e.Accepted = true;
                    return;
                }

                e.Accepted = true;
            };

            CollectionViewSource.SortDescriptions.Add(new SortDescription("LastUpdatedTime", ListSortDirection.Descending));
        }


        private void RefreshView()
        {

        }

    }
}
