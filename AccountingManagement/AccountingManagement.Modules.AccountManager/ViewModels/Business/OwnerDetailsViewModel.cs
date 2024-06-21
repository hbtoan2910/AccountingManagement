using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class OwnerDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings and Commands
        private Owner _owner;
        public Owner Owner
        {
            get { return _owner; }
            set { SetProperty(ref _owner, value); }
        }

        private PersonalTaxAccount _t1Account;
        public PersonalTaxAccount T1Account
        {
            get { return _t1Account; }
            set { SetProperty(ref _t1Account, value); }
        }

        private string _addOrRemoveBusinessOwnerErrorText;
        public string AddOrRemoveBusinessOwnerErrorText
        { 
            get { return _addOrRemoveBusinessOwnerErrorText; } 
            set { SetProperty(ref _addOrRemoveBusinessOwnerErrorText, value); }
        }

        #region Filterable Business List in a Combo Box
        public CollectionViewSource BusinessFilterableViewSource = new CollectionViewSource();
        public ICollectionView BusinessFilterableView => BusinessFilterableViewSource.View;

        private ObservableCollection<Business> _businessList;
        public ObservableCollection<Business> BusinessList
        {
            get { return _businessList; }
            set { SetProperty(ref _businessList, value); }
        }

        private Business _selectedBusiness;
        public Business SelectedBusiness
        {
            get { return _selectedBusiness; }
            set { SetProperty(ref _selectedBusiness, value); }
        }

        private string _businessFilterText;
        public string BusinessFilterText
        {
            get { return _businessFilterText; }
            set
            {
                if (SetProperty(ref _businessFilterText, value))
                {
                    BusinessFilterableView.Refresh();
                }
            }
        }
        #endregion

        public DelegateCommand<Owner> SaveOwnerCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }

        public DelegateCommand<Guid?> AddBusinessOwnerCommand { get; private set; }
        public DelegateCommand<int?> RemoveBusinessOwnerCommand { get; private set; }

        public DelegateCommand<Owner> AddNewT1AccountCommand { get; private set; }
        #endregion

        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly ITaxAccountService _taxAccountService;
        private readonly IUserAccountService _userAccountService;

        public OwnerDetailsViewModel(IBusinessOwnerService businessOwnerService, ITaxAccountService taxAccountService,
            IUserAccountService userAccountService)
        {
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _userAccountService = userAccountService ?? throw new ArgumentNullException(nameof(userAccountService));

            Initialize();
        }

        private void Initialize()
        {
            SaveOwnerCommand = new DelegateCommand<Owner>(UpsertOwner);
            CloseDialogCommand = new DelegateCommand(CloseDialog);

            AddBusinessOwnerCommand = new DelegateCommand<Guid?>(AddBusinessOwner);
            RemoveBusinessOwnerCommand = new DelegateCommand<int?>(RemoveBusinessOwner);

            AddNewT1AccountCommand = new DelegateCommand<Owner>(AddNewT1Account);

            BusinessList = new ObservableCollection<Business>(_businessOwnerService.GetBusinessListOnly()
                .OrderBy(x => x.OperatingName)
                .ToList());

            BusinessFilterableViewSource.Source = BusinessList;
            BusinessFilterableViewSource.Filter += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(BusinessFilterText))
                {
                    e.Accepted = true;
                    return;
                }

                if (e.Item is Business business 
                    && (StringContainsFilterText(business.OperatingName, BusinessFilterText) || StringContainsFilterText(business.LegalName, BusinessFilterText)))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            };
        }

        private void UpsertOwner(Owner owner)
        {
            try
            {
                if (owner != null)
                {
                    _businessOwnerService.UpsertOwner(owner);
                }

                if (T1Account != null)
                {
                    SavePersonalTaxAccount(T1Account);
                }

                if (owner.BusinessOwners.Count == 0 && SelectedBusiness != null)
                {
                    AddBusinessOwner(SelectedBusiness.Id);
                }
                
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Unable to add or update Owner details", ex);
            }
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        private void AddBusinessOwner(Guid? businessId)
        {
            if (businessId == null || SelectedBusiness == null)
            {
                ShowErrorMessage("Must select a Business from the list", null);
                return;
            }

            var existingOwner = _businessOwnerService.GetOwnerById(Owner.Id);
            if (existingOwner == null)
            {
                ShowErrorMessage("Must add Owner first", null);
                return;
            }
            if (existingOwner.BusinessOwners.Any(bo => bo.BusinessId == businessId))
            {
                ShowErrorMessage("Owner already has the selected Business", null);
                return;
            }

            try
            {
                if (_businessOwnerService.InsertBusinessOwner(businessId.Value, Owner.Id))
                {
                    var ownerId = Owner.Id;
                    Owner = _businessOwnerService.GetOwnerById(ownerId);

                    SelectedBusiness = null;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Unable to add Owner-Business relationship", ex);
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
                    var ownerId = Owner.Id;
                    Owner = _businessOwnerService.GetOwnerById(ownerId);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Unable to remove Owner-Business relationship", ex);
            }
        }

        private void AddNewT1Account(Owner owner)
        {
            if (owner == null)
            {
                return;
            }

            T1Account = new PersonalTaxAccount
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                TaxNumber = string.Empty,
                TaxType = PersonalTaxType.T1,
                Description = "",
                TaxYear = DateTime.Now.Year.ToString(),
                Notes = "New T1 Account",
                IsHighPriority = false,
                IsActive = true,
            };
        }

        private void SavePersonalTaxAccount(PersonalTaxAccount account)
        {
            _taxAccountService.UpsertPersonalTaxAccount(account);
        }

        private void ShowErrorMessage(string message, Exception ex)
        {
            AddOrRemoveBusinessOwnerErrorText = ex != null
                ? $"ERROR: {message}. Exception: {ex.Message}"
                : $"ERROR: {message}";
        }

        private bool StringContainsFilterText(string searchString, string filterText)
        {
            return string.IsNullOrWhiteSpace(searchString) == false
                && searchString.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // Clear initial selection in Business comboxbox. Not sure when this value is set
            SelectedBusiness = null;

            // Edit an existing Owner
            if (Guid.TryParse(parameters.GetValue<string>("OwnerId"), out Guid ownerId))
            {
                Owner = _businessOwnerService.GetOwnerById(ownerId);

                T1Account = Owner.T1Account;
            }
            // Add new Owner, having BusinessId as an initial relationship
            else
            {
                var newId = Guid.NewGuid();
                Owner = new Owner()
                {
                    Id = newId,
                    BusinessOwners = new List<BusinessOwner>()
                    { }
                };

                if (Guid.TryParse(parameters.GetValue<string>("BusinessId"), out Guid businessId))
                {
                    SelectedBusiness = BusinessList.Where(x => x.Id == businessId).FirstOrDefault();
                }
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
