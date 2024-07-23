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

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class TaxInstalmentOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView TaxInstalmentAccountsView => CollectionViewSource.View;

        private ObservableCollection<TaxInstalmentAccountModel> _taxInstalmentAccounts;
        public ObservableCollection<TaxInstalmentAccountModel> TaxInstalmentAccounts
        {
            get { return _taxInstalmentAccounts; }
            set { SetProperty(ref _taxInstalmentAccounts, value); }
        }

        private TaxInstalmentAccountModel _selectedTaxInstalmentAccount;
        public TaxInstalmentAccountModel SelectedTaxInstalmentAccount
        {
            get { return _selectedTaxInstalmentAccount; }
            set { SetProperty(ref _selectedTaxInstalmentAccount, value); }
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
                    TaxInstalmentAccountsView.Refresh();

                    RaisePropertyChanged(nameof(TaxInstalmentAccountsCount));
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
                    TaxInstalmentAccountsView.Refresh();
                    RaisePropertyChanged(nameof(TaxInstalmentAccountsCount));
                }
            }
        }

        private string _instalDueDateHeaderText;
        public string InstalDueDateHeaderText
        {
            get { return _instalDueDateHeaderText; }
            set { SetProperty(ref _instalDueDateHeaderText, value); }
        }

        public int TaxInstalmentAccountsCount
        {
            get { return TaxInstalmentAccountsView.Cast<object>()?.Count() ?? 0; }
        }

        /*
        private TaxAccountType _selectedTaxAccountType;
        public TaxAccountType SelectedTaxAccountType
        {
            get { return _selectedTaxAccountType; }
            set
            {
                if (SetProperty(ref _selectedTaxAccountType, value))
                {
                    LoadTaxInstalmentAccounts();

                    RaisePropertyChanged("BusinessTaxAccountsView");
                }
            }
        }
        */
        /*
        private string _itemFilterText;
        public string ItemFilterText
        {
            get { return _itemFilterText; }
            set
            {
                if (SetProperty(ref _itemFilterText, value))
                {
                    TaxInstalmentAccountsView.Refresh();
                }
            }
        }
        */
        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand SortByDueDateCommand { get; private set; }
        public DelegateCommand<TaxInstalmentAccountModel> ConfirmInstalmentCommand { get; private set; }
        public DelegateCommand<TaxAccountWithInstalment> OpenTaxAccountDetailsDialogCommand { get; private set; }
        public DelegateCommand<Business> NavigateToBusinessOverviewCommand { get; private set; }
        public DelegateCommand<TaxInstalmentAccountModel> OpenTaxInstalmentDetailsDialogCommand { get; private set; }
        public DelegateCommand<TaxAccountWithInstalment> SendInstalmentReminderEmailCommand { get; private set; }
        public DelegateCommand NavigateToInstalmentHistoryCommand { get; private set; }
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

        
        public TaxInstalmentOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
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

            ConfirmInstalmentCommand = new DelegateCommand<TaxInstalmentAccountModel>(ConfirmInstalment);
            OpenTaxAccountDetailsDialogCommand = new DelegateCommand<TaxAccountWithInstalment>(OpenTaxAccountWithInstalmentDetailsDialog);
            NavigateToBusinessOverviewCommand = new DelegateCommand<Business>(NavigateToBusinessOverview);
            SendInstalmentReminderEmailCommand = new DelegateCommand<TaxAccountWithInstalment>(SendInstalmentReminderEmail);

            UserAccounts = _userAccountService.GetUserAccounts();

            LoadTaxInstalmentAccounts();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is TaxInstalmentAccountModel model)
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

            SortByDueDate();
        }

        private void RefreshPage()
        {
            LoadTaxInstalmentAccounts();

            // TODO: Can I skip this step?
            RaisePropertyChanged("TaxInstalmentAccountsView");
            RaisePropertyChanged("TaxInstalmentAccountsCount");

            // TODO: Or this step?
            TaxInstalmentAccountsView.Refresh();
        }

        private void LoadTaxInstalmentAccounts()
        {
            //var instalmentAccounts = _taxAccountService.GetTaxAccountWithInstalments();
            var instalmentAccounts = _taxAccountService.GetTaxAccountWithInstalmentRequiredAndActive();

            if (instalmentAccounts.Count > 0)
            {
                TaxInstalmentAccounts = new ObservableCollection<TaxInstalmentAccountModel>(
                    instalmentAccounts.Select(x => new TaxInstalmentAccountModel(x)));
            }
            else
            {
                TaxInstalmentAccounts = new ObservableCollection<TaxInstalmentAccountModel>();
            }

            CollectionViewSource.Source = TaxInstalmentAccounts; //RYAN: populate CollectionViewSource with TaxInstalmentAccounts
        }

        private void SendInstalmentReminderEmail(TaxAccountWithInstalment taxInstalmentAccount)
        {
            if (VerifyBusinessEmailInfos(taxInstalmentAccount, out Business business) == false)
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
                    { "{InstalmentDueDate}", taxInstalmentAccount.InstalmentDueDate?.ToString("MMM-dd-yyyy") ?? "[DueDate]" },
                    { "{InstalmentAmount}", taxInstalmentAccount.InstalmentAmount.ToString() },
                };

                if (taxInstalmentAccount.AccountType == TaxAccountType.Corporation)
                {
                    _emailService.SendCorporationInstalmentReminderEmail(parameters, business.Email);
                }
                else if (taxInstalmentAccount.AccountType == TaxAccountType.HST)
                {
                    _emailService.SendHSTInstalmentReminderEmail(parameters, business.Email);
                }
                else
                {
                    throw new ArgumentException($"Unknown Tax Account Type {taxInstalmentAccount.AccountType}");
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
            InstalDueDateHeaderText = "Instalment Due Date";

            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("TaxAccount.InstalmentDueDate", ListSortDirection.Ascending),
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
        private void ConfirmInstalment(TaxInstalmentAccountModel model)
        {
            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            try
            {
                _filingHandler.ConfirmInstalment(model.TaxAccount, model.InstalmentConfirmText, confirmDate, currentUserId);

                model.InstalmentConfirmText = string.Empty;

                var oldRecord = TaxInstalmentAccounts.FirstOrDefault(x => x.TaxAccount.Id == model.TaxAccount.Id);
                var updated = _taxAccountService.GetTaxAccountWithInstalmentById(model.TaxAccount.Id);

                if (oldRecord != null && updated != null)
                {
                    oldRecord.TaxAccount = updated;

                    TaxInstalmentAccountsView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error", $"Failed to confirm Instalment. {ex.Message}");
            }
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

                    var oldRecord = TaxInstalmentAccounts.FirstOrDefault(x => x.TaxAccount.Id == taxAccount.Id);
                    if (oldRecord != null)
                    {
                        if (updatedRecord.IsActive == false)
                        {
                            TaxInstalmentAccounts.Remove(oldRecord);
                        }
                        else
                        {
                            oldRecord.TaxAccount = updatedRecord;
                        }

                        TaxInstalmentAccountsView.Refresh();
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
    }
}
