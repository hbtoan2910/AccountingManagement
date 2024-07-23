using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class BusinessDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Const
        public List<FilingCycle> HSTCycleList => new List<FilingCycle>
        {
            FilingCycle.Annually,
            FilingCycle.Quarterly,
            FilingCycle.Monthly,
        };

        public List<FilingCycle> CorporationTaxCycleList => new List<FilingCycle>
        {
            FilingCycle.Annually
        };

        public List<FilingCycle> PSTCycleList => new List<FilingCycle>
        {
            FilingCycle.Annually,
            FilingCycle.Quarterly,
            FilingCycle.Monthly,
        };

        public List<FilingCycle> PayrollCycleList => new List<FilingCycle>
        {
            FilingCycle.BiWeekly,
            FilingCycle.SemiMonthly,
            FilingCycle.Monthly,
            FilingCycle.None,
        };

        public List<FilingCycle> PD7ACycleList => new List<FilingCycle>
        {
            FilingCycle.Monthly,
            FilingCycle.Quarterly,
            FilingCycle.None,
        };

        public List<ClientPaymentCycle> ClientPaymentCycleList => new List<ClientPaymentCycle>
        {
            ClientPaymentCycle.Monthly,
            ClientPaymentCycle.BiMonthly,
            ClientPaymentCycle.Quarterly,
            ClientPaymentCycle.Undefined,
        };
        #endregion

        #region Bindings and Commands
        private Business _business;
        public Business Business
        {
            get { return _business; }
            set { SetProperty(ref _business, value); }
        }

        private TaxAccountWithInstalment _hstAccount;
        public TaxAccountWithInstalment HSTAccount
        {
            get { return _hstAccount; }
            set { SetProperty(ref _hstAccount, value); }
        }

        private TaxAccountWithInstalment _corporationTaxAccount;
        public TaxAccountWithInstalment CorporationTaxAccount
        {
            get { return _corporationTaxAccount; }
            set { SetProperty(ref _corporationTaxAccount, value); }
        }

        private TaxAccount _pstAccount;
        public TaxAccount PSTAccount
        {
            get { return _pstAccount; }
            set { SetProperty(ref _pstAccount, value); }
        }

        private TaxAccount _wsibAccount;
        public TaxAccount WSIBAccount
        {
            get { return _wsibAccount; }
            set { SetProperty(ref _wsibAccount, value); }
        }

        private TaxAccount _liqAccount;
        public TaxAccount LIQAccount
        {
            get { return _liqAccount; }
            set { SetProperty(ref _liqAccount, value); }
        }

        private TaxAccount _ontAccount;
        public TaxAccount ONTAccount
        {
            get { return _ontAccount; }
            set { SetProperty(ref _ontAccount, value); }
        }

        private ClientPayment _regularPayment;
        public ClientPayment RegularPayment
        {
            get { return _regularPayment; }
            set
            {
                SetProperty(ref _regularPayment, value);
            }
        }

        private ClientPayment _clientPayment2nd;
        public ClientPayment ClientPayment2nd
        {
            get { return _clientPayment2nd; }
            set
            {
                SetProperty(ref _clientPayment2nd, value);
            }
        }


        private PayrollAccount _payrollAccount;
        public PayrollAccount PayrollAccount 
        { 
            get { return _payrollAccount; }
            set { SetProperty(ref _payrollAccount, value); }
        }

        private string _errorMessageText;
        public string ErrorMessageText
        {
            get { return _errorMessageText; }
            set { SetProperty(ref _errorMessageText, value); }
        }

        #region Filterable Owner List in a Combo Box
        private Owner _selectedOwner;
        public Owner SelectedOwner
        {
            get { return _selectedOwner; }
            set { SetProperty(ref _selectedOwner, value); }
        }

        private string _ownerFilterText;
        public string OwnerFilterText
        {
            get { return _ownerFilterText; }
            set
            {
                if (SetProperty(ref _ownerFilterText, value))
                {
                    OwnerFilterableList.Refresh();
                }
            }
        }

        public CollectionViewSource OwnerFilterableViewSource = new CollectionViewSource();
        public ICollectionView OwnerFilterableList => OwnerFilterableViewSource.View;

        private ObservableCollection<Owner> _ownerList;
        public ObservableCollection<Owner> OwnerList
        {
            get { return _ownerList; }
            set { SetProperty(ref _ownerList, value); }
        }

        private List<UserAccount> _assignableUserAccounts;
        public List<UserAccount> AssignableUserAccounts
        {
            get { return _assignableUserAccounts; }
        }
        #endregion

        public DelegateCommand<Business> SaveBusinessCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }

        public DelegateCommand<Guid?> AddBusinessOwnerCommand { get; private set; }
        public DelegateCommand<int?> RemoveBusinessOwnerCommand { get; private set; }

        public DelegateCommand<Business> AddNewHSTAccountCommand { get; private set; }
        public DelegateCommand<Business> AddNewCorporationTaxAccountCommand { get; private set; }
        public DelegateCommand<Business> AddNewPSTAccountCommand { get; private set; }
        public DelegateCommand<Business> AddNewWSIBAccountCommand { get; private set; }
        public DelegateCommand<Business> AddNewLIQAccountCommand { get; private set; }
        public DelegateCommand<Business> AddNewONTAccountCommand { get; private set; }
        public DelegateCommand<Business> AddNewPayrollAccountCommand { get; private set; }

        public DelegateCommand<Business> AddNewRegularPaymentCommand { get; private set; }
        public DelegateCommand CustomerPaymentActivationChangedCommand { get; private set; }

        public DelegateCommand<Business> AddNewClientPayment2ndCommand { get; private set; }
        public DelegateCommand ClientPayment2ndActivationChangedCommand { get; private set; }
        #endregion

        private readonly IEventAggregator _eventAggregator;
        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly IClientPaymentService _clientPaymentService;
        private readonly IPayrollService _payrollService;
        private readonly ITaxAccountService _taxAccountService;
        private readonly IUserAccountService _userAccountService;

        public BusinessDetailsViewModel(IEventAggregator eventAggregator, IBusinessOwnerService businessOwnerService,
            IClientPaymentService clientPaymentService, IPayrollService payrollService, ITaxAccountService taxAccountService,
            IUserAccountService userAccountService)
        {
            _eventAggregator = eventAggregator;

            _businessOwnerService = businessOwnerService;
            _clientPaymentService = clientPaymentService;
            _payrollService = payrollService;
            _taxAccountService = taxAccountService;
            _userAccountService = userAccountService;

            Initialize();
        }

        private void Initialize()
        {
            _assignableUserAccounts = _userAccountService.GetUserAccounts();

            SaveBusinessCommand = new DelegateCommand<Business>(UpsertBusiness);
            CloseDialogCommand = new DelegateCommand(CloseDialog);

            AddBusinessOwnerCommand = new DelegateCommand<Guid?>(AddBusinessOwner);
            RemoveBusinessOwnerCommand = new DelegateCommand<int?>(RemoveBusinessOwner);

            AddNewHSTAccountCommand = new DelegateCommand<Business>(AddNewHSTAccount);
            AddNewCorporationTaxAccountCommand = new DelegateCommand<Business>(AddNewCorporationTaxAccount);
            AddNewPSTAccountCommand = new DelegateCommand<Business>(AddNewPSTAccount);
            AddNewWSIBAccountCommand = new DelegateCommand<Business>(AddNewWSIBAccount);
            AddNewLIQAccountCommand = new DelegateCommand<Business>(AddNewLIQAccount);
            AddNewONTAccountCommand = new DelegateCommand<Business>(AddNewONTAccount);
            AddNewPayrollAccountCommand = new DelegateCommand<Business>(AddNewPayrollAccount);

            AddNewRegularPaymentCommand = new DelegateCommand<Business>(AddNewRegularPayment);
            CustomerPaymentActivationChangedCommand = new DelegateCommand(CustomerPaymentActivationChanged);

            AddNewClientPayment2ndCommand = new DelegateCommand<Business>(AddNewClientPayment2nd);
            ClientPayment2ndActivationChangedCommand = new DelegateCommand(ClientPayment2ndActivationChanged);

            OwnerList = new ObservableCollection<Owner>(_businessOwnerService.GetOwners()
                .OrderBy(x => x.Name)
                .ToList());

            OwnerFilterableViewSource.Source = OwnerList;
            OwnerFilterableViewSource.Filter += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(OwnerFilterText))
                {
                    e.Accepted = true;
                    return;
                }

                if (e.Item is Owner owner && string.IsNullOrWhiteSpace(owner.Name) == false
                    && owner.Name.Contains(OwnerFilterText, StringComparison.InvariantCultureIgnoreCase))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            };
        }

        private void UpsertBusiness(Business business)
        {
            try
            {
                // TODO: An improvement is to batch the save operation into one transaction
                if (business != null)
                {
                    _businessOwnerService.UpsertBusiness(business);
                }

                if (business.BusinessOwners.Count == 0 && SelectedOwner != null)
                {
                    AddBusinessOwner(SelectedOwner.Id);
                }

                if (HSTAccount != null)
                {
                    SaveTaxAccountWithInstalment(HSTAccount);
                }

                if (CorporationTaxAccount != null)
                {
                    SaveTaxAccountWithInstalment(CorporationTaxAccount);
                }

                if (PSTAccount != null)
                {
                    SaveTaxAccount(PSTAccount);
                }

                if (WSIBAccount != null)
                {
                    SaveTaxAccount(WSIBAccount);
                }
                
                if (LIQAccount != null)
                {
                    SaveTaxAccount(LIQAccount);
                }

                if (ONTAccount != null)
                {
                    SaveTaxAccount(ONTAccount);
                }

                if (RegularPayment != null)
                {
                    SaveClientPayment(RegularPayment);
                }

                if (ClientPayment2nd != null)
                {
                    SaveClientPayment(ClientPayment2nd);
                }

                if (PayrollAccount != null)
                {
                    SavePayrollAccount(PayrollAccount);
                }

                _eventAggregator.GetEvent<Events.BusinessUpsertedEvent>().Publish(business.Id);

                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error while updating Business details. {ex.Message}", ex);
            }
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        private void AddBusinessOwner(Guid? ownerId)
        {
            if (ownerId == null || SelectedOwner == null)
            {
                ShowErrorMessage("Must select an Owner from the list");
                return;
            }

            var existingBusiness = _businessOwnerService.GetBusinessByIdWithFullDetails(Business.Id);
            if (existingBusiness == null)
            {
                ShowErrorMessage("Must add Business first");
                return;
            }
            if (existingBusiness.BusinessOwners.Any(bo => bo.OwnerId == ownerId))
            {
                ShowErrorMessage("Selected Owner already owns this Business");
                return;
            }

            try
            {
                if (_businessOwnerService.InsertBusinessOwner(Business.Id, ownerId.Value))
                {
                    var businessId = Business.Id;
                    Business = _businessOwnerService.GetBusinessByIdWithFullDetails(businessId);

                    SelectedOwner = null;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Unable to add Business-Owner relationship", ex);
            }
        }

        private void RemoveBusinessOwner(int? businessOwnerId)
        {
            if (businessOwnerId == null)
            {
                return;
            }

            try
            {
                if (_businessOwnerService.RemoveBusinessOwner(businessOwnerId.Value))
                {
                    var businessId = Business.Id;
                    Business = _businessOwnerService.GetBusinessByIdWithFullDetails(businessId);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Unable to remove Owner-Business relationship", ex);
            }
        }

        private void AddNewHSTAccount(Business business)
        {
            if (business == null || HSTAccount != null)
            {
                return;
            }

            var year = DateTime.Now.Year;

            HSTAccount = new TaxAccountWithInstalment
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                AccountType = TaxAccountType.HST,
                AccountNumber = business.BusinessNumber + "RT0001",
                Cycle = FilingCycle.Annually,
                EndingPeriod = new DateTime(year, 12, 31),
                DueDate = new DateTime(year, 12, 31),
                IsActive = true,
                Notes = "New HST Account",
                ProgressNotes = string.Empty,
                InstalmentRequired = false,
                InstalmentAmount = 0,
                InstalmentDueDate = null
            };
        }

        private void AddNewCorporationTaxAccount(Business business)
        {
            if (business == null || CorporationTaxAccount != null)
            {
                return;
            }

            var year = DateTime.Now.Year;

            CorporationTaxAccount = new TaxAccountWithInstalment
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                AccountType = TaxAccountType.Corporation,
                AccountNumber = business.BusinessNumber + "RC0001",
                Cycle = FilingCycle.Annually,
                EndingPeriod = new DateTime(year, 12, 31),
                DueDate = new DateTime(year, 12, 31),
                IsActive = true,
                Notes = "New Corporation Tax Account",
                ProgressNotes = string.Empty,
                InstalmentRequired = false,
                InstalmentAmount = 0,
                InstalmentDueDate = null
            };
        }

        private void AddNewPSTAccount(Business business)
        {
            if (business == null)
            {
                return;
            }

            var year = DateTime.Now.Year;

            PSTAccount = new TaxAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                AccountType = TaxAccountType.PST,
                AccountNumber = "0000000",
                Cycle = FilingCycle.Annually,
                EndingPeriod = new DateTime(year, 12, 31),
                DueDate = new DateTime(year, 12, 31),
                IsActive = true,
                Notes = "New PST Account",
            };
        }

        private void AddNewWSIBAccount(Business business)
        {
            if (business == null)
            {
                return;
            }

            var year = DateTime.Now.Year;

            WSIBAccount = new TaxAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                AccountType = TaxAccountType.WSIB,
                AccountNumber = "0000000",
                Cycle = FilingCycle.Quarterly,
                EndingPeriod = new DateTime(year, 12, 31),
                DueDate = new DateTime(year, 12, 31),
                IsActive = true,
                Notes = "New WSIB/WCB Account",
            };
        }

        private void AddNewLIQAccount(Business business)
        {
            if (business == null)
            {
                return;
            }

            var year = DateTime.Now.Year;

            LIQAccount = new TaxAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                AccountType = TaxAccountType.LIQ,
                AccountNumber = "0000000",
                Cycle = FilingCycle.Quarterly,
                EndingPeriod = new DateTime(year, 12, 31),
                DueDate = new DateTime(year, 12, 31),
                IsActive = true,
                Notes = "New LIQ Account",
            };
        }

        private void AddNewONTAccount(Business business)
        {
            if (business == null)
            {
                return;
            }

            var defaultDate = new DateTime(DateTime.Now.Year, 12, 31);

            ONTAccount = new TaxAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                AccountType = TaxAccountType.ONT,
                AccountNumber = "0000000",
                Cycle = FilingCycle.Annually,
                EndingPeriod = CorporationTaxAccount?.EndingPeriod ?? defaultDate,
                DueDate = CorporationTaxAccount?.DueDate ?? defaultDate,
                IsActive = true,
                Notes = "New ONT Account",
            };
        }

        private void AddNewPayrollAccount(Business business)
        {
            if (business == null)
            {
                return;
            }

            PayrollAccount = new PayrollAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                PayrollNumber = business.BusinessNumber + "RP001",
                PayrollCycle = FilingCycle.None,
                IsRunPayroll = true,
                PD7ACycle = FilingCycle.None,
                IsPayPD7A = true
            };

            // RaisePropertyChanged("ShowAddPayrollAccountButton");
        }

        private void AddNewRegularPayment(Business business)
        {
            if (business == null)
            {
                return;
            }

            var defaultDate = new DateTime(DateTime.Now.Year, 12, 31);

            RegularPayment = new ClientPayment
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                PaymentType = ClientPaymentType.Regular,
                PaymentCycle = ClientPaymentCycle.Monthly,
                BankInfo = "0000000",
                DueDate = defaultDate,
                IsActive = true,
                Notes = "New Customer Payment profile",
            };
        }

        private void AddNewClientPayment2nd(Business business)
        {
            if (business == null)
            {
                return;
            }

            var defaultDate = new DateTime(DateTime.Now.Year, 12, 31);

            ClientPayment2nd = new ClientPayment
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                PaymentType = ClientPaymentType.Secondary,
                PaymentCycle = ClientPaymentCycle.Monthly,
                BankInfo = "0000000",
                DueDate = defaultDate,
                IsActive = true,
                Notes = "New Customer Payment (2nd) profile",
            };
        }

        private void CustomerPaymentActivationChanged()
        {
            if (RegularPayment.IsActive && RegularPayment.PaymentCycle == ClientPaymentCycle.Undefined)
            {
                RegularPayment.DueDate = DateTime.Now.Date;
                RaisePropertyChanged("RegularPayment");
            }
            else if (RegularPayment.IsActive == false)
            {
                RegularPayment.TmpConfirmationText = string.Empty;
                RegularPayment.TmpReceiptEmailSent = false;
                RaisePropertyChanged("RegularPayment");
            }
        }

        private void ClientPayment2ndActivationChanged()
        {
            if (ClientPayment2nd.IsActive && ClientPayment2nd.PaymentCycle == ClientPaymentCycle.Undefined)
            {
                ClientPayment2nd.DueDate = DateTime.Now.Date;
                RaisePropertyChanged("ClientPayment2nd");
            }
            else if (ClientPayment2nd.IsActive == false)
            {
                ClientPayment2nd.TmpConfirmationText = string.Empty;
                ClientPayment2nd.TmpReceiptEmailSent = false;
                RaisePropertyChanged("ClientPayment2nd");
            }
        }

        private void SaveTaxAccountWithInstalment(TaxAccountWithInstalment taxAccount)
        {
            _taxAccountService.UpsertTaxAccountWithInstalment(taxAccount);
        }

        private void SaveTaxAccount(TaxAccount taxAccount)
        {
            _taxAccountService.UpsertTaxAccount(taxAccount);
        }

        private void SaveClientPayment(ClientPayment clientPayment)
        {
            _clientPaymentService.UpsertClientPayment(clientPayment);
        }

        private void SavePayrollAccount(PayrollAccount payrollAccount)
        {
            _payrollService.UpsertPayrollAccount(payrollAccount);
        }

        private void ShowErrorMessage(string message, Exception ex = null)
        {
            ErrorMessageText = ex != null
                ? $"ERROR: {message}. Exception: {ex.Message}"
                : $"ERROR: {message}";

            if (ex != null)
            {
                Log.Error(ex, ErrorMessageText);
            }
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // Clear initial selection in Owner comboxbox. Not sure when this value is set
            SelectedOwner = null;

            if (Guid.TryParse(parameters.GetValue<string>("BusinessId"), out Guid businessId))
            {
                Business = _businessOwnerService.GetBusinessByIdWithFullDetails(businessId);

                PayrollAccount = Business.PayrollAccount;
                HSTAccount = Business.HSTAccount;
                CorporationTaxAccount = Business.CorporationTaxAccount;
                PSTAccount = Business.PSTAccount;
                WSIBAccount = Business.WSIBAccount;
                LIQAccount = Business.LIQAccount;
                ONTAccount = Business.ONTAccount;

                RegularPayment = Business.RegularPayment;
                ClientPayment2nd = Business.ClientPayment2nd;
            }
            else
            {
                Business = new Business()
                {
                    Id = Guid.NewGuid(),
                };
            }
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Nothing yet
        }
        #endregion
    }
}
