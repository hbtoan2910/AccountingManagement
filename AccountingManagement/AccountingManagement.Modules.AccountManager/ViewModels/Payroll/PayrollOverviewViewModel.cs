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
using AccountingManagement.Services.Email;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class PayrollOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView BusinessPayrollRecordsView => CollectionViewSource.View;

        private ObservableCollection<BusinessPayrollModel> _businessPayrollModels;
        public ObservableCollection<BusinessPayrollModel> BusinessPayrollModels
        {
            get { return _businessPayrollModels; }
            set { SetProperty(ref _businessPayrollModels, value); }
        }

        public int FilteredItemCount
        {
            get { return BusinessPayrollRecordsView.Cast<object>()?.Count() ?? 0; }
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
                    ReloadPayrollRecords();

                    RaisePropertyChanged("BusinessPayrollRecordsView");
                }
            }
        }

        private string _payrollOverviewFilterText;
        public string PayrollOverviewFilterText
        {
            get { return _payrollOverviewFilterText; }
            set 
            {
                if (SetProperty(ref _payrollOverviewFilterText, value))
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

        #region Payroll and PD7A Cycle Selection
        public List<FilingCycle> PayrollCycles
        {
            get
            {
                return new List<FilingCycle>()
                {
                    // FilingCycle.Weekly,
                    FilingCycle.BiWeekly,
                    FilingCycle.SemiMonthly,
                    FilingCycle.Monthly,
                    // FilingCycle.None,
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

        public List<FilingCycle> PD7ACycles
        {
            get
            {
                return new List<FilingCycle>
                {
                    FilingCycle.BiMonthly,
                    FilingCycle.Monthly,
                    FilingCycle.Quarterly
                };
            }
        }

        private FilingCycle? _selectedPD7ACycle;
        public FilingCycle? SelectedPD7ACycle
        {
            get { return _selectedPD7ACycle; }
            set
            { 
                if (SetProperty(ref _selectedPD7ACycle, value))
                {
                    RefreshView();
                }
            }
        }
        #endregion

        private PayrollProgress _payrollProgressFilter;
        public PayrollProgress PayrollProgressFilter
        {
            get { return _payrollProgressFilter; }
            set
            {
                if (SetProperty(ref _payrollProgressFilter, value))
                {
                    RefreshView();
                }
            }
        }
        //RYAN: add new filter for field Timesheet in Payroll table
        private bool _isTimesheetFilter = false;
        public bool IsTimesheetFilter
        {
            get { return _isTimesheetFilter; }
            set
            {
                if (SetProperty(ref _isTimesheetFilter, value))
                {
                    RefreshView();
                }
            }
        }
        //RYAN: add new filter for PD7AStatus=2 (Done)
        /*
        private bool _isPD7ADoneFilter = false;
        public bool IsPD7ADoneFilter
        {
            get { return _isPD7ADoneFilter; }
            set
            {
                if (SetProperty(ref _isPD7ADoneFilter, value))
                {
                    RefreshView();
                }
            }
        }
        */

        private PayrollStatus _pd7aStatusFilter = PayrollStatus.All;
        public PayrollStatus PD7AStatusFilter
        {
            get { return _pd7aStatusFilter; }
            set
            {
                if (SetProperty(ref _pd7aStatusFilter, value))
                {
                    RefreshView();
                }
            }
        }

        public DelegateCommand RefreshPayrollOverviewCommand { get; private set; }
        public DelegateCommand OpenGeneratePayrollDialogCommand { get; private set; }

        public DelegateCommand<PayrollAccountRecord> PayrollRecordEditCommand { get; private set; }
        public DelegateCommand<PayrollAccountRecord> UpdatePayroll1StatusCommand { get; private set; }
        public DelegateCommand<PayrollAccountRecord> UpdatePayroll2StatusCommand { get; private set; }
        public DelegateCommand<PayrollAccountRecord> UpdatePayroll3StatusCommand { get; private set; }
        public DelegateCommand<PayrollAccountRecord> UpdatePD7APrintedCommand { get; private set; }
        public DelegateCommand<PayrollAccountRecord> UpdatePD7AStatusCommand { get; private set; }

        public DelegateCommand<PayrollAccountRecord> SendTimesheetRequestEmailCommand { get; private set; }
        public DelegateCommand<PayrollAccountRecord> SendPD7AReminderEmailCommand { get; private set; }

        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        private readonly IGlobalService _globalService;
        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly IEmailService _emailService;
        private readonly IPayrollService _payrollService;
        private readonly IPayrollProvider _payrollProvider;

        public PayrollOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService, IBusinessOwnerService businessOwnerService, IEmailService emailService,
            IPayrollService payrollService, IPayrollProvider payrollProvider)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            _globalService = globalService;
            _businessOwnerService = businessOwnerService;
            _emailService = emailService;
            _payrollService = payrollService;
            _payrollProvider = payrollProvider;

            Initialize();
        }

        private void ReloadPayrollRecords()
        {
            if (SelectedPayrollPeriod == null)
            {
                return;
            }

            var payrollRecords = _payrollService.GetBusinessPayrollRecordsForPeriod(SelectedPayrollPeriod.PayrollPeriod);

            if (payrollRecords.Count > 0)
            {
                BusinessPayrollModels = new ObservableCollection<BusinessPayrollModel>(
                    payrollRecords
                        .Where(x => x.PayrollAccount.PayrollAccountRecords.Count > 0)
                        .Select(x => new BusinessPayrollModel(x, SelectedPayrollPeriod.PayrollPeriod)));
            }
            else
            {
                BusinessPayrollModels = new ObservableCollection<BusinessPayrollModel>();
            }

            CollectionViewSource.Source = BusinessPayrollModels;
        }

        private void RefreshView()
        {
            BusinessPayrollRecordsView.Refresh();
            RaisePropertyChanged("FilteredItemCount");
        }

        private void Initialize()
        {
            RefreshPayrollOverviewCommand = new DelegateCommand(RefreshPayrollOverview);
            OpenGeneratePayrollDialogCommand = new DelegateCommand(OpenGeneratePayrollDialog);

            PayrollRecordEditCommand = new DelegateCommand<PayrollAccountRecord>(OpenPayrollRecordEdit);
            UpdatePayroll1StatusCommand = new DelegateCommand<PayrollAccountRecord>(UpdatePayroll1Status);
            UpdatePayroll2StatusCommand = new DelegateCommand<PayrollAccountRecord>(UpdatePayroll2Status);
            UpdatePayroll3StatusCommand = new DelegateCommand<PayrollAccountRecord>(UpdatePayroll3Status);
            UpdatePD7APrintedCommand = new DelegateCommand<PayrollAccountRecord>(UpdatePD7APrinted);
            UpdatePD7AStatusCommand = new DelegateCommand<PayrollAccountRecord>(UpdatePD7AStatus);

            SendTimesheetRequestEmailCommand = new DelegateCommand<PayrollAccountRecord>(SendTimesheetRequestEmail);
            SendPD7AReminderEmailCommand = new DelegateCommand<PayrollAccountRecord>(SendPD7AReminderEmail);

            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);

            ExistingPayrollPeriods = _payrollService.GetLatestPayrollPeriodLookups(12);
            SelectedPayrollPeriod = ExistingPayrollPeriods.FirstOrDefault();

            //RYAN: no data population here, why CollectionVieWSource suddenly has data ???
            CollectionViewSource.Filter += (s, e) =>
            {
                var model = e.Item as BusinessPayrollModel;

                if (SelectedPayrollCycle != null)
                {
                    if (model.PayrollAccount.PayrollCycle != SelectedPayrollCycle.Value)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (SelectedPD7ACycle != null)
                {
                    if (model.PayrollAccount.PD7ACycle != SelectedPD7ACycle.Value)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(PayrollOverviewFilterText) == false)
                {
                    if (model.Business.LegalName.Contains(PayrollOverviewFilterText, StringComparison.InvariantCultureIgnoreCase) == false
                        && model.Business.OperatingName.Contains(PayrollOverviewFilterText, StringComparison.InvariantCultureIgnoreCase) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (PayrollProgressFilter != PayrollProgress.All)
                {
                    if (PayrollProgressFilter == PayrollProgress.Payroll1Pending && (model.PayrollRecord.Payroll1DueDate == null || model.PayrollRecord.Payroll1Status == PayrollStatus.Done)
                        || PayrollProgressFilter == PayrollProgress.Payroll2Pending && (model.PayrollRecord.Payroll2DueDate == null || model.PayrollRecord.Payroll2Status == PayrollStatus.Done)
                        || PayrollProgressFilter == PayrollProgress.Payroll3Pending && (model.PayrollRecord.Payroll3DueDate == null || model.PayrollRecord.Payroll3Status == PayrollStatus.Done))
                    {
                        e.Accepted = false;
                        return;
                    }
                }
                //RYAN: new filter added here
                if (IsTimesheetFilter)
                {
                    if (!model.PayrollAccount.Timesheet)
                    {
                        e.Accepted = false;
                        return;
                    }
                }
                //RYAN: new filter for PD7AStatus <> PayrollStatus
                if (PD7AStatusFilter != PayrollStatus.All)
                {
                    if (PD7AStatusFilter == PayrollStatus.None && (model.PayrollRecord.PD7AStatus != PayrollStatus.None)
                        || PD7AStatusFilter == PayrollStatus.InProgress && (model.PayrollRecord.PD7AStatus != PayrollStatus.InProgress)
                        || PD7AStatusFilter == PayrollStatus.Done && (model.PayrollRecord.PD7AStatus != PayrollStatus.Done))
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

            //CollectionViewSource.GroupDescriptions.Add(
            //    new PropertyGroupDescription("PayrollAccount.PayrollCycle"));
        }

        private bool IsPayrollRecordHasPendingWork(PayrollAccountRecord record)
        {
            if ((record.Payroll1DueDate != null && record.Payroll1Status == PayrollStatus.None)
                || (record.Payroll2DueDate != null && record.Payroll2Status == PayrollStatus.None)
                || (record.Payroll3DueDate != null && record.Payroll3Status == PayrollStatus.None)
                || (record.PD7ADueDate != null && record.PD7AStatus == PayrollStatus.None))
            {
                return true;
            }

            return false;
        }

        private bool IsPayrollRecordAllDone(PayrollAccountRecord record)
        {
            if ((record.Payroll1DueDate != null && record.Payroll1Status != PayrollStatus.Done)
                || (record.Payroll2DueDate != null && record.Payroll2Status != PayrollStatus.Done)
                || (record.Payroll3DueDate != null && record.Payroll3Status != PayrollStatus.Done)
                || (record.PD7ADueDate != null && record.PD7AStatus != PayrollStatus.Done))
            {
                return false;
            }

            return true;
        }

        private void RefreshPayrollOverview()
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
                    RefreshPayrollOverview();
                }
            });
        }

        private void OpenPayrollRecordEdit(PayrollAccountRecord payrollRecord)
        {
            if (payrollRecord == null)
            {
                return;
            }

            var parameters = new DialogParameters($"PayrollRecordId={payrollRecord.Id}");

            _dialogService.ShowDialog(nameof(Views.PayrollEdit), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    var updatedRecord = _payrollService.GetPayrollRecordById(payrollRecord.Id);

                    var oldRecord = BusinessPayrollModels.FirstOrDefault(x => x.PayrollRecord.Id == payrollRecord.Id);
                    if (oldRecord != null)
                    {
                        oldRecord.PayrollRecord = updatedRecord;

                        BusinessPayrollRecordsView.Refresh();
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

        private void UpdatePayroll1Status(PayrollAccountRecord payrollRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payrollRecord.Payroll1Status.ToString());

            payrollRecord.Payroll1UpdatedBy = updatedByText;

            _payrollProvider.UpdatePayrollRecordDueDate1Status(payrollRecord.Id, payrollRecord.Payroll1Status, updatedByText);

            BusinessPayrollRecordsView.Refresh();
        }

        private void UpdatePayroll2Status(PayrollAccountRecord payrollRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payrollRecord.Payroll2Status.ToString());

            payrollRecord.Payroll2UpdatedBy = updatedByText;

            _payrollProvider.UpdatePayrollRecordDueDate2Status(payrollRecord.Id, payrollRecord.Payroll2Status, updatedByText);

            BusinessPayrollRecordsView.Refresh();
        }

        private void UpdatePayroll3Status(PayrollAccountRecord payrollRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payrollRecord.Payroll3Status.ToString());

            payrollRecord.Payroll3UpdatedBy = updatedByText;

            _payrollProvider.UpdatePayrollRecordDueDate3Status(payrollRecord.Id, payrollRecord.Payroll3Status, updatedByText);

            BusinessPayrollRecordsView.Refresh();
        }

        private void UpdatePD7APrinted(PayrollAccountRecord payrollRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payrollRecord.PD7APrinted.ToString());

            payrollRecord.PD7AUpdatedBy = updatedByText;

            _payrollProvider.UpdatePayrollRecordPD7APrinted(payrollRecord.Id, payrollRecord.PD7APrinted, updatedByText);

            BusinessPayrollRecordsView.Refresh();
        }

        private void UpdatePD7AStatus(PayrollAccountRecord payrollRecord)
        {
            var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, payrollRecord.PD7AStatus.ToString());

            payrollRecord.PD7AUpdatedBy = updatedByText;

            _payrollProvider.UpdatePayrollRecordPD7AStatus(payrollRecord.Id, payrollRecord.PD7AStatus, payrollRecord.PD7AConfirmation, updatedByText);

            BusinessPayrollRecordsView.Refresh();
        }


        private void SendPD7AReminderEmail(PayrollAccountRecord payrollRecord)
        {
            if (VerifyBusinessEmailInfos(payrollRecord, out Business business) == false)
            {
                return;
            }

            if (payrollRecord.PD7AReminder == EmailReminder.Sent)
            {
                if (_dialogService.ShowConfirmation("Reminder Email sent", "A reminder Email was already sent. Do you want to re-send?") == false)
                {
                    return;
                }
            }

            try
            {
                _emailService.SendPayrollReminderEmail(business, payrollRecord);

                var updatedByText = GenerateLastUpdatedText(_globalService.CurrentSession.UserDisplayName, EmailReminder.Sent.ToString());

                _payrollProvider.UpdatePayrollRecordPD7AReminder(payrollRecord.Id, EmailReminder.Sent, payrollRecord.PD7AConfirmation, updatedByText);

                // Update ViewModel record // Find a better way to do this?s
                payrollRecord.PD7AReminder = EmailReminder.Sent;
                payrollRecord.PD7AUpdatedBy = updatedByText;

                _dialogService.ShowInformation("Email Sent", "Payroll reminder has been sent!");

                BusinessPayrollRecordsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR sending Payroll reminder email", ex.Message);
                Log.Error(ex, "ERROR sending email");
            }
        }

        private void SendTimesheetRequestEmail(PayrollAccountRecord payrollRecord)
        {
            if (VerifyBusinessEmailInfos(payrollRecord, out Business business) == false)
            {
                return;
            }

            var messageParameters = new Dictionary<string, string>
            {
                { "{LegalName}", business.LegalName },
                { "{OperatingName}", business.OperatingName },
                { "{EmailContact}", business.EmailContact },
            };

            try
            {
                _emailService.SendTimesheetRequestEmail(messageParameters, business.Email);

                _dialogService.ShowInformation("Email Sent", "Timesheet request has been sent!");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR sending Timesheet request", ex.Message);
            }
        }

        private bool VerifyBusinessEmailInfos(PayrollAccountRecord payrollRecord, out Business business)
        {
            business = _businessOwnerService.GetBusinessById(payrollRecord.PayrollAccount.BusinessId);
            if (business == null)
            {
                _dialogService.ShowError(new Core.Exceptions.BusinessNotFoundException(payrollRecord.PayrollAccount.BusinessId),
                    "Business Not Found", $"Business account for Payroll Number [{payrollRecord.PayrollAccount.PayrollNumber}] not found");

                return false;
            }

            if (string.IsNullOrWhiteSpace(business.Email) || string.IsNullOrWhiteSpace(business.EmailContact))
            {
                _dialogService.ShowInformation("Email Infos Not Setup Yet", "Please setup Business Email and EmailContact first and try again.");

                return false;
            }

            return true;
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

    public enum PayrollProgress
    {
        All = 0,
        Payroll1Pending = 1,
        Payroll2Pending = 2,
        Payroll3Pending = 3
    }

}

