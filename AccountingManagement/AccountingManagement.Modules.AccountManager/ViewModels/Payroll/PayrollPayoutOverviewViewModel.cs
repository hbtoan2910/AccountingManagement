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
    public class PayrollPayoutOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView PayoutRecordsView => CollectionViewSource.View;

        private ObservableCollection<BusinessPayoutModel> _businessPayoutModels;
        public ObservableCollection<BusinessPayoutModel> BusinessPayoutModels
        {
            get { return _businessPayoutModels; }
            set { SetProperty(ref _businessPayoutModels, value); }
        }

        public int FilteredItemCount
        {
            get { return PayoutRecordsView.Cast<object>()?.Count() ?? 0; }
        }

        public List<PayrollPeriodLookup> ExistingPayrollPeriods { get; private set; }

        private PayrollPeriodLookup _selectedPayrollPeriod;
        public PayrollPeriodLookup SelectedPayrollPeriod
        {
            get { return _selectedPayrollPeriod; }
            set
            {
                if (SetProperty(ref _selectedPayrollPeriod, value))
                {
                    ReloadPayoutRecords();

                    RaisePropertyChanged("PayoutRecordsView");
                }
            }
        }

        private string _payoutOverviewFilterText;
        public string PayoutOverviewFilterText
        {
            get { return _payoutOverviewFilterText; }
            set
            {
                if (SetProperty(ref _payoutOverviewFilterText, value))
                {
                    RefreshView();
                }
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetProperty(ref _isExpanded, value); }
        }

        public List<FilingCycle> PayrollCycles
        {
            get
            {
                return new List<FilingCycle>()
                {
                    FilingCycle.BiWeekly,
                    FilingCycle.SemiMonthly,
                    FilingCycle.Monthly,
                };
            }
        }

        private FilingCycle? _selectedPayrollCycle;
        public FilingCycle? SelectedPayrollCycle
        {
            get { return _selectedPayrollCycle; }
            set
            {
                if (SetProperty(ref _selectedPayrollCycle, value))
                {
                    RefreshView();
                }
            }
        }

        private PayrollProgress _payoutProgressFilter;
        public PayrollProgress PayoutProgressFilter
        {
            get { return _payoutProgressFilter; }
            set
            {
                if (SetProperty(ref _payoutProgressFilter, value))
                {
                    RefreshView();
                }
            }
        }

        public DelegateCommand RefreshPayoutOverviewCommand { get; private set; }
        public DelegateCommand OpenGeneratePayrollDialogCommand { get; private set; }

        public DelegateCommand<PayrollPayoutRecord> PayoutRecordEditCommand { get; private set; }
        public DelegateCommand<PayrollPayoutRecord> UpdatePayrollPayout1StatusCommand { get; private set; }
        public DelegateCommand<PayrollPayoutRecord> UpdatePayrollPayout2StatusCommand { get; private set; }
        public DelegateCommand<PayrollPayoutRecord> UpdatePayrollPayout3StatusCommand { get; private set; }

        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        #endregion


        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        private readonly IGlobalService _globalService;
        private readonly IPayrollService _payrollService;
        private readonly IPayrollProvider _payrollProvider;

        public PayrollPayoutOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService, IPayrollService payrollService, IPayrollProvider payrollProvider)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            _globalService = globalService;
            _payrollService = payrollService;
            _payrollProvider = payrollProvider;

            Initialize();
        }

        private void ReloadPayoutRecords()
        {
            if (SelectedPayrollPeriod == null)
            {
                return;
            }

            var payoutRecords = _payrollService.GetBusinessPayrollPayoutRecordsForPeriod(SelectedPayrollPeriod.PayrollPeriod);

            if (payoutRecords.Count > 0)
            {
                BusinessPayoutModels = new ObservableCollection<BusinessPayoutModel>(
                    payoutRecords
                        .Where(x => x.PayrollAccount.PayrollPayoutRecords.Count > 0)
                        .Select(x => new BusinessPayoutModel(x, SelectedPayrollPeriod.PayrollPeriod)));
            }
            else
            {
                BusinessPayoutModels = new ObservableCollection<BusinessPayoutModel>();
            }

            CollectionViewSource.Source = BusinessPayoutModels;
        }

        private void RefreshView()
        {
            PayoutRecordsView.Refresh();
            RaisePropertyChanged("FilteredItemCount");
        }

        private void Initialize()
        {
            RefreshPayoutOverviewCommand = new DelegateCommand(RefreshPayoutOverview);
            OpenGeneratePayrollDialogCommand = new DelegateCommand(OpenGeneratePayrollDialog);

            PayoutRecordEditCommand = new DelegateCommand<PayrollPayoutRecord>(OpenPayoutRecordEdit);
            UpdatePayrollPayout1StatusCommand = new DelegateCommand<PayrollPayoutRecord>(UpdatePayrollPayout1Status);
            UpdatePayrollPayout2StatusCommand = new DelegateCommand<PayrollPayoutRecord>(UpdatePayrollPayout2Status);
            UpdatePayrollPayout3StatusCommand = new DelegateCommand<PayrollPayoutRecord>(UpdatePayrollPayout3Status);

            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);

            ExistingPayrollPeriods = _payrollService.GetLatestPayrollPeriodLookups(12);
            SelectedPayrollPeriod = ExistingPayrollPeriods.FirstOrDefault();

            CollectionViewSource.Filter += (s, e) =>
            {
                var model = e.Item as BusinessPayoutModel;

                if (SelectedPayrollCycle != null)
                {
                    if (model.PayrollAccount.PayrollCycle != SelectedPayrollCycle.Value)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(PayoutOverviewFilterText) == false)
                {
                    if (model.Business.LegalName.Contains(PayoutOverviewFilterText, StringComparison.InvariantCultureIgnoreCase) == false
                        && model.Business.OperatingName.Contains(PayoutOverviewFilterText, StringComparison.InvariantCultureIgnoreCase) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (PayoutProgressFilter != PayrollProgress.All)
                {
                    if (PayoutProgressFilter == PayrollProgress.Payroll1Pending && (model.PayoutRecord.PayrollPayout1DueDate == null || model.PayoutRecord.PayrollPayout1Status == PayrollStatus.Done)
                        || PayoutProgressFilter == PayrollProgress.Payroll2Pending && (model.PayoutRecord.PayrollPayout2DueDate == null || model.PayoutRecord.PayrollPayout2Status == PayrollStatus.Done)
                        || PayoutProgressFilter == PayrollProgress.Payroll3Pending && (model.PayoutRecord.PayrollPayout3DueDate == null || model.PayoutRecord.PayrollPayout3Status == PayrollStatus.Done))
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending)
            });
        }

        private void RefreshPayoutOverview()
        {
            var previousSelectedStr = SelectedPayrollPeriod.PayrollPeriod;

            ExistingPayrollPeriods = _payrollService.GetLatestPayrollPeriodLookups(16);
            RaisePropertyChanged("ExistingPayrollPeriods");

            var previousSelected = ExistingPayrollPeriods.FirstOrDefault(x => x.PayrollPeriod.Equals(previousSelectedStr));
            if (previousSelected != null)
            {
                SelectedPayrollPeriod = previousSelected;
            }
            else
            {
                SelectedPayrollPeriod = ExistingPayrollPeriods.First();
            }
        }

        private void OpenGeneratePayrollDialog()
        {
            var parameters = new DialogParameters();

            _dialogService.ShowDialog(nameof(Views.PayrollPeriodGenerator), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    RefreshPayoutOverview();
                }
            });
        }

        private void OpenPayoutRecordEdit(PayrollPayoutRecord payoutRecord)
        {
            if (payoutRecord == null)
            {
                return;
            }

            var parameters = new DialogParameters($"PayoutRecordId={payoutRecord.Id}");

            _dialogService.ShowDialog(nameof(Views.PayrollPayoutEdit), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    var updatedRecord = _payrollService.GetPayrollPayoutRecordById(payoutRecord.Id);

                    var oldRecord = BusinessPayoutModels.FirstOrDefault(x => x.PayoutRecord.Id == payoutRecord.Id);
                    if (oldRecord != null)
                    {
                        oldRecord.PayoutRecord = updatedRecord;

                        PayoutRecordsView.Refresh();
                    }
                }
            });
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

        private void UpdatePayrollPayout1Status(PayrollPayoutRecord payoutRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payoutRecord.PayrollPayout1Status.ToString());

            payoutRecord.PayrollPayout1UpdatedBy = updatedByText;

            _payrollProvider.UpdatePayoutRecordDueDate1Status(payoutRecord.Id, payoutRecord.PayrollPayout1Status, updatedByText);

            PayoutRecordsView.Refresh();
        }

        private void UpdatePayrollPayout2Status(PayrollPayoutRecord payoutRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payoutRecord.PayrollPayout2Status.ToString());

            payoutRecord.PayrollPayout2UpdatedBy = updatedByText;

            _payrollProvider.UpdatePayoutRecordDueDate2Status(payoutRecord.Id, payoutRecord.PayrollPayout2Status, updatedByText);

            PayoutRecordsView.Refresh();
        }

        private void UpdatePayrollPayout3Status(PayrollPayoutRecord payoutRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payoutRecord.PayrollPayout3Status.ToString());

            payoutRecord.PayrollPayout3UpdatedBy = updatedByText;

            _payrollProvider.UpdatePayoutRecordDueDate3Status(payoutRecord.Id, payoutRecord.PayrollPayout3Status, updatedByText);

            PayoutRecordsView.Refresh();
        }

        private string GenerateLastUpdatedText(string username, string newValue)
        {
            var lastUpdatedText = $"[{DateTime.Now.ToString(Constant.DateTimeFormat)}]: [{username}] updated to [{newValue}]";

            return lastUpdatedText.Length > LastUpdatedTextCharacterLimit
                ? lastUpdatedText.Substring(0, LastUpdatedTextCharacterLimit)
                : lastUpdatedText;
        }

        private const int LastUpdatedTextCharacterLimit = 70;

    }
}
