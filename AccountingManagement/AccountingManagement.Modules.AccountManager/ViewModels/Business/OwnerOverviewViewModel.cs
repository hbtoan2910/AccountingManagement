using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;
using Microsoft.Win32;
using System.Linq;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class OwnerOverviewViewModel : ViewModelBase, INavigationAware
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView OwnerView => CollectionViewSource.View;

        private ObservableCollection<Owner> _ownerList;
        public ObservableCollection<Owner> OwnerList
        {
            get { return _ownerList; }
            set { SetProperty(ref _ownerList, value); }
        }

        private string _ownerFilterText;
        public string OwnerFilterText
        {
            get { return _ownerFilterText; }
            set
            {
                if (SetProperty(ref _ownerFilterText, value))
                {
                    OwnerView.Refresh();
                    RaisePropertyChanged(nameof(FilteredItemCount));
                }
            }
        }

        private bool _filterHasT1;
        public bool FilterHasT1
        {
            get { return _filterHasT1; }
            set
            {
                if (SetProperty(ref _filterHasT1, value))
                {
                    OwnerView.Refresh();
                    RaisePropertyChanged(nameof(FilteredItemCount));
                }
            }
        }

        private bool _filterInactiveT1;
        public bool FilterInactiveT1
        {
            get { return _filterInactiveT1; }
            set
            {
                if (SetProperty(ref _filterInactiveT1, value))
                {
                    OwnerView.Refresh();
                    RaisePropertyChanged(nameof(FilteredItemCount));
                }
            }
        }

        private bool _filterNoT1;
        public bool FilterNoT1
        {
            get { return _filterNoT1; }
            set
            {
                if (SetProperty(ref _filterNoT1, value))
                {
                    OwnerView.Refresh();
                    RaisePropertyChanged(nameof(FilteredItemCount));
                }
            }
        }


        private bool _includeDeletedOwner;
        public bool IncludeDeletedOwner
        {
            get { return _includeDeletedOwner; }
            set
            {
                if (SetProperty(ref _includeDeletedOwner, value))
                {
                    OwnerView.Refresh();
                    RaisePropertyChanged(nameof(FilteredItemCount));
                }
            }
        }

        public int FilteredItemCount
        {
            get { return OwnerView.Cast<object>()?.Count() ?? 0; }
        }

        public bool IsUploadOwnerListEnabled => _globalService.CurrentSession.Role == Core.Authentication.AccountRole.Administator;

        public DelegateCommand RefreshViewCommand { get; private set; }
        public DelegateCommand OpenNewOwnerDialogCommand { get; private set; }
        public DelegateCommand<Owner> OpenOwnerDetailsDialogCommand { get; private set; }
        public DelegateCommand<Owner> DeleteOwnerCommand { get; private set; }
        public DelegateCommand<Owner> UndoDeleteOwnerCommand { get; private set; }
        public DelegateCommand UploadOwnerFromFileCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEntityParser _entityParser;
        private readonly IGlobalService _globalService;

        private readonly IBusinessOwnerService _businessOwnerService;

        public OwnerOverviewViewModel(IDialogService dialogService, IEntityParser entityParser, IGlobalService globalService,
            IBusinessOwnerService businessOwnerService)
        {
            _dialogService = dialogService;
            _entityParser = entityParser;
            _globalService = globalService;

            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));

            Initialize();
        }

        private void Initialize()
        {
            RefreshViewCommand = new DelegateCommand(RefreshView);
            OpenNewOwnerDialogCommand = new DelegateCommand(OpenNewOwnerDialog);
            OpenOwnerDetailsDialogCommand = new DelegateCommand<Owner>(OpenOwnerDetailsDialog);
            DeleteOwnerCommand = new DelegateCommand<Owner>(DeleteOwner);
            UndoDeleteOwnerCommand = new DelegateCommand<Owner>(UndoDeleteOwner);

            UploadOwnerFromFileCommand = new DelegateCommand(UploadOwnerFromFile);

            OwnerList = new ObservableCollection<Owner>(_businessOwnerService.GetOwners());

            CollectionViewSource.Source = OwnerList;
            CollectionViewSource.Filter += (s, e) =>
            {
                if ((e.Item is Owner owner) == false)
                {
                    e.Accepted = false;
                    return;
                }
                //RYAN: this step filters active owners only (IsDeleted=0), which make CollectionViewSource & OwnerView different
                if (IncludeDeletedOwner != owner.IsDeleted) 
                {
                    e.Accepted = false;
                    return;
                }
                
                /** if (FilterHasT1 && (owner.T1Account == null || owner.T1Account.IsActive == false))
                {
                    e.Accepted = false;
                    return;
                } **/

                if (FilterNoT1 && owner.T1Account != null)
                {
                    e.Accepted = false;
                    return;
                }

                if (FilterInactiveT1 && (owner.T1Account == null || owner.T1Account.IsActive))
                {
                    e.Accepted = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(OwnerFilterText) == false)
                {
                    if (StringContainsFilterText(owner.Name, OwnerFilterText) == false)
                    {
                        e.Accepted = false;
                        return;
                    }
                }

                e.Accepted = true;
            };

            CollectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void RefreshView()
        {
            OwnerList = new ObservableCollection<Owner>(_businessOwnerService.GetOwners());

            CollectionViewSource.Source = OwnerList;
            RaisePropertyChanged("OwnerView");
        }

        private void OpenNewOwnerDialog()
        {
            _dialogService.ShowDialog(nameof(Views.OwnerDetails), new DialogParameters(), p => 
            {
                if (p.Result == ButtonResult.OK)
                {
                    RefreshView();
                }
            });
        }

        private void OpenOwnerDetailsDialog(Owner owner)
        {
            if (owner == null)
            {
                return;
            }

            var parameters = new DialogParameters($"OwnerId={owner.Id}");

            _dialogService.ShowDialog(nameof(Views.OwnerDetails), parameters, p =>
            {
                if (p.Result == ButtonResult.OK)
                {
                    RefreshView();
                }
            });
        }

        private void DeleteOwner(Owner owner)
        {
            if (owner == null)
            {
                return;
            }

            try
            {
                if (_dialogService.ShowConfirmation("Confirm Deactivate Owner", "Do you really want to deactivate this Owner?"))
                {
                    _businessOwnerService.DeleteOwner(owner.Id);

                    //owner.IsDeleted = true;
                    //OwnerView.Refresh();

                    RefreshView();
                    RaisePropertyChanged("OwnerView");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, $"ERROR deactivating Owner:[{owner.Id}|{owner.Name}]", $"{ex.Message}");
            }
        }

        private void UndoDeleteOwner(Owner owner)
        {
            if (owner == null)
            {
                return;
            }

            try
            {
                if (_dialogService.ShowConfirmation("Confirm Re-Activate Owner", "Do you really want to re-activate this Owner?"))
                {
                    _businessOwnerService.UndoDeleteOwner(owner.Id);

                    //owner.IsDeleted = false;
                    //OwnerView.Refresh();
                    RefreshView();
                    RaisePropertyChanged("OwnerView");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, $"ERROR re-activating Owner:[{owner.Id}|{owner.Name}]", $"{ex.Message}");
            }
        }

        private bool StringContainsFilterText(string searchString, string filterText)
        {
            return string.IsNullOrWhiteSpace(searchString) == false
                   && searchString.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }

        private void UploadOwnerFromFile()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                var fileName = dialog.FileName;

                try
                {
                    if (_entityParser.TryParseOwner(fileName, out List<Owner> owners))
                    {
                        if (owners.Count > 0)
                        {
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError(ex, "ERROR parsing owner file", ex.Message);
                }
            }
        }

        #region INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("OwnerName"))
            {
                var ownerName = navigationContext.Parameters.GetValue<string>("OwnerName");

                OwnerFilterText = ownerName;
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // do nothing
        }
        #endregion
    }
}
