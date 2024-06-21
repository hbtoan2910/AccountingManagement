using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Utility;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class WorkManagerViewModel : BindableBase
    {
        #region Binding properties
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView WorksView => CollectionViewSource.View;

        private List<Work> _works;
        public List<Work> Works
        {
            get { return _works; }
            set { SetProperty(ref _works, value); }
        }
        #endregion

        public DelegateCommand CreateNewWorkCommand { get; private set; }
        public DelegateCommand<Guid?> CreateNewTaskCommand { get; private set; }
        public DelegateCommand FilterUserCommand { get; private set; }

        public DelegateCommand WorkItemDoubleClickCommand { get; private set; }
        public DelegateCommand<Guid?> MoveWorkItemUpCommand { get; private set; }
        public DelegateCommand<Guid?> MoveWorkItemDownCommand { get; private set; }
        public DelegateCommand<Guid?> TaskItemDoubleClickCommand { get; private set; }

        private readonly IDialogService _dialogService;
        private readonly IWorkTaskService _workTaskService;

        public WorkManagerViewModel(IDialogService dialogService, IWorkTaskService workTaskService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _workTaskService = workTaskService ?? throw new ArgumentNullException(nameof(workTaskService));

            CreateNewWorkCommand = new DelegateCommand(CreateNewWork);
            CreateNewTaskCommand = new DelegateCommand<Guid?>(CreateNewTask);
            FilterUserCommand = new DelegateCommand(FilterUser);

            WorkItemDoubleClickCommand = new DelegateCommand(OnWorkItemDoubleClicked);
            MoveWorkItemUpCommand = new DelegateCommand<Guid?>(MoveWorkItemUp);
            MoveWorkItemDownCommand = new DelegateCommand<Guid?>(MoveWorkItemDown);

            TaskItemDoubleClickCommand = new DelegateCommand<Guid?>(TaskItemDoubleClicked);

            Works = _workTaskService.GetPendingWorks();

            CollectionViewSource.Source = Works;
        }

        private void CreateNewWork()
        {

        }

        private void CreateNewTask(Guid? workId)
        {
            if (IsNullOrEmpty(workId))
            {
                return;
            }

            var parameters = new DialogParameters($"WorkId={workId}");

            _dialogService.ShowDialog(nameof(Views.TaskDetails), parameters, p => 
            {
                if (p.Result == ButtonResult.OK)
                {
                    Works = _workTaskService.GetPendingWorks();
                    RaisePropertyChanged("Works");
                }
            });
        }

        private void FilterUser()
        {

        }

        private void OnWorkItemDoubleClicked()
        {

        }

        private void MoveWorkItemUp(Guid? workId)
        {
            if (IsNullOrEmpty(workId))
            {
                return;
            }

            // TODO: Obsolete
            if (_workTaskService.SetWorkPriorityUp(workId.Value) > 0)
            {
                Works = _workTaskService.GetPendingWorks();
                RaisePropertyChanged("Works");
            }
        }

        private void MoveWorkItemDown(Guid? workId)
        {
            if (IsNullOrEmpty(workId))
            {
                return;
            }
        }

        private void TaskItemDoubleClicked(Guid? taskId)
        {
            if (IsNullOrEmpty(taskId))
            {
                return;
            }

            var parameters = new DialogParameters($"TaskId={taskId}");

            _dialogService.ShowDialog(nameof(Views.TaskDetails), parameters, p => 
            {
                Works = _workTaskService.GetPendingWorks();
                RaisePropertyChanged("Works");
            });
        }

        private bool IsNullOrEmpty(Guid? id)
        {
            return id == null || id == Guid.Empty;
        }
    }
}
