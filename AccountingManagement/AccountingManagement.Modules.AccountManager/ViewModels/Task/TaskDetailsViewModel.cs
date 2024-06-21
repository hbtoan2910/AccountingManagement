using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using WorkTask = AccountingManagement.DataAccess.Entities.Task;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaskDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings and Commands
        private Work _work;
        public Work Work
        {
            get { return _work; }
            set { SetProperty(ref _work, value); }
        }

        private WorkTask _task;
        public WorkTask Task
        {
            get { return _task; }
            set { SetProperty(ref _task, value); }
        }

        public List<TaskStatus> TaskStatuses
        {
            get
            {
                if (_globalService.CurrentSession?.Role == Core.Authentication.AccountRole.Administator)
                {
                    return new List<TaskStatus>
                    {
                        TaskStatus.New,
                        TaskStatus.Done,
                        TaskStatus.Closed,
                    };
                }
                else
                {
                    return new List<TaskStatus>
                    {
                        TaskStatus.New,
                        TaskStatus.Done
                    };
                }
            }
        }

        private TaskStatus _selectedTaskStatus;
        public TaskStatus SelectedTaskStatus
        {
            get { return _selectedTaskStatus; }
            set { SetProperty(ref _selectedTaskStatus, value); }
        }

        private UserAccount _selectedUserAccount;
        public UserAccount SelectedUserAccount
        {
            get { return _selectedUserAccount; }
            set { SetProperty(ref _selectedUserAccount, value); }
        }

        private List<UserAccount> _userAccounts;
        public List<UserAccount> UserAccounts
        {
            get { return _userAccounts; }
            set { SetProperty(ref _userAccounts, value); }
        }

        public DelegateCommand SaveTaskCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        // public DelegateCommand UserAccountSelectionChangedCommand { get; private set; }
        #endregion

        private readonly IGlobalService _globalService;
        private readonly IUserAccountService _userAccountService;
        private readonly IWorkTaskService _workTaskProvider;

        public TaskDetailsViewModel(IGlobalService globalService, IUserAccountService userAccountService,
            IWorkTaskService workTaskProvider)
            : base()
        {
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));
            _userAccountService = userAccountService ?? throw new ArgumentNullException(nameof(userAccountService));
            _workTaskProvider = workTaskProvider ?? throw new ArgumentNullException(nameof(workTaskProvider));

            Initialize();
        }

        private void Initialize()
        {
            // TODO:
            _globalService.ValidateCurrentSession();

            UserAccounts = _userAccountService.GetUserAccounts();

            SaveTaskCommand = new DelegateCommand(SaveTask);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        public void SaveTask()
        {
            var currentUser = _globalService.CurrentSession.UserDisplayName;
            Task.LastUpdated = $"{currentUser} on {DateTime.Now:u}";
            Task.LastUpdatedTime = DateTime.UtcNow;

            _workTaskProvider.UpsertTask(Task);

            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        public void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (Guid.TryParse(parameters.GetValue<string>("BusinessId"), out Guid businessId))
            {
                Work = _workTaskProvider.GetWorkByBusinessId(businessId);
                Task = new WorkTask { Id = -1, Work = Work, TaskStatus = TaskStatus.New };

                RaisePropertyChanged("Task");
            }
            else if (Guid.TryParse(parameters.GetValue<string>("WorkId"), out Guid workId))
            {
                Work = _workTaskProvider.GetWorkById(workId);
                Task = new WorkTask { Id = -1, Work = Work, TaskStatus = TaskStatus.New };

                RaisePropertyChanged("Task");
            }
            else if (Int32.TryParse(parameters.GetValue<string>("TaskId"), out int taskId))
            {
                Task = _workTaskProvider.GetTaskById(taskId);
                Work = Task.Work;

                SelectedUserAccount = Task.UserAccount;
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
            // Nothing to do, for now
        }
        #endregion
    }

    public class TaskDetailsDialogParameter : DialogParameters
    {
        public TaskDetailsDialogParameter()
        {
            // GetValue<string>()
        }
    }
}

