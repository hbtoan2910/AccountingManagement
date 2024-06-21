using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using Prism.Regions;
using AccountingManagement.Core;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Modules.AccountManager.Helpers;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxAccountHistoryViewModel : ViewModelBase, INavigationAware
    {
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView TaxFilingLogsView => CollectionViewSource.View;

        private ObservableCollection<TaxFilingLog> _taxFilingLogs;
        public ObservableCollection<TaxFilingLog> TaxFilingLogs
        {
            get { return _taxFilingLogs; }
            set { SetProperty(ref _taxFilingLogs, value); }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    TaxFilingLogsView.Refresh();
                }
            }
        }

        public List<TaxAccountType> TaxAccountTypes
        {
            get
            {
                return new List<TaxAccountType>
                {
                    TaxAccountType.HST,
                    TaxAccountType.Corporation,
                    TaxAccountType.PST,
                    TaxAccountType.WSIB,
                    TaxAccountType.LIQ,
                    TaxAccountType.ONT,
                };
            }
        }

        private TaxAccountType _selectedTaxAccountType = TaxAccountType.HST;
        public TaxAccountType SelectedTaxAccountType
        {
            get { return _selectedTaxAccountType; }
            set
            {
                if (SetProperty(ref _selectedTaxAccountType, value))
                {
                    LoadTaxFilingLogs();

                    RaisePropertyChanged("TaxFilingLogsView");
                }
            }
        }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        public DelegateCommand NavigateToTaxAccountOverviewCommand { get; private set; }

        private readonly IRegionManager _regionManager;
        private readonly ITaxAccountService _taxAccountService;

        private string _previousPage = string.Empty;

        public TaxAccountHistoryViewModel(IRegionManager regionManager, ITaxAccountService taxAccountService)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));

            Initialize();
        }

        private void Initialize()
        {
            RefreshPageCommand = new DelegateCommand(RefreshPage);
            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);
            NavigateToTaxAccountOverviewCommand = new DelegateCommand(NavigateToTaxAccountOverview);

            LoadTaxFilingLogs();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is TaxFilingLog log))
                {
                    e.Accepted = false;
                    return;
                }

                if (log.AccountType != SelectedTaxAccountType)
                {
                    e.Accepted = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(BusinessFilterText) == false)
                {
                    if (FilterHelper.StringContainsFilterText(log.Business.LegalName, BusinessFilterText) == false
                        && FilterHelper.StringContainsFilterText(log.Business.OperatingName, BusinessFilterText) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            SortByUpdatedTimestamp();
        }

        private void LoadTaxFilingLogs()
        {
            var cutoffDate = DateTime.Now.AddYears(-1);
            var logs = _taxAccountService.GetTaxFilingLogs(cutoffDate);

            if (logs.Count > 0)
            {
                TaxFilingLogs = new ObservableCollection<TaxFilingLog>(logs);
            }
            else
            {
                TaxFilingLogs = new ObservableCollection<TaxFilingLog>();
            }

            CollectionViewSource.Source = TaxFilingLogs;
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

        private void NavigateToTaxAccountOverview()
        {
            var navParams = new NavigationParameters($"");

            if (string.IsNullOrWhiteSpace(_previousPage) == false)
            {
                _regionManager.RequestNavigate(RegionNames.MainViewRegion, _previousPage, navParams);
                return;
            }

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaxAccountOverview, navParams);
        }

        private void RefreshPage()
        {
            LoadTaxFilingLogs();

            RaisePropertyChanged("TaxFilingLogsView");

            TaxFilingLogsView.Refresh();
        }

        private void SortByUpdatedTimestamp()
        {
            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("Timestamp", ListSortDirection.Descending),
            });
        }

        #region INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.TryGetValue<string>("PreviousPage", out var previousPage))
            {
                _previousPage = previousPage;
            }

            if (navigationContext.Parameters.TryGetValue<TaxAccountType>("TaxType", out var taxType))
            {
                SelectedTaxAccountType = taxType;
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
