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
using AccountingManagement.Modules.AccountManager.Helpers;
using AccountingManagement.Modules.AccountManager.Models;
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;
using AccountingManagement.Services.Email;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class PersonalTaxAccountOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView PersonalTaxAccountsView => CollectionViewSource.View;

        private ObservableCollection<OwnerPersonalTaxAccountModel> _personalTaxAccountModels;
        public ObservableCollection<OwnerPersonalTaxAccountModel> PersonalTaxAccountModels
        {
            get { return _personalTaxAccountModels; }
            set { SetProperty(ref _personalTaxAccountModels, value); }
        }

        private OwnerPersonalTaxAccountModel _selectedPersonalTaxAccountModel;
        public OwnerPersonalTaxAccountModel SelectedPersonalTaxAccountModel
        {
            get { return _selectedPersonalTaxAccountModel; }
            set { SetProperty(ref _selectedPersonalTaxAccountModel, value); }
        }

        private string _ownerFilterText;
        public string OwnerFilterText
        {
            get { return _ownerFilterText; }
            set
            {
                if (SetProperty(ref _ownerFilterText, value))
                {
                    PersonalTaxAccountsView.Refresh();
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
                    PersonalTaxAccountsView.Refresh();
                }
            }
        }

        private T1Progress _t1ProgressFilter;
        public T1Progress T1ProgressFilter
        {
            get { return _t1ProgressFilter; }
            set 
            { 
                if (SetProperty(ref _t1ProgressFilter, value))
                {
                    PersonalTaxAccountsView.Refresh();
                }
            }
        }

        public DelegateCommand RefreshPageCommand { get; private set; }
        public DelegateCommand<PersonalTaxAccount> UpdateStep1StatusCommand { get; private set; }
        public DelegateCommand<PersonalTaxAccount> UpdateStep2StatusCommand { get; private set; }
        public DelegateCommand<PersonalTaxAccount> UpdateStep3StatusCommand { get; private set; }
        public DelegateCommand<PersonalTaxAccount> UpdateStep4StatusCommand { get; private set; }
        public DelegateCommand<PersonalTaxAccount> UpdateStep5StatusCommand { get; private set; }
        public DelegateCommand<OwnerPersonalTaxAccountModel> ConfirmTaxFiledCommand { get; private set; }
        public DelegateCommand<PersonalTaxAccount> OpenPersonalTaxAccountDetailsDialogCommand { get; private set; }
        public DelegateCommand<Owner> NavigateToOwnerOverviewCommand { get; private set; }
        public DelegateCommand NavigateToPersonalTaxAccountHistoryCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IGlobalService _globalService;

        private readonly IBusinessOwnerService _businessOwnerService;
        private readonly ITaxAccountService _taxAccountService;
        private readonly IEmailService _emailService;
        private readonly IPersonalTaxFilingHandler _taxFilingHandler;

        public PersonalTaxAccountOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager,
            IGlobalService globalService, IBusinessOwnerService businessOwnerService, ITaxAccountService taxAccountService,
            IEmailService emailService, IPersonalTaxFilingHandler taxFilingHandler)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));

            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));
            _taxAccountService = taxAccountService ?? throw new ArgumentNullException(nameof(taxAccountService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _taxFilingHandler = taxFilingHandler ?? throw new ArgumentNullException(nameof(taxFilingHandler));

            Initialize();
        }

        private void Initialize()
        {
            RefreshPageCommand = new DelegateCommand(RefreshPage);
            UpdateStep1StatusCommand = new DelegateCommand<PersonalTaxAccount>(UpdateStep1Status);
            UpdateStep2StatusCommand = new DelegateCommand<PersonalTaxAccount>(UpdateStep2Status);
            UpdateStep3StatusCommand = new DelegateCommand<PersonalTaxAccount>(UpdateStep3Status);
            UpdateStep4StatusCommand = new DelegateCommand<PersonalTaxAccount>(UpdateStep4Status);
            UpdateStep5StatusCommand = new DelegateCommand<PersonalTaxAccount>(UpdateStep5Status);
            ConfirmTaxFiledCommand = new DelegateCommand<OwnerPersonalTaxAccountModel>(ConfirmTaxFiled);

            OpenPersonalTaxAccountDetailsDialogCommand = new DelegateCommand<PersonalTaxAccount>(OpenPersonalTaxAccountDetailsDialog);
            NavigateToOwnerOverviewCommand = new DelegateCommand<Owner>(NavigateToOwnerOverview);
            NavigateToPersonalTaxAccountHistoryCommand = new DelegateCommand(NavigateToPersonalTaxAccountHistory);

            LoadAccounts();

            CollectionViewSource.Filter += (s, e) =>
            {
                if (!(e.Item is OwnerPersonalTaxAccountModel model))
                {
                    e.Accepted = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(OwnerFilterText) == false)
                {
                    if (FilterHelper.StringContainsFilterText(model.Owner.Name, OwnerFilterText) == false
                        && FilterHelper.StringContainsFilterText(model.Owner.SIN, OwnerFilterText) == false
                        && FilterHelper.StringContainsFilterText(model.Owner.PhoneNumber, OwnerFilterText) == false
                        && FilterHelper.StringContainsFilterText(model.PersonalTaxAccount.Notes, OwnerFilterText) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(TaxYearFilter) == false)
                {
                    if (FilterHelper.StringContainsFilterText(model.PersonalTaxAccount.TaxYear, TaxYearFilter) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                //if (T1ProgressFilter != T1Progress.All)
                //{
                //    if ((T1ProgressFilter == T1Progress.InProgress && model.PersonalTaxAccount.TaxFilingProgress == 0)
                //        || (T1ProgressFilter == T1Progress.NotSent && model.PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step2))
                //        || (T1ProgressFilter == T1Progress.NotSigned && (model.PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step5 || model.PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step2) == false)))
                //        || (T1ProgressFilter == T1Progress.NotPaid && model.PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step3))
                //        || (T1ProgressFilter == T1Progress.NotSubmitted && model.PersonalTaxAccount.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step4))
                //        || (T1ProgressFilter == T1Progress.Done && (byte)model.PersonalTaxAccount.TaxFilingProgress < 30)
                //        )
                //    {
                //        e.Accepted = false;
                //        return;
                //    }
                //}

                if (T1ProgressFilter != T1Progress.All)
                {
                    if (T1ProgressFilter == T1Progress.NotSent && SignatureNotSent(model.PersonalTaxAccount.TaxFilingProgress)
                        || T1ProgressFilter == T1Progress.NotSigned && SignatureSentButNotSigned(model.PersonalTaxAccount.TaxFilingProgress)
                        || T1ProgressFilter == T1Progress.NotPaid && SignatureSignedButNotPaid(model.PersonalTaxAccount.TaxFilingProgress)
                        || T1ProgressFilter == T1Progress.NotSubmitted && PaidButNotSubmitted(model.PersonalTaxAccount.TaxFilingProgress)
                        || T1ProgressFilter == T1Progress.InProgress && (byte)model.PersonalTaxAccount.TaxFilingProgress > 0
                        )
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            SortByOwnerName();
        }

        private bool SignatureNotSent(PersonalTaxFilingProgress progress)
        {
            return progress.HasFlag(PersonalTaxFilingProgress.Step2) == false;
        }

        private bool SignatureSentButNotSigned(PersonalTaxFilingProgress progress)
        {
            return progress.HasFlag(PersonalTaxFilingProgress.Step2) 
                && progress.HasFlag(PersonalTaxFilingProgress.Step5) == false;
        }

        private bool SignatureSignedButNotPaid(PersonalTaxFilingProgress progress)
        {
            return progress.HasFlag(PersonalTaxFilingProgress.Step5)
                && progress.HasFlag(PersonalTaxFilingProgress.Step3) == false;
        }

        private bool PaidButNotSubmitted(PersonalTaxFilingProgress progress)
        {
            return progress.HasFlag(PersonalTaxFilingProgress.Step3)
                && progress.HasFlag(PersonalTaxFilingProgress.Step4) == false;
        }


        private void LoadAccounts()
        {
            var taxType = PersonalTaxType.T1;
            var taxAccounts = _taxAccountService.GetPersonalTaxAccountsByType(taxType);

            if (taxAccounts.Count > 0)
            {
                PersonalTaxAccountModels = new ObservableCollection<OwnerPersonalTaxAccountModel>(
                    taxAccounts.Select(x => new OwnerPersonalTaxAccountModel(x)));
            }
            else
            {
                PersonalTaxAccountModels = new ObservableCollection<OwnerPersonalTaxAccountModel>();
            }

            CollectionViewSource.Source = PersonalTaxAccountModels;
        }

        private void UpdateStep1Status(PersonalTaxAccount account)
        {
            if (VerifyOwnerEmailInfos(account, out Owner owner) == false)
            {
                return;
            }

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "{OwnerName}", owner.Name },
                    { "{OwnerAddress}", owner.Address },
                    { "{OwnerPhone}", owner.PhoneNumber },
                    { "{OwnerEmail}", owner.Email },
                };

                _emailService.SendPersonalContactConfirmationEmail(parameters, owner.Email);

                // Update UI
                account.Step1Completed = true;
                account.TaxFilingProgress |= PersonalTaxFilingProgress.Step1;

                _taxFilingHandler.UpdateStep1Status(account);

                _dialogService.ShowInformation("Email Sent", "Personal contact confirmation email sent!");

                PersonalTaxAccountsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR updating T1 progress, step: Sending Email", ex.Message);
            }
        }

        // Sent flag
        private void UpdateStep2Status(PersonalTaxAccount account)
        {
            try
            {
                if (account.Step2Completed)
                {
                    account.Step2Completed = false;
                    account.TaxFilingProgress = account.TaxFilingProgress & ~PersonalTaxFilingProgress.Step2;
                }
                else
                {
                    account.Step2Completed = true;
                    account.TaxFilingProgress = account.TaxFilingProgress | PersonalTaxFilingProgress.Step2;
                }

                _taxFilingHandler.UpdateStep2Status(account);

                PersonalTaxAccountsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR updating T1 progress, step: Signature - Sent", ex.Message);
            }
        }

        // Paid flag
        private void UpdateStep3Status(PersonalTaxAccount account)
        {
            try
            {
                if (account.Step3Completed)
                {
                    account.Step3Completed = false;
                    account.TaxFilingProgress = account.TaxFilingProgress & ~PersonalTaxFilingProgress.Step3;
                }
                else
                {
                    account.Step3Completed = true;
                    account.TaxFilingProgress = account.TaxFilingProgress | PersonalTaxFilingProgress.Step3;
                }

                _taxFilingHandler.UpdateStep3Status(account);

                PersonalTaxAccountsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR updating T1 progress, step: Payment", ex.Message);
            }
        }

        // Filed/Submitted flag
        private void UpdateStep4Status(PersonalTaxAccount account)
        {
            try
            {
                if (account.Step4Completed)
                {
                    account.Step4Completed = false;
                    account.TaxFilingProgress = account.TaxFilingProgress & ~PersonalTaxFilingProgress.Step4;
                }
                else
                {
                    account.Step4Completed = true;
                    account.TaxFilingProgress = account.TaxFilingProgress | PersonalTaxFilingProgress.Step4;
                }

                _taxFilingHandler.UpdateStep4Status(account);

                PersonalTaxAccountsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR updating T1 progress, step: Tax Filed", ex.Message);
            }
        }

        // Signed flag
        private void UpdateStep5Status(PersonalTaxAccount account)
        {
            try
            {
                if (account.TaxFilingProgress.HasFlag(PersonalTaxFilingProgress.Step5))
                {
                    account.TaxFilingProgress = account.TaxFilingProgress & ~PersonalTaxFilingProgress.Step5;
                }
                else
                {
                    account.TaxFilingProgress = account.TaxFilingProgress | PersonalTaxFilingProgress.Step5;
                }

                _taxFilingHandler.UpdateTaxFilingProgress(account);

                PersonalTaxAccountsView.Refresh();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR updating T1 progress, step: Signature - Signed", ex.Message);
            }
        }


        private void ConfirmTaxFiled(OwnerPersonalTaxAccountModel model)
        {
            if (model.PersonalTaxAccount.TaxFilingProgress.HasFlag(
                PersonalTaxFilingProgress.Step2 | PersonalTaxFilingProgress.Step3 | PersonalTaxFilingProgress.Step4) == false)
            {
                _dialogService.ShowInformation("Invalid action", "Must complete previous steps before confirming tax filed.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmText))
            {
                _dialogService.ShowInformation("Invalid action", "Confirmation Text must not be blank."); 
                return;
            }

            var currentUserId = _globalService.CurrentSession.UserAccountId;
            var confirmDate = DateTime.Now;

            try
            {
                _taxFilingHandler.ConfirmTaxFiled(model.PersonalTaxAccount, model.ConfirmText, confirmDate, currentUserId);

                model.ConfirmText = string.Empty;

                var oldRecord = PersonalTaxAccountModels.FirstOrDefault(x => x.PersonalTaxAccount.Id == model.PersonalTaxAccount.Id);
                var updated = _taxAccountService.GetPersonalTaxAccountById(model.PersonalTaxAccount.Id);

                if (oldRecord != null && updated != null)
                {
                    oldRecord.PersonalTaxAccount = updated;

                    PersonalTaxAccountsView.Refresh();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error: Failed to Confirm Personal Tax Filed", ex.Message);
            }
        }

        private void SendTaxFiledConfirmationEmail(OwnerPersonalTaxAccountModel model)
        {
            if (VerifyOwnerEmailInfos(model.PersonalTaxAccount, out Owner owner) == false)
            {
                return;
            }

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "{OwnerName}", owner.Name },
                    { "{OwnerAddress}", owner.Address },
                    { "{OwnerPhone}", owner.PhoneNumber },
                    { "{OwnerEmail}", owner.Email },
                    { "{ConfirmationText}", model.ConfirmText },
                };

                if (model.PersonalTaxAccount.TaxType == PersonalTaxType.T1)
                {
                    _emailService.SendT1ConfirmationEmail(parameters, owner.Email);

                    _dialogService.ShowInformation("Email Sent", "T1 Confirmation email has been sent!");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "ERROR Sending Email", ex.Message);
            }
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

        private void NavigateToPersonalTaxAccountHistory()
        {
            var navParams = new NavigationParameters($"");

            _regionManager.RequestNavigate(RegionNames.MainViewRegion, ViewRegKeys.PersonalTaxAccountHistory, navParams);
        }

        private void OpenPersonalTaxAccountDetailsDialog(PersonalTaxAccount account)
        {
            if (account == null)
            {
                return;
            }

            // TODO: implement details form
        }

        private void RefreshPage()
        {
            LoadAccounts();

            // TODO: Can I skip this step?
            RaisePropertyChanged("PersonalTaxAccountsView");

            // TODO: Or this step?
            PersonalTaxAccountsView.Refresh();
        }

        private void SortByOwnerName()
        {
            CollectionViewSource.SortDescriptions.Clear();
            CollectionViewSource.SortDescriptions.AddRange(new[]
            {
                new SortDescription("PersonalTaxAccount.IsHighPriority", ListSortDirection.Descending),
                new SortDescription("Owner.Name", ListSortDirection.Ascending),
            });
        }

        private bool VerifyOwnerEmailInfos(PersonalTaxAccount account, out Owner owner)
        {
            owner = _businessOwnerService.GetOwnerById(account.OwnerId);
            if (owner == null)
            {
                _dialogService.ShowError(new Core.Exceptions.OwnerNotFoundException(account.OwnerId),
                    "Owner Not Found", $"Owner account for Personal Tax Account [{account.Id}] not found");

                return false;
            }

            if (string.IsNullOrWhiteSpace(owner.Email) || string.IsNullOrWhiteSpace(owner.Name))
            {
                _dialogService.ShowInformation("Email Infos Not Setup Yet", "Please setup Owner Email and try again.");

                return false;
            }

            return true;
        }
    }

    public enum T1Progress
    {
        All = 0,
        InProgress = 1,
        NotSent = 2,
        NotSigned = 3,
        NotPaid = 4,
        NotSubmitted = 5,
        Done = 10,
    }
}
