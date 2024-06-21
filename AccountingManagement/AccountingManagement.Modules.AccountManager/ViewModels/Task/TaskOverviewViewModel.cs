using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using AccountingManagement.Core;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaskOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView WorkTasksView => CollectionViewSource.View;

        private Work _selectedWork;
        public Work SelectedWork
        {
            get { return _selectedWork; }
            set { SetProperty(ref _selectedWork, value); }
        }

        private ObservableCollection<Work> _workList;
        public ObservableCollection<Work> WorkList
        {
            get { return _workList; }
            set { SetProperty(ref _workList, value); }
        }

        private bool _filterHasTasks = true;
        public bool FilterHasTasks
        {
            get { return _filterHasTasks; }
            set 
            {
                if (SetProperty(ref _filterHasTasks, value))
                {
                    WorkTasksView.Refresh();
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
                    WorkTasksView.Refresh();
                }
            }
        }

        private bool _taskStatusFilterNew;
        public bool TaskStatusFilterNew
        { 
            get { return _taskStatusFilterNew; } 
            set 
            { 
                if (SetProperty(ref _taskStatusFilterNew, value))
                {
                    WorkTasksView.Refresh();
                }
            }
        }

        private bool _taskStatusFilterDone;
        public bool TaskStatusFilterDone
        {
            get { return _taskStatusFilterDone; }
            set 
            {
                if (SetProperty(ref _taskStatusFilterDone, value))
                {
                    WorkTasksView.Refresh();
                }
            }
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
            set 
            { 
                if (SetProperty(ref _selectedUserAccount, value))
                {
                    if (_selectedUserAccount != null)
                    {
                        RefreshView(_selectedUserAccount.Id);
                    }
                    else
                    {
                        RefreshView();
                    }
                }
            }
        }

        public DelegateCommand RefreshViewCommand { get; private set; }
        public DelegateCommand<Work> OpenNewTaskDialogCommand { get; private set; }
        public DelegateCommand<Task> OpenEditTaskDialogCommand { get; private set; }

        public DelegateCommand<Work> MoveWorkToTopCommand { get; private set; }
        public DelegateCommand<Work> MoveWorkUpCommand { get; private set; }
        public DelegateCommand<Work> MoveWorkDownCommand { get; private set; }

        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        private readonly IUserAccountService _userAccountService;
        private readonly IWorkTaskService _workTaskService;

        public TaskOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator,
            IRegionManager regionManager, IUserAccountService userAccountService, IWorkTaskService workTaskService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            _userAccountService = userAccountService;
            _workTaskService = workTaskService;

            Initialize();
        }

        private void Initialize()
        {
            RefreshViewCommand = new DelegateCommand(RefreshView);
            OpenNewTaskDialogCommand = new DelegateCommand<Work>(OpenNewTaskDialog);
            OpenEditTaskDialogCommand = new DelegateCommand<Task>(OpenEditTaskDialog);

            MoveWorkToTopCommand = new DelegateCommand<Work>(MoveWorkToTop);
            MoveWorkUpCommand = new DelegateCommand<Work>(MoveWorkUp);
            MoveWorkDownCommand = new DelegateCommand<Work>(MoveWorkDown);

            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);

            UserAccounts = _userAccountService.GetUserAccounts();

            CollectionViewSource.Filter += (s, e) => 
            {
                if (e.Item is Work work)
                {
                    if (string.IsNullOrWhiteSpace(BusinessFilterText) == false)
                    {
                        if (work.Business.OperatingName.Contains(BusinessFilterText, StringComparison.InvariantCultureIgnoreCase) == false
                            && work.Business.LegalName.Contains(BusinessFilterText, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            e.Accepted = false;
                            return;
                        }
                    }

                    if (FilterHasTasks && work.Tasks.Count == 0)
                    {
                        e.Accepted = false;
                        return;
                    }

                    if (TaskStatusFilterNew && work.Tasks.All(t => t.TaskStatus != TaskStatus.New))
                    {
                        e.Accepted = false;
                        return;
                    }

                    if (TaskStatusFilterDone && work.Tasks.All(t => t.TaskStatus != TaskStatus.Done))
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            CollectionViewSource.SortDescriptions.Add(new SortDescription("Priority", ListSortDirection.Descending));

            WorkList = new ObservableCollection<Work>(_workTaskService.GetPendingWorks());

            CollectionViewSource.Source = WorkList;
            SelectedWork = WorkList.FirstOrDefault();
        }

        private void RefreshView()
        {
            WorkList = new ObservableCollection<Work>(_workTaskService.GetPendingWorks());
            CollectionViewSource.Source = WorkList;

            RaisePropertyChanged("WorkTasksView");
        }

        private void RefreshView(Guid userId)
        {
            WorkList = new ObservableCollection<Work>(_workTaskService.GetPendingWorksByUserAccount(userId));
            CollectionViewSource.Source = WorkList;

            RaisePropertyChanged("WorkTasksView");
        }

        private void OpenNewTaskDialog(Work work)
        {
            if (work == null)
            {
                return;
            }

            var parameters = new DialogParameters($"BusinessId={work.BusinessId}");

            _dialogService.ShowDialog(nameof(Views.TaskDetails), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    LoadSelectedWorkTasks(work.Id);

                    WorkTasksView.Refresh();
                }
            });
        }

        private void OpenEditTaskDialog(Task task)
        {
            if (task == null)
            {
                return;
            }

            var parameters = new DialogParameters($"TaskId={task.Id}");

            _dialogService.ShowDialog(nameof(Views.TaskDetails), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    LoadSelectedWorkTasks(task.WorkId);

                    WorkTasksView.Refresh();
                }
            });
        }

        private void LoadSelectedWorkTasks(Guid? workId)
        {
            if (workId != null)
            {
                var updatedTasks = _workTaskService.GetTasksByWorkId(workId.Value)
                    .OrderByDescending(x => x.LastUpdatedTime)
                    .ToList();

                var work = WorkList.FirstOrDefault(w => w.Id == workId);
                if (work != null)
                {
                    work.Tasks = updatedTasks;
                }
            }
        }

        private void MoveWorkToTop(Work work)
        {
            if (work == null)
            {
                return;
            }

            var result = _workTaskService.SetWorkPriorityToTop(work.Id);
            if (result != int.MinValue)
            {
                work.Priority = result;
                WorkTasksView.Refresh();
            }
        }

        private void MoveWorkUp(Work work)
        {
            if (work == null)
            {
                return;
            }

            var result = _workTaskService.SetWorkPriorityUp(work.Id);
            if (result != int.MinValue)
            {
                work.Priority = result;
                WorkTasksView.Refresh();
            }
        }

        private void MoveWorkDown(Work work)
        {
            if (work == null)
            {
                return;
            }

            var result = _workTaskService.SetWorkPriorityDown(work.Id);
            if (result != int.MinValue)
            {
                work.Priority = result;
                WorkTasksView.Refresh();
            }
        }

        private void NavigateToBusinessOverview(Business business)
        {
            if (business == null)
            {
                return;
            }

            var navParams = new NavigationParameters($"BusinessOperatingName={business.OperatingName}");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.BusinessOverview, navParams);
        }
    }
}
