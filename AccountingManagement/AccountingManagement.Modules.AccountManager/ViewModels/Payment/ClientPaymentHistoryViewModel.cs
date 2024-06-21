using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Prism.Commands;
using Prism.Regions;
using AccountingManagement.Core;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Modules.AccountManager.Models;
using AccountingManagement.Modules.AccountManager.Helpers;
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;
using Prism.Services.Dialogs;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class ClientPaymentHistoryViewModel : ViewModelBase
    {
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView ClientPaymentLogsView => CollectionViewSource.View;

        private ObservableCollection<ClientPaymentLogModel> _clientPaymentLogModels;
        public ObservableCollection<ClientPaymentLogModel> ClientPaymentLogModels
        {
            get { return _clientPaymentLogModels; }
            set { SetProperty(ref _clientPaymentLogModels, value); }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    ClientPaymentLogsView.Refresh();
                }
            }
        }

        public DelegateCommand ExportToFileCommand { get; private set; }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        public DelegateCommand NavigateToClientPaymentOverviewCommand { get; private set; }

        private readonly IDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IClientPaymentService _clientPaymentService;

        public ClientPaymentHistoryViewModel(IDialogService dialogService, IRegionManager regionManager,
            IClientPaymentService clientPaymentService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _clientPaymentService = clientPaymentService ?? throw new ArgumentNullException(nameof(clientPaymentService));

            Initialize();
        }

        private void Initialize()
        {
            ExportToFileCommand = new DelegateCommand(ExportToFile);

            RefreshPageCommand = new DelegateCommand(RefreshPage);
            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);
            NavigateToClientPaymentOverviewCommand = new DelegateCommand(NavigateToClientPaymentOverview);

            LoadClientPaymentLogs();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is ClientPaymentLogModel log))
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


        private void ExportToFile()
        {
            var selectedRecords = ClientPaymentLogModels.Where(x => x.IsSelected).ToList();

            if (selectedRecords.Any() == false)
            {
                _dialogService.ShowInformation("Export to File", "Must select at least an item to export");
                return;
            }

            var exportRecords = selectedRecords.Select(x =>
                new
                {
                    Name = x.Business.LegalName,
                    BankInfo = x.ClientPayment.BankInfo.ToString(),
                    x.ClientPayment.Notes,
                    DueDate = x.ClientPayment.DueDate.ToString(Constant.DateFormat),
                    Payment = x.ClientPayment.PaymentAmount.ToString("c"),
                    SpecialText = x.ClientPayment.TmpConfirmationText,
                })
                .ToList();

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export to File",
                    FileName = "Client Payment",
                    DefaultExt = ".csv",
                    Filter = "CSV File |*.csv",
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var writer = new System.IO.StreamWriter(saveFileDialog.FileName);
                    using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
                    csv.WriteRecords(exportRecords);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Export Failed", $"Unable to export to file. {ex.Message}");
            }
        }

        private void LoadClientPaymentLogs()
        {
            var paymentType = ClientPaymentType.Regular;
            var logs = _clientPaymentService.GetClientPaymentLogsByType(paymentType)
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            if (logs.Count > 0)
            {
                var logModels = logs.Select(x => new ClientPaymentLogModel(x));
                ClientPaymentLogModels = new ObservableCollection<ClientPaymentLogModel>(logModels);
            }
            else
            {
                ClientPaymentLogModels = new ObservableCollection<ClientPaymentLogModel>();
            }

            CollectionViewSource.Source = ClientPaymentLogModels;
        }

        private void NavigateToBusinessOverview(Business business)
        {
            if (business == null)
            {
                return;
            }

            var navParams = new NavigationParameters($"BusinessOperatingName={business.OperatingName}");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.OwnerOverview, navParams);
        }

        private void NavigateToClientPaymentOverview()
        {
            var navParams = new NavigationParameters($"");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.ClientPaymentOverview, navParams);
        }

        private void RefreshPage()
        {
            LoadClientPaymentLogs();

            // TODO: Can I skip this step?
            RaisePropertyChanged("ClientPaymentLogsView");

            // TODO: Or this step?
            ClientPaymentLogsView.Refresh();
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
