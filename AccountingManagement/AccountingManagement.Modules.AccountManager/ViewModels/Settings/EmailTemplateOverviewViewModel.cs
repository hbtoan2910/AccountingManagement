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
using AccountingManagement.Modules.AccountManager.Utilities;
using AccountingManagement.Services;
using AccountingManagement.Services.Email;
using Serilog;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class EmailTemplateOverviewViewModel : ViewModelBase
    {
        #region Bindings & Commands
        public CollectionViewSource CollectionViewSource = new CollectionViewSource();
        public ICollectionView EmailTemplatesView => CollectionViewSource.View;

        private ObservableCollection<EmailTemplate> _emailTemplates;
        public ObservableCollection<EmailTemplate> EmailTemplates
        {
            get { return _emailTemplates; }
            set { SetProperty(ref _emailTemplates, value); }
        }

        private List<EmailSender> _emailSenderList;
        public List<EmailSender> EmailSenderList
        {
            get { return _emailSenderList; }
            set { SetProperty(ref _emailSenderList, value); }
        }

        private EmailTemplate _selectedEmailTemplate;
        public EmailTemplate SelectedEmailTemplate
        {
            get { return _selectedEmailTemplate; }
            set { SetProperty(ref _selectedEmailTemplate, value); }
        }

        public DelegateCommand<EmailTemplate> SaveEmailTemplateCommand { get; private set; }
        public DelegateCommand RefreshPageCommand { get; private set; }
        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IGlobalService _globalService;
        private readonly IEmailSenderQueryService _emailSenderQueryService;
        private readonly IEmailTemplateQueryService _emailTemplateRepository;

        public EmailTemplateOverviewViewModel(IDialogService dialogService, IEventAggregator eventAggregator,
            IGlobalService globalService, IEmailSenderQueryService emailSenderQueryService,
            IEmailTemplateQueryService emailTemplateQueryService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _globalService = globalService;
            _emailSenderQueryService = emailSenderQueryService ?? throw new ArgumentNullException(nameof(emailSenderQueryService));
            _emailTemplateRepository = emailTemplateQueryService ?? throw new ArgumentNullException(nameof(emailTemplateQueryService));

            Initialize();
        }

        private void Initialize()
        {
            SaveEmailTemplateCommand = new DelegateCommand<EmailTemplate>(SaveEmailTemplate);
            RefreshPageCommand = new DelegateCommand(ReloadEmailTemplates);

            ReloadEmailTemplates();

            CollectionViewSource.Filter += (s, e) =>
            {
            
            };

            CollectionViewSource.SortDescriptions.Add(new SortDescription("Template", ListSortDirection.Ascending));
        }

        private void ReloadEmailTemplates()
        {
            EmailSenderList = _emailSenderQueryService.GetEmailSenders();

            EmailTemplates = new ObservableCollection<EmailTemplate>(_emailTemplateRepository.GetEmailTemplates());
            CollectionViewSource.Source = EmailTemplates;
        }

        private void SaveEmailTemplate(EmailTemplate template)
        {
            try
            {
                var name = _globalService.CurrentSession.UserDisplayName;

                template.LastUpdated = DateTime.Now;
                template.LastUpdatedBy = $"{name} at {template.LastUpdated:MM-dd-yyyy HH:mm:ss}";

                _emailTemplateRepository.UpsertEmailTemplate(template);

                RaisePropertyChanged("SelectedEmailTemplate");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex, "Error saving Email Template", $"{ex.Message}");
            }
        }
    }
}
