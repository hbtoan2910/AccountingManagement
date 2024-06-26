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
using System.Windows;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxAccountWithInstalmentOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView BusinessTaxAccountsView => CollectionViewSource.View;

        private ObservableCollection<BusinessTaxAccountWithInstalmentModel> _taxAccountModels;
        public ObservableCollection<BusinessTaxAccountWithInstalmentModel> TaxAccountModels
        {
            get { return _taxAccountModels; }
            set { SetProperty(ref _taxAccountModels, value); }
        }

        private BusinessTaxAccountWithInstalmentModel _selectedTaxAccountModel;
        public BusinessTaxAccountWithInstalmentModel SelectedTaxAccountModel
        {
            get { return _selectedTaxAccountModel; }
            set { SetProperty(ref _selectedTaxAccountModel, value); }
        }

        private List<UserAccount> _userAccounts;
        public List<UserAccount> UserAccounts
        {
            get { return _userAccounts; }
            set { SetProperty(ref _userAccounts, value); }
        }

        private UserAccount _selectedUserAccount = null;
        public UserAccount SelectedUserAccount
        {
            get { return _selectedUserAccount; }
            set
            {
                if (SetProperty(ref _selectedUserAccount, value))
                {
                    BusinessTaxAccountsView.Refresh();
                }
            }
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

        public List<TaxAccountType> TaxAccountTypes
        {
            get
            {
                return new List<TaxAccountType>
                {
                    TaxAccountType.HST,
                    TaxAccountType.Corporation
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
                    LoadTaxAccounts();

                    RaisePropertyChanged("BusinessTaxAccountsView");
                }
            }
        }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand SortByDueDateCommand { get; private set; }
        public DelegateCommand SortByEndingPeriodCommand { get; private set; }

        public DelegateCommand<BusinessTaxAccountWithInstalmentModel> ConfirmFilingCommand { get; private set; }
        public DelegateCommand<BusinessTaxAccountWithInstalmentModel> ConfirmInstalmentCommand { get; private set; }
        public DelegateCommand<TaxAccountWithInstalment> OpenTaxAccountDetailsDialogCommand { get; private set; }
        public DelegateCommand<TaxAccountWithInstalment> SendPaperworkReminderEmailCommand { get; private set; }
        public DelegateCommand<TaxAccountWithInstalment> SendInstalmentReminderEmailCommand { get; private set; }
        
        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        public DelegateCommand NavigateToTaxAccountHistoryCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;

        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly IEmailService _emailService;
        private readonly ITaxAccountService _taxAccountService;
        private readonly IFilingHandler _filingHandler;
        private readonly IUserAccountService _userAccountService;

        public TaxAccountWithInstalmentOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService, IBusinessOwnerService businessOwnerService, IEmailService emailService,
            ITaxAccountService taxAccountService, IFilingHandler filingHandler, IUserAccountService userAccountService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));

            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _filingHandler = filingHandler ?? throw new ArgumentNullException(nameof(filingHandler));
            _userAccountService = userAccountService ?? throw new ArgumentNullException(nameof(userAccountService));

            Initialize();
        }

        private void Initialize()
        {
            RefreshPageCommand = new DelegateCommand(RefreshPage);
            SortByDueDateCommand = new DelegateCommand(SortByDueDate);
            SortByEndingPeriodCommand = new DelegateCommand(SortByEndingPeriod);

            ConfirmFilingCommand = new DelegateCommand<BusinessTaxAccountWithInstalmentModel>(ConfirmFiling);
            ConfirmInstalmentCommand = new DelegateCommand<BusinessTaxAccountWithInstalmentModel>(ConfirmInstalment);
            OpenTaxAccountDetailsDialogCommand = new DelegateCommand<TaxAccountWithInstalment>(OpenTaxAccountWithInstalmentDetailsDialog);
            SendPaperworkReminderEmailCommand = new DelegateCommand<TaxAccountWithInstalment>(SendPaperworkReminderEmail);
            SendInstalmentReminderEmailCommand = new DelegateCommand<TaxAccountWithInstalment>(SendInstalmentReminderEmail);
            
            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);
            NavigateToTaxAccountHistoryCommand = new DelegateCommand(NavigateToTaxAccountHistory);

            UserAccounts = _userAccountService.GetUserAccounts();

            LoadTaxAccounts();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is BusinessTaxAccountWithInstalmentModel model)
                    || (string.IsNullOrWhiteSpace(BusinessFilterText) && SelectedUserAccount == null))
                {
                    e.Accepted = true;
                    return;
                }

                if (SelectedUserAccount != null
                    && model.TaxAccount.UserAccountId != SelectedUserAccount.Id)
                {
                    e.Accepted = false;
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
            var taxAccounts = _taxAccountService.GetTaxAccountWithInstalmentsByAccountType(SelectedTaxAccountType);

            if (taxAccounts.Count > 0)
            {
                TaxAccountModels = new ObservableCollection<BusinessTaxAccountWithInstalmentModel>(
                    taxAccounts.Select(x => new BusinessTaxAccountWithInstalmentModel(x)));
            }
            else
            {
                TaxAccountModels = new ObservableCollection<BusinessTaxAccountWithInstalmentModel>();
            }

            CollectionViewSource.Source = TaxAccountModels; //RYAN: populate CollectionViewSource with TaxAccountModels
        }


        private void ConfirmFiling(BusinessTaxAccountWithInstalmentModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;
            var businessId = model.Business.Id;
            var taxAccountId = model.TaxAccount.Id;

            try
            {
                
                //RYAN: popup to check if instalment is needed
                var needed = _dialogService.ShowConfirmation("Instalment", "Is Instalment needed ?");
                if (needed)
                {
                    if (taxAccountId == null)
                    {
                        return;
                    }

                    var parameters = new DialogParameters();
                    parameters.Add("TaxAccountId", taxAccountId.ToString());
                    parameters.Add("BusinessId", businessId.ToString());

                    _dialogService.ShowDialog(nameof(Views.TaxAccountWithInstalmentBrief), parameters, dialog =>
                    {
                        if (dialog.Result == ButtonResult.OK)
                        {
                            //save to db with updated data: [user-input Instalment amount & auto-generated Instalment due date with a logic]
                                                        
                            //RYAN: do the filing, after this, EndingPeriod and DueDate will be updated                            
                            _filingHandler.ConfirmTaxFiling(model.TaxAccount, model.ConfirmText, confirmDate, currentUserId); 

                            model.ConfirmText = string.Empty;

                            var oldRecord = TaxAccountModels.FirstOrDefault(x => x.TaxAccount.Id == model.TaxAccount.Id);
                            var updated = _taxAccountService.GetTaxAccountWithInstalmentById(model.TaxAccount.Id);

                            if (oldRecord != null && updated != null)
                            {
                                oldRecord.TaxAccount = updated;

                                BusinessTaxAccountsView.Refresh();
                            }
                        }
                        else
                        {
                            //do nothing
                        }
                            
                    });
                }                              
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to confirm Tax Filing. {ex.Message}");
            }
        }

        private void ConfirmInstalment(BusinessTaxAccountWithInstalmentModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            try
            {
                _filingHandler.ConfirmInstalment(model.TaxAccount, model.InstalmentConfirmText, confirmDate, currentUserId);

                model.InstalmentConfirmText = string.Empty;

                var oldRecord = TaxAccountModels.FirstOrDefault(x => x.TaxAccount.Id == model.TaxAccount.Id);
                var updated = _taxAccountService.GetTaxAccountWithInstalmentById(model.TaxAccount.Id);

                if (oldRecord != null && updated != null)
                {
                    oldRecord.TaxAccount = updated;

                    BusinessTaxAccountsView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to confirm Instalment. {ex.Message}");
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
            navParams.Add("PreviousPage", ViewRegKeys.TaxAccountWithInstalmentOverview);
            navParams.Add("TaxType", SelectedTaxAccountType);

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.TaxAccountHistory, navParams);
        }

        private void OpenTaxAccountWithInstalmentDetailsDialog(TaxAccountWithInstalment taxAccount)
        {
            if (taxAccount == null)
            {
                return;
            }

            var parameters = new DialogParameters($"TaxAccountId={taxAccount.Id}");

            _dialogService.ShowDialog(nameof(Views.TaxAccountWithInstalmentDetails), parameters, d =>
            {
                if (d.Result == ButtonResult.OK)
                {
                    var updatedRecord = _taxAccountService.GetTaxAccountWithInstalmentById(taxAccount.Id);

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

        private void SendPaperworkReminderEmail(TaxAccountWithInstalment taxAccount)
        {
            if (VerifyBusinessEmailInfos(taxAccount, out Business business) == false)
            {
                return;
            }

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "{LegalName}", business.LegalName },
                    { "{OperatingName}", business.OperatingName },
                    { "{EmailContact}", business.EmailContact },
                    { "{EndingPeriod}", taxAccount.EndingPeriod.ToString("MMM-dd-yyyy") },
                    { "{DueDate}", taxAccount.DueDate.ToString("MMM-dd-yyyy") },
                };

                if (SelectedTaxAccountType == TaxAccountType.Corporation)
                {
                    _emailService.SendCorporationTaxReminderEmail(parameters, business.Email);
                }
                else if (SelectedTaxAccountType == TaxAccountType.HST)
                {
                    _emailService.SendHSTReminderEmail(parameters, business.Email);
                }
                else
                {
                    throw new ArgumentException($"Unknown Tax Account Type {SelectedTaxAccountType}");
                }

                _dialogService.ShowInformation("Email Sent", "Reminder email has been sent!");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR Sending Email", ex.Message);
            }
        }

        private void SendInstalmentReminderEmail(TaxAccountWithInstalment taxAccount)
        {
            if (VerifyBusinessEmailInfos(taxAccount, out Business business) == false)
            {
                return;
            }

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "{LegalName}", business.LegalName },
                    { "{OperatingName}", business.OperatingName },
                    { "{EmailContact}", business.EmailContact },
                    { "{InstalmentDueDate}", taxAccount.InstalmentDueDate?.ToString("MMM-dd-yyyy") ?? "[DueDate]" },
                    { "{InstalmentAmount}", taxAccount.InstalmentAmount.ToString() },
                };

                if (SelectedTaxAccountType == TaxAccountType.Corporation)
                {
                    _emailService.SendCorporationInstalmentReminderEmail(parameters, business.Email);
                }
                else if (SelectedTaxAccountType == TaxAccountType.HST)
                {
                    _emailService.SendHSTInstalmentReminderEmail(parameters, business.Email);
                }
                else
                {
                    throw new ArgumentException($"Unknown Tax Account Type {SelectedTaxAccountType}");
                }

                _dialogService.ShowInformation("Email Sent", "Instalment reminder email has been sent!");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR Sending Email", ex.Message);
            }
        }

        private bool VerifyBusinessEmailInfos(TaxAccountWithInstalment taxAccount, out Business business)
        {
            business = _businessOwnerService.GetBusinessById(taxAccount.BusinessId);
            if (business == null)
            {
                _dialogService.ShowError(new Core.Exceptions.BusinessNotFoundException(taxAccount.BusinessId),
                    "Business Not Found", $"Business account for Tax Account [{taxAccount.AccountNumber}] not found");

                return false;
            }

            if (string.IsNullOrWhiteSpace(business.Email) || string.IsNullOrWhiteSpace(business.EmailContact))
            {
                _dialogService.ShowInformation("Email Infos Not Setup Yet", "Please setup Business Email and EmailContact first and try again.");

                return false;
            }

            return true;
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
            if (string.IsNullOrEmpty(filterText))
            {
                return true;
            }

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return input.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
