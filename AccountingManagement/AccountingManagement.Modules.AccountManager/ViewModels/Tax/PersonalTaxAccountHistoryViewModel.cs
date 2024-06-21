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
    public class PersonalTaxAccountHistoryViewModel : ViewModelBase
    {
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView AccountLogsView => CollectionViewSource.View;

        private ObservableCollection<PersonalTaxAccountLog> _personalTaxAccountLogs;
        public ObservableCollection<PersonalTaxAccountLog> PersonalTaxAccountLogs
        {
            get { return _personalTaxAccountLogs; }
            set { SetProperty(ref _personalTaxAccountLogs, value); }
        }

        private string _ownerFilterText;
        public string OwnerFilterText
        {
            get { return _ownerFilterText; }
            set
            {
                if (SetProperty(ref _ownerFilterText, value))
                {
                    AccountLogsView.Refresh();
                }
            }
        }

        private string _taxYearFilter;
        public string TaxYearFilter
        {
            get { return _taxYearFilter; }
            set
            {
                if (SetProperty(ref _taxYearFilter, value))
                {
                    AccountLogsView.Refresh();
                }
            }
        }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand<Owner> NavigateToOwnerOverviewCommand { get; private set; }
        public DelegateCommand NavigateToPersonalTaxAccountOverviewCommand { get; private set; }

        private readonly IRegionManager _regionManager;
        private readonly ITaxAccountService _taxAccountService;

        public PersonalTaxAccountHistoryViewModel(IRegionManager regionManager, ITaxAccountService taxAccountService)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));

            Initialize();
        }

        private void Initialize()
        {
            RefreshPageCommand = new DelegateCommand(RefreshPage);
            NavigateToOwnerOverviewCommand = new DelegateCommand<Owner>(NavigateToOwnerOverview);
            NavigateToPersonalTaxAccountOverviewCommand = new DelegateCommand(NavigateToPersonalTaxAccountOverview);

            LoadAccountLogs();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is PersonalTaxAccountLog log))
                {
                    e.Accepted = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(OwnerFilterText) == false)
                {
                    if (FilterHelper.StringContainsFilterText(log.Owner.Name, OwnerFilterText) == false
                        && FilterHelper.StringContainsFilterText(log.Owner.SIN, OwnerFilterText) == false
                        && FilterHelper.StringContainsFilterText(log.Owner.PhoneNumber, OwnerFilterText) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(TaxYearFilter) == false)
                {
                    if (FilterHelper.StringContainsFilterText(log.TaxYear, TaxYearFilter) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            SortByUpdatedTimestamp();
        }

        private void LoadAccountLogs()
        {
            var taxType = PersonalTaxType.T1;
            var accountLogs = _taxAccountService.GetPersonalTaxAccountLogsByType(taxType);

            if (accountLogs.Count > 0)
            {
                PersonalTaxAccountLogs = new ObservableCollection<PersonalTaxAccountLog>(accountLogs);
            }
            else
            {
                PersonalTaxAccountLogs = new ObservableCollection<PersonalTaxAccountLog>();
            }

            CollectionViewSource.Source = PersonalTaxAccountLogs;
        }


        private void NavigateToOwnerOverview(Owner owner)
        {
            if (owner == null)
            {
                return;
            }

            var navParams = new NavigationParameters($"OwnerName={owner.Name}");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.OwnerOverview, navParams);
        }

        private void NavigateToPersonalTaxAccountOverview()
        {
            var navParams = new NavigationParameters($"");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.PersonalTaxAccountOverview, navParams);
        }

        private void RefreshPage()
        {
            LoadAccountLogs();

            // TODO: Can I skip this step?
            RaisePropertyChanged("AccountLogsView");

            // TODO: Or this step?
            AccountLogsView.Refresh();
        }

        private void SortByUpdatedTimestamp()
        {
            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("Timestamp", ListSortDirection.Descending),
            });
        }
    }
}
