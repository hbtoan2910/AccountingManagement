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
    public class ClientPaymentOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView ClientPaymentsView => CollectionViewSource.View;

        private ObservableCollection<BusinessClientPaymentModel> _clientPaymentModels;
        public ObservableCollection<BusinessClientPaymentModel> ClientPaymentModels
        {
            get { return _clientPaymentModels; }
            set { SetProperty(ref _clientPaymentModels, value); }
        }

        private BusinessClientPaymentModel _selectedClientPaymentModel;
        public BusinessClientPaymentModel SelectedClientPaymentModel
        {
            get { return _selectedClientPaymentModel; }
            set { SetProperty(ref _selectedClientPaymentModel, value); }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    ClientPaymentsView.Refresh();
                }
            }
        }

        private string _dueDateHeaderText = "Due Date";
        public string DueDateHeaderText
        {
            get { return _dueDateHeaderText; }
            set { SetProperty(ref _dueDateHeaderText, value); }
        }

        public List<ClientPaymentType> ClientPaymentTypes
        {
            get
            {
                return new List<ClientPaymentType>
                {
                    ClientPaymentType.Regular,
                    ClientPaymentType.Undefined,
                    ClientPaymentType.Secondary,
                };
            }
        }

        private ClientPaymentType _selectedPaymentType = ClientPaymentType.Regular;
        public ClientPaymentType SelectedPaymentType
        {
            get { return _selectedPaymentType; }
            set
            {
                if (SetProperty(ref _selectedPaymentType, value))
                {
                    LoadClientPayments();

                    RaisePropertyChanged("ClientPaymentsView");
                }
            }
        }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand SortByDueDateCommand { get; private set; }

        public DelegateCommand<BusinessClientPaymentModel> SaveConfirmationTextCommand { get; private set; }
        public DelegateCommand<BusinessClientPaymentModel> SendReceiptEmailCommand { get; private set; }
        public DelegateCommand<BusinessClientPaymentModel> ConfirmPaymentCommand { get; private set; }
        public DelegateCommand<ClientPayment> OpenClientPaymentDetailsDialogCommand { get; private set; }

        public DelegateCommand ExportToFileCommand { get; private set; }

        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        public DelegateCommand NavigateToClientPaymentHistoryCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;
        private readonly IPaymentHandler _paymentHandler;

        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly IClientPaymentService _clientPaymentService;
        private readonly IEmailService _emailService;

        public ClientPaymentOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator,
            IRegionManager regionManager, IGlobalService globalService, IBusinessOwnerService businessOwnerService,
            IClientPaymentService clientPaymentService, IEmailService emailService, IPaymentHandler paymentHandler)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));

            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _clientPaymentService = clientPaymentService ?? throw new ArgumentNullException(nameof(clientPaymentService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _paymentHandler = paymentHandler ?? throw new ArgumentNullException(nameof(paymentHandler));

            Initialize();
        }

        private void Initialize()
        {
            RefreshPageCommand = new DelegateCommand(RefreshPage);
            SortByDueDateCommand = new DelegateCommand(SortByDueDate);

            SaveConfirmationTextCommand = new DelegateCommand<BusinessClientPaymentModel>(SaveConfirmationText);
            SendReceiptEmailCommand = new DelegateCommand<BusinessClientPaymentModel>(SendReceiptEmail);
            ConfirmPaymentCommand = new DelegateCommand<BusinessClientPaymentModel>(ConfirmPayment);
            OpenClientPaymentDetailsDialogCommand = new DelegateCommand<ClientPayment>(OpenClientPaymentDetailsDialog);

            ExportToFileCommand = new DelegateCommand(ExportToFile);

            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);
            NavigateToClientPaymentHistoryCommand = new DelegateCommand(NavigateToClientPaymentHistory);

            LoadClientPayments();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is BusinessClientPaymentModel model) || string.IsNullOrWhiteSpace(BusinessFilterText))
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

        private void LoadClientPayments()
        {
            var clientPayments = _clientPaymentService.GetClientPayments();

            if (clientPayments.Count > 0)
            {
                ClientPaymentModels = new ObservableCollection<BusinessClientPaymentModel>(
                    clientPayments.Select(x => new BusinessClientPaymentModel(x, _paymentHandler)));
            }
            else
            {
                ClientPaymentModels = new ObservableCollection<BusinessClientPaymentModel>();
            }

            CollectionViewSource.Source = ClientPaymentModels;
        }


        private void SaveConfirmationText(BusinessClientPaymentModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ConfirmText))
            {
                _dialogService.ShowInformation("Invalid action", "Confirmation Text must not be blank.");
                return;
            }

            try
            {
                _paymentHandler.SaveConfirmationText(model.ClientPayment, model.ConfirmText);

                model.ClientPayment.TmpConfirmationText = model.ConfirmText;

                ClientPaymentsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to save confirmation text. {ex.Message}");
            }
        }

        private void SendReceiptEmail(BusinessClientPaymentModel model)
        {
            var clientPayment = model.ClientPayment;

            if (VerifyBusinessEmailInfos(clientPayment, out Business business) == false)
            {
                return;
            }

            try
            {
                _emailService.SendPaymentReceiptEmail(business, clientPayment);

                clientPayment.TmpReceiptEmailSent = true;

                _paymentHandler.SaveEmailSentStatus(clientPayment);

                ClientPaymentsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR Sending Receipt Email", ex.Message);
                Log.Error(ex, "ERROR sending receipt email");
            }
        }

        private void ConfirmPayment(BusinessClientPaymentModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            if (model.ClientPayment.TmpReceiptEmailSent == false)
            {
                _dialogService.ShowInformation("Invalid action", "Client Payment Receipt email is not sent yet");
                return;
            }

            try
            {
                _paymentHandler.ConfirmClientPayment(model.ClientPayment, model.ConfirmText, confirmDate, currentUserId);

                model.ConfirmText = string.Empty;

                var oldRecord = ClientPaymentModels.FirstOrDefault(x => x.ClientPayment.Id == model.ClientPayment.Id);
                var updated = _clientPaymentService.GetClientPaymentById(model.ClientPayment.Id);

                if (oldRecord != null && updated != null)
                {
                    if (updated.IsActive)
                    {
                        oldRecord.ClientPayment = updated;

                        ClientPaymentsView.Refresh();
                    }
                    else
                    {
                        ClientPaymentModels.Remove(oldRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to confirm Client Payment. {ex.Message}");
                Log.Error(ex, "Failed to confirm Client Payment");
            }
        }

        private void ExportToFile()
        {
            var selectedRecords = ClientPaymentModels.Where(x => x.IsSelectedForExport).ToList();

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
                    Payment = x.ClientPayment.PaymentAmount.ToString("c"),
                    DueDate = x.ClientPayment.DueDate.ToString(Constant.DateFormat),
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

            try
            {
                foreach (var record in selectedRecords)
                {
                    ConfirmPayment(record);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error Confirm Payment", $"Unable to move one or more account to next period. {ex.Message}");
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

        private void NavigateToClientPaymentHistory()
        {
            var navParams = new NavigationParameters($"");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.ClientPaymentHistory, navParams);
        }

        private void OpenClientPaymentDetailsDialog(ClientPayment clientPayment)
        {
            if (clientPayment == null)
            {
                return;
            }

            var parameters = new DialogParameters($"ClientPaymentId={clientPayment.Id}");

            /*
            _dialogService.ShowDialog(nameof(Views.TaxAccountDetails), parameters, d =>
            {
                if (d.Result == ButtonResult.OK)
                {
                    var updatedRecord = _userAccountProvider.GetTaxAccountById(taxAccount.Id);

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
            */
        }

        private void RefreshPage()
        {
            LoadClientPayments();

            // TODO: Can I skip this step?
            RaisePropertyChanged("ClientPaymentsView");

            // TODO: Or this step?
            ClientPaymentsView.Refresh();
        }

        private void SortByDueDate()
        {
            DueDateHeaderText = "Due Date *";

            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("ClientPayment.DueDate", ListSortDirection.Ascending),
                new SortDescription("Business.OperatingName", ListSortDirection.Ascending),
            });
        }

        private bool StringContainsFilterText(string input, string filterText)
        {
            return string.IsNullOrWhiteSpace(input) == false && input.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool VerifyBusinessEmailInfos(ClientPayment clientPayment, out Business business)
        {
            business = _businessOwnerService.GetBusinessById(clientPayment.BusinessId);

            if (business == null)
            {
                _dialogService.ShowError(new Core.Exceptions.BusinessNotFoundException(clientPayment.BusinessId),
                    "Business Not Found", $"Business account associated with ClientPaymentId:{clientPayment.Id} not found");

                return false;
            }

            if (string.IsNullOrWhiteSpace(business.Email) || string.IsNullOrWhiteSpace(business.EmailContact))
            {
                _dialogService.ShowInformation("Email Infos Not Setup Yet", "Please setup Business Email and EmailContact first and try again.");

                return false;
            }

            return true;
        }
    }
}
