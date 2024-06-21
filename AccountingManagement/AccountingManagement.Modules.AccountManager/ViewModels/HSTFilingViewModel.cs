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
using AccountingManagement.Modules.AccountManager.Models;
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class HSTFilingViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView BusinessHSTFilingView => CollectionViewSource.View;

        private ObservableCollection<BusinessHSTAccountModel> _hstAccountModels;
        public ObservableCollection<BusinessHSTAccountModel> HSTAccountModels
        {
            get { return _hstAccountModels; }
            set { SetProperty(ref _hstAccountModels, value); }
        }

        private BusinessHSTAccountModel _selectedHSTAccountModel;
        public BusinessHSTAccountModel SelectedHSTAccountModel
        {
            get { return _selectedHSTAccountModel; }
            set { SetProperty(ref _selectedHSTAccountModel, value); }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            { 
                if (SetProperty(ref _businessFilterText, value))
                {
                    BusinessHSTFilingView.Refresh();
                }
            }
        }

        private string _hstEndingPeriodHeaderText = "Ending Period";
        public string HstEndingPeriodHeaderText
        {
            get { return _hstEndingPeriodHeaderText; }
            set { SetProperty(ref _hstEndingPeriodHeaderText, value); }
        }

        private string _hstDueDateHeaderText = "Due Date";
        public string HstDueDateHeaderText
        {
            get { return _hstDueDateHeaderText; }
            set { SetProperty(ref _hstDueDateHeaderText, value); }
        }

        public DelegateCommand SortByHstDueDateCommand { get; private set; }
        public DelegateCommand SortByHstEndingPeriodCommand { get; private set; }

        public DelegateCommand<BusinessHSTAccountModel> ConfirmFilingCommand { get; private set; }
        public DelegateCommand<BusinessHSTAccountModel> ConfirmInstalmentCommand { get; private set; }
        public DelegateCommand<HSTAccount> OpenHstAccountDialogCommand { get; private set; }

        public DelegateCommand GenerateMissingHSTAccountCommand { get; private set; }
        public DelegateCommand RefreshPageCommand { get; private set; }

        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;
        private readonly IDataProvider _dataProvider;
        private readonly IFilingHandler _filingHandler;

        public HSTFilingViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService, IDataProvider dataProvider, IFilingHandler filingHandler)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _filingHandler = filingHandler ?? throw new ArgumentNullException(nameof(filingHandler));

            Initialize();
        }

        private void Initialize()
        {
            _eventAggregator.GetEvent<Events.BusinessDeletedEvent>().Subscribe(HandleBusinessDeletedEvent);

            SortByHstEndingPeriodCommand = new DelegateCommand(SortByHstEndingPeriod);
            SortByHstDueDateCommand = new DelegateCommand(SortByHstDueDate);

            ConfirmFilingCommand = new DelegateCommand<BusinessHSTAccountModel>(ConfirmFiling);
            ConfirmInstalmentCommand = new DelegateCommand<BusinessHSTAccountModel>(ConfirmInstalment);
            OpenHstAccountDialogCommand = new DelegateCommand<HSTAccount>(OpenHstAccountDialog);

            GenerateMissingHSTAccountCommand = new DelegateCommand(GenerateMissingHSTAccount);
            RefreshPageCommand = new DelegateCommand(RefreshPage);

            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);

            LoadHSTAccounts();

            CollectionViewSource.Filter += (s, e) => 
            {
                var model = e.Item as BusinessHSTAccountModel;

                if (string.IsNullOrWhiteSpace(BusinessFilterText))
                {
                    e.Accepted = true;
                    return;
                }

                if (StringContainsFilterText(model.Business.LegalName, BusinessFilterText)
                    || StringContainsFilterText(model.Business.OperatingName, BusinessFilterText))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            };

            SortByHstDueDate();
        }

        private void LoadHSTAccounts()
        {
            // TODO: Retire HST specific forms
            HSTAccountModels = new ObservableCollection<BusinessHSTAccountModel>();

            //var hstAccounts = _userAccountProvider.GetHSTAccounts();

            //if (hstAccounts?.Count() > 0)
            //{
            //    HSTAccountModels = new ObservableCollection<BusinessHSTAccountModel>(
            //        hstAccounts.Select(x => new BusinessHSTAccountModel(x)));
            //}
            //else
            //{
            //    HSTAccountModels = new ObservableCollection<BusinessHSTAccountModel>();
            //}

            CollectionViewSource.Source = HSTAccountModels;
        }


        private void ConfirmFiling(BusinessHSTAccountModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            try
            {
                // _filingHandler.ConfirmHSTFiling(model.HSTAccount, model.HSTConfirmText, confirmDate, currentUserId);

                model.HSTConfirmText = string.Empty;

                // Another solution is update hstAccount.HSTEndingPeriod/DueDate in _filingHandler.ConfirmHSTFiling method
                // but I'm not sure about including that logic in _filingHandler.ConfirmHSTFiling is a good idea
                // This solution, however, requires one additional DB call
                var oldRecord = HSTAccountModels.FirstOrDefault(x => x.HSTAccount.Id == model.HSTAccount.Id);
                var updated = new HSTAccount(); // _userAccountProvider.GetHSTAccountById(model.HSTAccount.Id);

                if (oldRecord != null && updated != null)
                {
                    oldRecord.HSTAccount = updated;

                    BusinessHSTFilingView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to confirm Tax Filing. {ex.Message}");
            }
        }

        private void ConfirmInstalment(BusinessHSTAccountModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            try
            {
                // _filingHandler.ConfirmInstalmentPayment(model.HSTAccount, model.InstalmentConfirmText, confirmDate, currentUserId);

                model.InstalmentConfirmText = string.Empty;

                var oldRecord = HSTAccountModels.FirstOrDefault(x => x.HSTAccount.Id == model.HSTAccount.Id);
                var updated = new HSTAccount(); // _userAccountProvider.GetHSTAccountById(model.HSTAccount.Id);

                if (oldRecord != null && updated != null)
                {
                    oldRecord.HSTAccount = updated;

                    BusinessHSTFilingView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR", $"{ex.Message}");
            }
        }

        private void GenerateMissingHSTAccount()
        {
            try
            {
                // TODO: Fix Generate
                // _filingHandler.GenerateMissingHSTAccounts();

                _dialogService.ShowInformation("Information", "Placeholder HST Accounts generated!");

                RefreshPage();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error while generating placeholder HST Accounts. {ex.Message}");

                _dialogService.ShowError(ex, "ERROR", $"Error while generating placeholder HST Accounts. {ex.Message}");
            }
        }

        private void HandleBusinessDeletedEvent(Guid businessId)
        {
            var existing = HSTAccountModels.FirstOrDefault(x => x.Business.Id == businessId);
            if (existing != null)
            {
                HSTAccountModels.Remove(existing);

                BusinessHSTFilingView.Refresh();
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

        private void OpenHstAccountDialog(HSTAccount hstAccount)
        {
            if (hstAccount == null)
            {
                return;
            }

            var parameters = new DialogParameters($"HSTAccountId={hstAccount.Id}");

            _dialogService.ShowDialog(nameof(Views.HSTDetails), parameters, a => 
            {
                if (a.Result == ButtonResult.OK)
                {
                    var updatedRecord = new HSTAccount(); // _userAccountProvider.GetHSTAccountById(hstAccount.Id);

                    var oldRecord = HSTAccountModels.FirstOrDefault(x => x.HSTAccount.Id == hstAccount.Id);
                    if (oldRecord != null)
                    {
                        if (updatedRecord.IsRunHST == false)
                        {
                            HSTAccountModels.Remove(oldRecord);
                        }
                        else
                        {
                            oldRecord.HSTAccount = updatedRecord;
                        }

                        BusinessHSTFilingView.Refresh();
                    }
                }
            });
        }

        private void RefreshPage()
        {
            LoadHSTAccounts();

            // TODO: Can I skip one?
            RaisePropertyChanged("BusinessHSTFilingView");

            BusinessHSTFilingView.Refresh();
        }

        private void SortByHstEndingPeriod()
        {
            HstEndingPeriodHeaderText = "Ending Period *";
            HstDueDateHeaderText = "Due Date";

            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("HSTAccount.HSTEndingPeriod", ListSortDirection.Ascending),
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending)
            });
        }

        private void SortByHstDueDate()
        {
            HstEndingPeriodHeaderText = "Ending Period";
            HstDueDateHeaderText = "Due Date *";

            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("HSTAccount.HSTDueDate", ListSortDirection.Ascending),
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending)
            });
        }

        private bool StringContainsFilterText(string input, string filterText)
        {
            return string.IsNullOrWhiteSpace(input) == false && input.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
