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
    public class PayrollYearEndOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView PayrollYearEndRecordsView => CollectionViewSource.View;

        private ObservableCollection<PayrollYearEndRecordModel> _payrollYearEndRecordModels;
        public ObservableCollection<PayrollYearEndRecordModel> PayrollYearEndRecordModels
        {
            get { return _payrollYearEndRecordModels; }
            set { SetProperty(ref _payrollYearEndRecordModels, value); }
        }

        public int FilteredItemCount
        {
            get { return PayrollYearEndRecordsView.Cast<object>()?.Count() ?? 0; }
        }

        private PayrollYearEndRecordModel _selectedRecordModel;
        public PayrollYearEndRecordModel SelectedRecordModel
        {
            get { return _selectedRecordModel; }
            set { SetProperty(ref _selectedRecordModel, value); }
        }

        private string _selectedYearEndPeriod;
        public string SelectedYearEndPeriod
        {
            get { return _selectedYearEndPeriod; }
            set
            {
                SetProperty(ref _selectedYearEndPeriod, value);
                ReloadPayrollYearEndRecords();
            }
        }

        public List<string> YearEndPeriods { get; set; }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    RefreshView();
                }
            }
        }

        public DelegateCommand GeneratePayrollYearEndRecordsCommand { get; private set; }

        public DelegateCommand<PayrollYearEndRecord> UpdateT4ReconciliationCommand { get; private set; }
        public DelegateCommand<PayrollYearEndRecord> UpdateT4FormReadyCommand { get; private set; }
        public DelegateCommand<PayrollYearEndRecord> UpdateT4FilingStatusCommand { get; private set; }
        public DelegateCommand<PayrollYearEndRecord> UpdateT4AReconciliationCommand { get; private set; }
        public DelegateCommand<PayrollYearEndRecord> UpdateT4AFilingStatusCommand { get; private set; }
        public DelegateCommand<PayrollYearEndRecord> UpdateT5ReconciliationCommand { get; private set; }
        public DelegateCommand<PayrollYearEndRecord> UpdateT5FilingStatusCommand { get; private set; }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        #endregion

        #region T4, T4A and T5 Progress Filters
        private T4Progress _t4ProgressFilter;
        public T4Progress T4ProgressFilter
        {
            get { return _t4ProgressFilter; }
            set
            {
                if (SetProperty(ref _t4ProgressFilter, value))
                {
                    RefreshView();
                }
            }
        }
        #endregion

        private const int NumberOfNextYearsInsertedToYearEndPeriod = 2;

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;
        private readonly IPayrollService _payrollService;
        private readonly IPayrollProvider _payrollProvider;

        public PayrollYearEndOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
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

        private void Initialize()
        {
            GeneratePayrollYearEndRecordsCommand = new DelegateCommand(GeneratePayrollYearEndRecords);

            UpdateT4ReconciliationCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT4Reconciliation);
            UpdateT4FormReadyCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT4FormReady);
            UpdateT4FilingStatusCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT4FilingStatus);
            UpdateT4AReconciliationCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT4AReconciliation);
            UpdateT4AFilingStatusCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT4AFilingStatus);
            UpdateT5ReconciliationCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT5Reconciliation);
            UpdateT5FilingStatusCommand = new DelegateCommand<PayrollYearEndRecord>(UpdateT5FilingStatus);

            RefreshPageCommand = new DelegateCommand(ReloadPayrollYearEndPeriods);
            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);

            _eventAggregator.GetEvent<Events.BusinessDeletedEvent>().Subscribe(HandleBusinessDeletedEvent);
            _eventAggregator.GetEvent<Events.BusinessUpsertedEvent>().Subscribe(HandleBusinessUpdatedEvent);

            ReloadPayrollYearEndPeriods();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is PayrollYearEndRecordModel model))
                {
                    e.Accepted = true;
                    return;
                }

                if (T4ProgressFilter != T4Progress.All)
                {
                    if (model.T4BlockVisibility != System.Windows.Visibility.Visible)
                    {
                        e.Accepted = false;
                        return;
                    }

                    if (T4ProgressFilter == T4Progress.Step1Pending && model.PayrollYearEndRecord.T4Reconciliation == false
                        || T4ProgressFilter == T4Progress.Step2Pending && model.PayrollYearEndRecord.T4FormReady == false
                        || T4ProgressFilter == T4Progress.Step3Pending && model.PayrollYearEndRecord.T4Status == PayrollStatus.None)
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(BusinessFilterText) == false)
                {
                    if (StringContainsFilterText(model.Business.LegalName, BusinessFilterText)
                        || StringContainsFilterText(model.Business.OperatingName, BusinessFilterText))
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                    }
                }
            };

            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending)
            });
        }

        private void ReloadPayrollYearEndRecords()
        {
            if (string.IsNullOrWhiteSpace(SelectedYearEndPeriod))
            {
                return;
            }

            var payrollYearEndRecords = _payrollService.GetPayrollYearEndRecordsByPeriod(SelectedYearEndPeriod);
            if (payrollYearEndRecords.Count > 0)
            {
                PayrollYearEndRecordModels = new ObservableCollection<PayrollYearEndRecordModel>(
                    payrollYearEndRecords.Select(x => new PayrollYearEndRecordModel(x)));
            }
            else
            {
                PayrollYearEndRecordModels = new ObservableCollection<PayrollYearEndRecordModel>();
            }

            CollectionViewSource.Source = PayrollYearEndRecordModels;

            RaisePropertyChanged(nameof(PayrollYearEndRecordsView));
            RaisePropertyChanged(nameof(FilteredItemCount));
        }

        private void ReloadPayrollYearEndPeriods()
        {
            var previousSelected = SelectedYearEndPeriod;

            YearEndPeriods = _payrollService.GetPayrollYearEndPeriods();
            InsertNextYearEndPeriods(YearEndPeriods);

            RaisePropertyChanged(nameof(YearEndPeriods));

            if (string.IsNullOrEmpty(previousSelected) == false && YearEndPeriods.Contains(previousSelected))
            {
                SelectedYearEndPeriod = previousSelected;
            }
            else
            {
                var currentYear = DateTime.Now.Year.ToString();

                if (YearEndPeriods.Contains(currentYear))
                {
                    SelectedYearEndPeriod = currentYear;
                }
                else
                {
                    SelectedYearEndPeriod = YearEndPeriods.First();
                }
            }
        }

        private void InsertNextYearEndPeriods(List<string> yearEndPeriods)
        {
            for (int i = 0; i <= NumberOfNextYearsInsertedToYearEndPeriod; i++)
            {
                var nextYear = (DateTime.Now.Year + i).ToString();
                if (yearEndPeriods.Contains(nextYear) == false)
                {
                    yearEndPeriods.Insert(0, nextYear);
                }
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

        private bool StringContainsFilterText(string input, string filterText)
        {
            return string.IsNullOrWhiteSpace(input) == false && input.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }


        private void GeneratePayrollYearEndRecords()
        {
            if (string.IsNullOrWhiteSpace(SelectedYearEndPeriod) == false)
            {
                try
                {
                    _payrollProvider.GeneratePayrollYearEndRecords(SelectedYearEndPeriod);

                    _dialogService.ShowInformation("Year End Payroll records generated",
                        $"Year End Payroll records for {SelectedYearEndPeriod} have been gerenated!");

                    ReloadPayrollYearEndPeriods();
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError(ex, "Year End Payroll records generation failed", ex.Message);
                }
            }
            else
            {
                _dialogService.ShowInformation("Year End Period not selected", 
                    "Unable to generate Year End Payroll records because no Year End Period is selected.");
            }
        }

        private void UpdateT4Reconciliation(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T4Reconciliation == false)
                {
                    if (_dialogService.ShowConfirmation("Confirm T4 Reconciliation", "Do you want to confirm T4 reconciliation?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4Reconciliation(yearEndRecord.Id, true, updatedBy);

                        yearEndRecord.T4Reconciliation = true;
                        yearEndRecord.T4UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else
                {
                    if (_dialogService.ShowConfirmation("Undo T4 Reconciliation", "Are you sure to undo T4 reconciliation?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4Reconciliation(yearEndRecord.Id, false, updatedBy);

                        yearEndRecord.T4Reconciliation = false;
                        yearEndRecord.T4UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR", $"Unexpected error executing {nameof(UpdateT4Reconciliation)}: {ex.Message}");
            }
        }

        private void UpdateT4FormReady(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T4FormReady == false)
                {
                    if (_dialogService.ShowConfirmation("Confirm T4 Ready", "Do you want to confirm T4 Ready?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4FormReady(yearEndRecord.Id, true, updatedBy);

                        yearEndRecord.T4FormReady = true;
                        yearEndRecord.T4UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else
                {
                    if (_dialogService.ShowConfirmation("Undo T4 Ready", "Are you sure to undo T4 Ready?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4FormReady(yearEndRecord.Id, false, updatedBy);

                        yearEndRecord.T4FormReady = false;
                        yearEndRecord.T4UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR", $"Unexpected error executing {nameof(UpdateT4FormReady)}: {ex.Message}");
            }
        }

        private void UpdateT4FilingStatus(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T4Status == PayrollStatus.None)
                {
                    if (_dialogService.ShowConfirmation("Confirm T4 Filing", "Do you want to confirm T4 Filing?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4FilingStatus(yearEndRecord.Id, PayrollStatus.Done,
                        yearEndRecord.T4Confirmation, updatedBy);

                        yearEndRecord.T4Status = PayrollStatus.Done;
                        yearEndRecord.T4UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else if (yearEndRecord.T4Status == PayrollStatus.Done)
                {
                    if (_dialogService.ShowConfirmation("Undo T4 Filing", "Are you sure to undo T4 Filing?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4FilingStatus(yearEndRecord.Id, PayrollStatus.None,
                        yearEndRecord.T4Confirmation, updatedBy);

                        yearEndRecord.T4Status = PayrollStatus.None;
                        yearEndRecord.T4UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR", $"Unexpected error executing {nameof(UpdateT4FilingStatus)}. {ex.Message}");
            }
        }


        // TODO: Duplicated code
        private void UpdateT4AReconciliation(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T4AReconciliation == false)
                {
                    if (_dialogService.ShowConfirmation("Confirm T4A Reconciliation", "Do you want to confirm T4A reconciliation?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4AReconciliation(yearEndRecord.Id, true, updatedBy);

                        yearEndRecord.T4AReconciliation = true;
                        yearEndRecord.T4AUpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else
                {
                    if (_dialogService.ShowConfirmation("Undo T4A Reconciliation", "Are you sure to undo T4A reconciliation?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4AReconciliation(yearEndRecord.Id, false, updatedBy);

                        yearEndRecord.T4AReconciliation = false;
                        yearEndRecord.T4AUpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, $"Error Executing {nameof(UpdateT4AReconciliation)}", $"Unexpected error: {ex.Message}");
            }
        }

        private void UpdateT4AFilingStatus(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T4AStatus == PayrollStatus.None)
                {
                    if (_dialogService.ShowConfirmation("Confirm T4A Filing", "Do you want to confirm T4A Filing?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4AFilingStatus(yearEndRecord.Id, PayrollStatus.Done,
                        yearEndRecord.T4AConfirmation, updatedBy);

                        yearEndRecord.T4AStatus = PayrollStatus.Done;
                        yearEndRecord.T4AUpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else if (yearEndRecord.T4AStatus == PayrollStatus.Done)
                {
                    if (_dialogService.ShowConfirmation("Undo T4A Filing", "Are you sure to undo T4A Filing?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT4AFilingStatus(yearEndRecord.Id, PayrollStatus.None,
                        yearEndRecord.T4AConfirmation, updatedBy);

                        yearEndRecord.T4AStatus = PayrollStatus.None;
                        yearEndRecord.T4AUpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, $"Error executing {nameof(UpdateT4AFilingStatus)}.", $"Unexpected error: {ex.Message}");
            }
        }

        // TODO: Duplicated code, I know...
        private void UpdateT5Reconciliation(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T5Reconciliation == false)
                {
                    if (_dialogService.ShowConfirmation("Confirm T5 Reconciliation", "Do you want to confirm T5 reconciliation?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT5Reconciliation(yearEndRecord.Id, true, updatedBy);

                        yearEndRecord.T5Reconciliation = true;
                        yearEndRecord.T5UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else
                {
                    if (_dialogService.ShowConfirmation("Undo T5 Reconciliation", "Are you sure to undo T5 reconciliation?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT5Reconciliation(yearEndRecord.Id, false, updatedBy);

                        yearEndRecord.T5Reconciliation = false;
                        yearEndRecord.T5UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, $"Error executing {nameof(UpdateT5Reconciliation)}", $"Unexpected error: {ex.Message}");
            }
        }

        private void UpdateT5FilingStatus(PayrollYearEndRecord yearEndRecord)
        {
            var updatedBy = GenerateUpdatedByText();

            try
            {
                if (yearEndRecord.T5Status == PayrollStatus.None)
                {
                    if (_dialogService.ShowConfirmation("Confirm T5 Filing", "Do you want to confirm T5 Filing?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT5FilingStatus(yearEndRecord.Id, PayrollStatus.Done,
                        yearEndRecord.T5Confirmation, updatedBy);

                        yearEndRecord.T5Status = PayrollStatus.Done;
                        yearEndRecord.T5UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
                else if (yearEndRecord.T5Status == PayrollStatus.Done)
                {
                    if (_dialogService.ShowConfirmation("Undo T5 Filing", "Are you sure to undo T5 Filing?"))
                    {
                        _payrollProvider.UpdateYearEndRecordT5FilingStatus(yearEndRecord.Id, PayrollStatus.None,
                        yearEndRecord.T5Confirmation, updatedBy);

                        yearEndRecord.T5Status = PayrollStatus.None;
                        yearEndRecord.T5UpdatedBy = updatedBy;

                        RefreshView();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, $"Error executing {nameof(UpdateT5FilingStatus)}", $"Unexpected error: {ex.Message}");
            }
        }

        private string GenerateUpdatedByText()
        {
            var username = _globalService.CurrentSession.UserDisplayName;

            return $"[{username}] on {DateTime.Now.ToString(Constant.DateTimeFormat)}";
        }


        // TODO: Better handle Business or Payroll Account updated
        private void HandleBusinessDeletedEvent(Guid businessId)
        {
            ReloadPayrollYearEndRecords();
        }

        // TODO: Better handle Business or Payroll Account updated
        private void HandleBusinessUpdatedEvent(Guid businessId)
        {
            ReloadPayrollYearEndRecords();
        }

        private void RefreshView()
        {
            PayrollYearEndRecordsView.Refresh();
            RaisePropertyChanged(nameof(FilteredItemCount));
        }
    }

    public enum T4Progress
    {
        All = 0,
        Step1Pending = 1,
        Step2Pending = 2,
        Step3Pending = 3,
    }
}
