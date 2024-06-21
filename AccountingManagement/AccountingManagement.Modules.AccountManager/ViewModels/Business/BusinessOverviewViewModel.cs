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
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using AccountingManagement.Modules.AccountManager.Utilities;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class BusinessOverviewViewModel : ViewModelBase, INavigationAware
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView BusinessesView => CollectionViewSource.View;

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    BusinessesView.Refresh();
                    RaisePropertyChanged(nameof(BusinessItemCount));
                }
            }
        }

        private string _selectedDueDateQuickFilter;
        public string SelectedDueDateQuickFilter
        {
            get { return _selectedDueDateQuickFilter; }
            set { SetProperty(ref _selectedDueDateQuickFilter, value); }
        }

        private DateTime? _selectedDueDate;
        public DateTime? SelectedDueDate
        {
            get { return _selectedDueDate; }
            set { SetProperty(ref _selectedDueDate, value); }
        }

        private bool _sortByBusinessNameAscending = true;
        public bool SortByBusinessNameAscending
        {
            get { return _sortByBusinessNameAscending; }
            set
            { 
                if (SetProperty(ref _sortByBusinessNameAscending, value))
                {
                    CollectionViewSource.SortDescriptions.Clear();
                    CollectionViewSource.SortDescriptions.Add(new SortDescription("OperatingName",
                        _sortByBusinessNameAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
                }
            }
        }

        private bool _includeDeletedBusiness;
        public bool IncludeDeletedBusiness
        {
            get { return _includeDeletedBusiness; }
            set
            {
                if (SetProperty(ref _includeDeletedBusiness, value))
                {
                    BusinessesView.Refresh();
                    RaisePropertyChanged(nameof(BusinessItemCount));
                }
            }
        }

        public int BusinessItemCount
        {
            get { return BusinessesView.Cast<object>()?.Count() ?? 0; }
        }

        // TODO: Create a simple model of Business
        private ObservableCollection<Business> _businessList;
        public ObservableCollection<Business> BusinessList
        {
            get { return _businessList; }
            set { SetProperty(ref _businessList, value); }
        }

        private Business _selectedBusiness;
        public Business SelectedBusiness
        {
            get { return _selectedBusiness; }
            set
            {
                if (SetProperty(ref _selectedBusiness, value))
                {
                    RefreshSelectedBusinessInformation();
                }
            }
        }

        private Business _businessFullDetails;
        public Business BusinessFullDetails 
        { 
            get { return _businessFullDetails; }
            set { SetProperty(ref _businessFullDetails, value); }
        }

        private ObservableCollection<Task> _selectedBusinessTasks;
        public ObservableCollection<Task> SelectedBusinessTasks
        {
            get { return _selectedBusinessTasks; }
            set { SetProperty(ref _selectedBusinessTasks, value); }
        }

        private ObservableCollection<Owner> _selectedBusinessOwners;
        public ObservableCollection<Owner> SelectedBusinessOwners
        {
            get { return _selectedBusinessOwners; }
            set { SetProperty(ref _selectedBusinessOwners, value); }
        }

        private ObservableCollection<BusinessInfo> _selectedBusinessInfos;
        public ObservableCollection<BusinessInfo> SelectedBusinessInfos
        {
            get { return _selectedBusinessInfos; }
            set { SetProperty(ref _selectedBusinessInfos, value); }
        }

        private ObservableCollection<Note> _selectedBusinessNotes;
        public ObservableCollection<Note> SelectedBusinessNotes
        {
            get { return _selectedBusinessNotes; }
            set { SetProperty(ref _selectedBusinessNotes, value); }
        }

        public PayrollAccount SelectedPayrollAccount
        {
            get { return SelectedBusiness?.PayrollAccount; }
        }

        public TaxAccountWithInstalment SelectedHSTAccount
        {
            get { return SelectedBusiness?.HSTAccount; }
        }

        public TaxAccountWithInstalment SelectedCorporationTaxAccount
        {
            get { return SelectedBusiness?.CorporationTaxAccount; }
        }

        public TaxAccount SelectedPSTAccount
        {
            get { return SelectedBusiness?.PSTAccount; }
        }

        public TaxAccount SelectedWSIBAccount
        {
            get { return SelectedBusiness?.WSIBAccount; }
        }

        public TaxAccount SelectedLIQAccount
        {
            get { return SelectedBusiness?.LIQAccount; }
        }

        public TaxAccount SelectedONTAccount
        {
            get { return SelectedBusiness?.ONTAccount; }
        }

        public ClientPayment SelectedRegularPayment
        {
            get { return SelectedBusiness?.RegularPayment; }
        }

        public ClientPayment SelectedClientPayment2nd
        {
            get { return SelectedBusiness?.ClientPayment2nd; }
        }

        public DelegateCommand<Guid?> OpenEditBusinessDialogCommand { get; private set; }
        public DelegateCommand<Business> DeleteBusinessCommand { get; private set; }
        public DelegateCommand<Business> ReactivateBusinessCommand { get; private set; }

        public DelegateCommand<Guid?> OpenNewTaskDialogCommand { get; private set; }
        public DelegateCommand<int?> OpenEditTaskDialogCommand { get; private set; }

        public DelegateCommand<Guid?> OpenNewOwnerDialogCommand { get; private set; }
        public DelegateCommand<Guid?> OpenEditOwnerDialogCommand { get; private set; }

        public DelegateCommand OpenNewNoteDialogCommand { get; private set; }
        public DelegateCommand<int?> OpenEditNoteDialogCommand { get; private set; }
        public DelegateCommand<Note> DeleteNoteCommand { get; private set; }

        public DelegateCommand OpenNewBusinessInfoDialogCommand { get; private set; }
        public DelegateCommand<int?> OpenEditBusinessInfoDialogCommand { get; private set; }

        public DelegateCommand RefreshPageCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly IWorkTaskService _workTaskProvider;
        private readonly IUserAccountService _userAccountProvider;

        public BusinessOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator,
            IBusinessOwnerService businessOwnerService, IWorkTaskService workTaskProvider, IUserAccountService userAccountProvider)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            _businessOwnerService = businessOwnerService;
            _workTaskProvider = workTaskProvider;
            _userAccountProvider = userAccountProvider;

            Initialize();
        }

        private void Initialize()
        {
            DeleteBusinessCommand = new DelegateCommand<Business>(DeleteBusiness);
            ReactivateBusinessCommand = new DelegateCommand<Business>(ReactivateBusiness);

            OpenEditBusinessDialogCommand = new DelegateCommand<Guid?>(OpenEditBusinessDialog);
            OpenNewTaskDialogCommand = new DelegateCommand<Guid?>(OpenNewTaskDialog);
            OpenEditTaskDialogCommand = new DelegateCommand<int?>(OpenEditTaskDialog);
            OpenNewOwnerDialogCommand = new DelegateCommand<Guid?>(OpenNewOwnerDialog);
            OpenEditOwnerDialogCommand = new DelegateCommand<Guid?>(OpenEditOwnerDialog);
            OpenNewNoteDialogCommand = new DelegateCommand(OpenNewNoteDialog);
            OpenEditNoteDialogCommand = new DelegateCommand<int?>(OpenEditNoteDialog);
            OpenNewBusinessInfoDialogCommand = new DelegateCommand(OpenNewBusinessInfoDialog);
            OpenEditBusinessInfoDialogCommand = new DelegateCommand<int?>(OpenEditBusinessInfoDialog);
            RefreshPageCommand = new DelegateCommand(RefreshPage);

            DeleteNoteCommand = new DelegateCommand<Note>(DeleteNoteById);

            _eventAggregator.GetEvent<Events.BusinessDeletedEvent>().Subscribe(HandleBusinessDeletedEvent);
            _eventAggregator.GetEvent<Events.BusinessUpsertedEvent>().Subscribe(HandleBusinessUpdatedEvent);

            BusinessList = new ObservableCollection<Business>(_businessOwnerService.GetBusinesses());
            CollectionViewSource.Source = BusinessList;

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is Business business))
                {
                    e.Accepted = false;
                    return;
                }

                if (IncludeDeletedBusiness != business.IsDeleted)
                {
                    e.Accepted = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(BusinessFilterText) == false)
                {
                    if (StringContainsFilterText(business.LegalName, BusinessFilterText)
                        || StringContainsFilterText(business.OperatingName, BusinessFilterText)
                        || StringContainsFilterText(business.BusinessNumber, BusinessFilterText)
                        || business.BusinessOwners.Any(o => StringContainsFilterText(o.Owner.Name, BusinessFilterText)))
                    {
                        // e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            CollectionViewSource.SortDescriptions.Add(new SortDescription("OperatingName", ListSortDirection.Ascending));

            // CollectionViewSource.View.Cast<object>().Count();
        }

        private void RefreshPage()
        {
            BusinessList = new ObservableCollection<Business>(_businessOwnerService.GetBusinesses());
            CollectionViewSource.Source = BusinessList;

            // BusinessesView.Refresh();
            RaisePropertyChanged("BusinessesView");
            RaisePropertyChanged("BusinessItemCount");
        }


        private void DeleteBusiness(Business business)
        {
            if (business == null)
            {
                return;
            }

            _dialogService.ShowConfirmation("Confirm Deactivate", $"Are you sure you want to deactivate the business [{business.OperatingName}]?", 
                a => 
                {
                    if (a.Result == ButtonResult.OK)
                    {
                        _businessOwnerService.SoftDeleteBusiness(business.Id);

                        _eventAggregator.GetEvent<Events.BusinessDeletedEvent>().Publish(business.Id);
                    }
                });
        }

        private void ReactivateBusiness(Business business)
        {
            if (business == null)
            {
                return;
            }

            _dialogService.ShowConfirmation("Confirm Reactivate", $"Are you sure you want to re-activate the business [{business.OperatingName}]?",
                a =>
                {
                    if (a.Result == ButtonResult.OK)
                    {
                        _businessOwnerService.UnDeleteBusiness(business.Id);

                        _eventAggregator.GetEvent<Events.BusinessUpsertedEvent>().Publish(business.Id);
                    }
                });
        }


        private void HandleBusinessDeletedEvent(Guid businessId)
        {
            var existing = BusinessList.FirstOrDefault(x => x.Id == businessId);
            if (existing != null)
            {
                BusinessList.Remove(existing);

                BusinessesView.Refresh();
            }
        }

        private void HandleBusinessUpdatedEvent(Guid businessId)
        {
            var updated = _businessOwnerService.GetBusinessByIdWithFullDetails(businessId);

            var existing = BusinessList.FirstOrDefault(x => x.Id == businessId);
            if (existing != null && updated != null)
            {
                existing.LegalName = updated.LegalName;
                existing.OperatingName = updated.OperatingName;
                existing.BusinessNumber = updated.BusinessNumber;
                existing.Address = updated.Address;
                existing.MailingAddress = updated.MailingAddress;
                existing.Email = updated.Email;
                existing.EmailContact = updated.EmailContact;

                existing.TaxAccounts = updated.TaxAccounts;
                existing.TaxAccountWithInstalments = updated.TaxAccountWithInstalments;
                existing.ClientPayments = updated.ClientPayments;
            }
            else if (updated != null)
            {
                BusinessList.Add(updated);
            }

            BusinessesView.Refresh();
        }

        private void LoadSelectedBusinessTasks(Guid? workId)
        {
            if (workId != null)
            {
                SelectedBusinessTasks = new ObservableCollection<Task>(
                    _workTaskProvider.GetTasksByWorkId(workId.Value).OrderByDescending(x => x.LastUpdatedTime));
            }
            else
            {
                SelectedBusinessTasks = new ObservableCollection<Task>();
            }
        }

        private void LoadSelectedBusinessOwners(Guid? businessId)
        {
            if (businessId != null)
            {
                SelectedBusinessOwners = new ObservableCollection<Owner>(_businessOwnerService.GetOwnersByBusinessId(businessId.Value));
            }
        }

        private void LoadSelectedBusinessInfos(Guid? businessId)
        {
            if (businessId != null)
            {
                SelectedBusinessInfos = new ObservableCollection<BusinessInfo>(_businessOwnerService.GetBusinessInfosByBusinessId(businessId.Value));
            }
        }

        private void LoadSelectedBusinessNotes(Guid? businessId)
        {
            if (businessId != null)
            {
                SelectedBusinessNotes = new ObservableCollection<Note>(_businessOwnerService.GetNotesByBusinessId(businessId.Value));
            }
        }

        private void OpenEditBusinessDialog(Guid? businessId)
        {
            if (businessId == null)
            {
                return;
            }

            var parameters = new DialogParameters($"BusinessId={businessId}");

            _dialogService.ShowDialog(nameof(Views.BusinessDetails), parameters, dialog => 
            {
                // TODO: Move code into HandleBusinessUpdatedEvent
                if (dialog.Result == ButtonResult.OK)
                {
                    var updated = _businessOwnerService.GetBusinessByIdWithFullDetails(businessId.Value);
                    var existing = BusinessList.FirstOrDefault(x => x.Id == businessId);

                    if (existing != null)
                    {
                        existing = updated;
                        SelectedBusiness = updated;
                    }
                }
            });
        }

        private void OpenNewTaskDialog(Guid? businessId)
        {
            if (businessId == null)
            {
                return;
            }

            var parameters = new DialogParameters($"BusinessId={businessId}");

            _dialogService.ShowDialog(nameof(Views.TaskDetails), parameters, p => 
            {
                if (p.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessTasks(SelectedBusiness?.Work?.Id);
                }
            });
        }

        private void OpenEditTaskDialog(int? taskId)
        {
            if (taskId == null)
            {
                return;
            }

            var parameters = new DialogParameters($"TaskId={taskId}");

            _dialogService.ShowDialog(nameof(Views.TaskDetails), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessTasks(SelectedBusiness?.Work?.Id);
                }
            });
        }

        private void OpenNewOwnerDialog(Guid? businessId)
        {
            if (businessId == null)
            {
                return;
            }

            var parameters = businessId != null
                ? new DialogParameters($"BusinessId={businessId}")
                : new DialogParameters();

            _dialogService.ShowDialog(nameof(Views.OwnerDetails), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessOwners(SelectedBusiness?.Id);
                }
            });
        }

        private void OpenEditOwnerDialog(Guid? ownerId)
        {
            if (ownerId == null)
            {
                return;
            }

            var parameters = new DialogParameters($"OwnerId={ownerId}");

            _dialogService.ShowDialog(nameof(Views.OwnerDetails), parameters, p => 
            {
                if (p.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessOwners(SelectedBusiness?.Id);
                }
            });
        }

        private void OpenNewBusinessInfoDialog()
        {
            if (SelectedBusiness == null)
            {
                return;
            }

            var parameters = new DialogParameters($"BusinessId={SelectedBusiness.Id}");
            _dialogService.ShowDialog(nameof(Views.BankAccountDetails), parameters, d => 
            {
                if (d.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessInfos(SelectedBusiness.Id);
                }
            });
        }

        private void OpenEditBusinessInfoDialog(int? businessInfoId)
        {
            if (businessInfoId == null)
            {
                return;
            }

            var parameters = new DialogParameters($"BusinessInfoId={businessInfoId}");
            _dialogService.ShowDialog(nameof(Views.BankAccountDetails), parameters, d => 
            {
                if (d.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessInfos(SelectedBusiness?.Id);
                }
            });
        }

        private void OpenNewNoteDialog()
        {
            if (SelectedBusiness == null)
            {
                return;
            }

            var parameters = new DialogParameters($"BusinessId={SelectedBusiness.Id}");

            _dialogService.ShowDialog(nameof(Views.NoteDetails), parameters, d => 
            {
                if (d.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessNotes(SelectedBusiness.Id);
                }
            });
        }

        private void OpenEditNoteDialog(int? noteId)
        {
            if (noteId == null)
            {
                return;
            }

            var parameters = new DialogParameters($"NoteId={noteId}");

            _dialogService.ShowDialog(nameof(Views.NoteDetails), parameters, d =>
            {
                if (d.Result == ButtonResult.OK)
                {
                    LoadSelectedBusinessNotes(SelectedBusiness.Id);
                }
            });
        }

        private void DeleteNoteById(Note note)
        {
            if (note == null)
            {
                return;
            }

            _dialogService.ShowConfirmation("Confirm Delete", $"Are you sure you want to delete the Note [{note.Header}]?",
                a =>
                {
                    if (a.Result == ButtonResult.OK)
                    {
                        _businessOwnerService.DeleteNote(note.Id);

                        // _eventAggregator.GetEvent<Events.BusinessUpsertedEvent>().Publish(note.BusinessId);

                        LoadSelectedBusinessNotes(note.BusinessId);
                    }
                });
        }

        private void RefreshSelectedBusinessInformation()
        {
            if (SelectedBusiness == null)
            {
                return;
            }

            // BusinessFullDetails = _userAccountProvider.GetBusinessByIdWithFullDetails(SelectedBusiness.Id);
            LoadSelectedBusinessTasks(SelectedBusiness.Work?.Id);
            LoadSelectedBusinessOwners(SelectedBusiness.Id);
            LoadSelectedBusinessInfos(SelectedBusiness.Id);
            LoadSelectedBusinessNotes(SelectedBusiness.Id);

            RaisePropertyChanged("SelectedHSTAccount");
            RaisePropertyChanged("SelectedCorporationTaxAccount");
            RaisePropertyChanged("SelectedPSTAccount");
            RaisePropertyChanged("SelectedWSIBAccount");
            RaisePropertyChanged("SelectedLIQAccount");
            RaisePropertyChanged("SelectedONTAccount");
            RaisePropertyChanged("SelectedRegularPayment");
            RaisePropertyChanged("SelectedClientPayment2nd");
            RaisePropertyChanged("SelectedPayrollAccount");
        }

        private bool StringContainsFilterText(string searchString, string filterText)
        {
            return string.IsNullOrWhiteSpace(searchString) == false
                   && searchString.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }

        #region INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("BusinessOperatingName"))
            {
                var businessOperatingName = navigationContext.Parameters.GetValue<string>("BusinessOperatingName");

                BusinessFilterText = businessOperatingName;
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // do nothing
        }
        #endregion
    }
}
