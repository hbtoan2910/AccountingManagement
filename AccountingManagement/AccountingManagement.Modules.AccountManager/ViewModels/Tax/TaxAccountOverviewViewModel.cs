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

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxAccountOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView BusinessTaxAccountsView => CollectionViewSource.View;

        private ObservableCollection<BusinessTaxAccountModel> _taxAccountModels;
        public ObservableCollection<BusinessTaxAccountModel> TaxAccountModels
        {
            get { return _taxAccountModels; }
            set { SetProperty(ref _taxAccountModels, value); }
        }

        private BusinessTaxAccountModel _selectedTaxAccountModel;
        public BusinessTaxAccountModel SelectedTaxAccountModel
        {
            get { return _selectedTaxAccountModel; }
            set { SetProperty(ref _selectedTaxAccountModel, value); }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    BusinessTaxAccountsView.Refresh();
                }
            }
        }

        private string _endingPeriodHeaderText = "Ending Period";
        public string EndingPeriodHeaderText
        {
            get { return _endingPeriodHeaderText; }
            set { SetProperty(ref _endingPeriodHeaderText, value); }
        }

        private string _dueDateHeaderText = "Due Date";
        public string DueDateHeaderText
        {
            get { return _dueDateHeaderText; }
            set { SetProperty(ref _dueDateHeaderText, value); }
        }

        private Dictionary<TaxAccountType, string> _taxAccountTypes = new Dictionary<TaxAccountType, string>
        {
            { TaxAccountType.PST, "PST" },
            { TaxAccountType.WSIB, "WSIB/WCB" },
            { TaxAccountType.LIQ, "LIQ" },
            { TaxAccountType.ONT, "Annual Return" },
        };
        public Dictionary<TaxAccountType, string> TaxAccountTypes { get { return _taxAccountTypes; } }

        private TaxAccountType _selectedTaxAccountType = TaxAccountType.PST;
        public TaxAccountType SelectedTaxAccountType
        {
            get { return _selectedTaxAccountType; }
            set
            {
                if (SetProperty(ref _selectedTaxAccountType, value))
                {
                    LoadTaxAccounts();

                    RaisePropertyChanged("BusinessTaxAccountsView");
                }
            }
        }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand SortByDueDateCommand { get; private set; }
        public DelegateCommand SortByEndingPeriodCommand { get; private set; }

        public DelegateCommand<BusinessTaxAccountModel> ConfirmFilingCommand { get; private set; }
        public DelegateCommand<TaxAccount> OpenTaxAccountDetailsDialogCommand { get; private set; }

        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        public DelegateCommand NavigateToTaxAccountHistoryCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;
        private readonly ITaxAccountService _taxAccountService;
        private readonly IFilingHandler _filingHandler;

        public TaxAccountOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService, ITaxAccountService taxAccountService, IFilingHandler filingHandler)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _filingHandler = filingHandler ?? throw new ArgumentNullException(nameof(filingHandler));

            Initialize();
        }

        private void Initialize()
        {
            RefreshPageCommand = new DelegateCommand(RefreshPage);
            SortByDueDateCommand = new DelegateCommand(SortByDueDate);
            SortByEndingPeriodCommand = new DelegateCommand(SortByEndingPeriod);

            ConfirmFilingCommand = new DelegateCommand<BusinessTaxAccountModel>(ConfirmFiling);
            OpenTaxAccountDetailsDialogCommand = new DelegateCommand<TaxAccount>(OpenTaxAccountDetailsDialog);

            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);
            NavigateToTaxAccountHistoryCommand = new DelegateCommand(NavigateToTaxAccountHistory);

            LoadTaxAccounts();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is BusinessTaxAccountModel model) || string.IsNullOrWhiteSpace(BusinessFilterText))
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

            SortByDueDate();
        }

        private void LoadTaxAccounts()
        {
            var taxAccounts = _taxAccountService.GetTaxAccountsByAccountType(SelectedTaxAccountType);

            if (taxAccounts.Count > 0)
            {
                TaxAccountModels = new ObservableCollection<BusinessTaxAccountModel>(
                    taxAccounts.Select(x => new BusinessTaxAccountModel(x)));
            }
            else
            {
                TaxAccountModels = new ObservableCollection<BusinessTaxAccountModel>();
            }

            CollectionViewSource.Source = TaxAccountModels;
        }


        private void ConfirmFiling(BusinessTaxAccountModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            try
            {
                _filingHandler.ConfirmTaxFiling(model.TaxAccount, model.ConfirmText, confirmDate, currentUserId);

                model.ConfirmText = string.Empty;

                var oldRecord = TaxAccountModels.FirstOrDefault(x => x.TaxAccount.Id == model.TaxAccount.Id);
                var updated = _taxAccountService.GetTaxAccountById(model.TaxAccount.Id);

                if (oldRecord != null && updated != null)
                {
                    oldRecord.TaxAccount = updated;

                    BusinessTaxAccountsView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to confirm Tax Filing. {ex.Message}");
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

        private void NavigateToTaxAccountHistory()
        {
            var navParams = new NavigationParameters();
            navParams.Add("PreviousPage", ViewRegKeys.TaxAccountOverview);
            navParams.Add("TaxType", SelectedTaxAccountType);

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaxAccountHistory, navParams);
        }

        private void OpenTaxAccountDetailsDialog(TaxAccount taxAccount)
        {
            if (taxAccount == null)
            {
                return;
            }

            var parameters = new DialogParameters($"TaxAccountId={taxAccount.Id}");

            _dialogService.ShowDialog(nameof(Views.TaxAccountDetails), parameters, d =>
            {
                if (d.Result == ButtonResult.OK)
                {
                    var updatedRecord = _taxAccountService.GetTaxAccountById(taxAccount.Id);

                    var oldRecord = TaxAccountModels.FirstOrDefault(x => x.TaxAccount.Id == taxAccount.Id);
                    if (oldRecord != null)
                    {
                        if (updatedRecord.IsActive == false)
                        {
                            TaxAccountModels.Remove(oldRecord);
                        }
                        else
                        {
                            oldRecord.TaxAccount = updatedRecord;
                        }

                        BusinessTaxAccountsView.Refresh();
                    }
                }
            });
        }

        private void RefreshPage()
        {
            LoadTaxAccounts();

            // TODO: Can I skip this step?
            RaisePropertyChanged("BusinessTaxAccountsView");

            // TODO: Or this step?
            BusinessTaxAccountsView.Refresh();
        }

        private void SortByDueDate()
        {
            DueDateHeaderText = "Due Date *";
            EndingPeriodHeaderText = "Ending Period";

            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[] 
            {
                new SortDescription("TaxAccount.DueDate", ListSortDirection.Ascending),
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending),
            });
        }

        private void SortByEndingPeriod()
        {
            DueDateHeaderText = "Due Date";
            EndingPeriodHeaderText = "Ending Period *";

            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("TaxAccount.EndingPeriod", ListSortDirection.Ascending),
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending),
            });
        }

        private bool StringContainsFilterText(string input, string filterText)
        {
            return string.IsNullOrWhiteSpace(input) == false && input.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
