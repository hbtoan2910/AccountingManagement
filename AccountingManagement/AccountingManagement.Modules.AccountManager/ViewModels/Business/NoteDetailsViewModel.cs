using System;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class NoteDetailsViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings and Commands
        private bool _isNew = false;
        public bool IsNew
        { 
            get { return _isNew; } 
            set { SetProperty(ref _isNew, value); }
        }

        private Note _note;
        public Note Note
        {
            get { return _note; }
            set { SetProperty(ref _note, value); }
        }

        public DelegateCommand SaveNoteCommand { get; private set; }
        public DelegateCommand CloseDialogCommand { get; private set; }
        #endregion

        private readonly IGlobalService _globalService;
        private readonly IBusinessOwnerService _businessOwnerService;

        public NoteDetailsViewModel(IGlobalService globalService, IBusinessOwnerService businessOwnerService)
        {
            _globalService = globalService ?? throw new ArgumentNullException(nameof(globalService));
            _businessOwnerService = businessOwnerService ?? throw new ArgumentNullException(nameof(businessOwnerService));

            Initialize();
        }

        private void Initialize()
        {
            SaveNoteCommand = new DelegateCommand(SaveNote);
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        private void SaveNote()
        {
            var user = _globalService.CurrentSession.UserDisplayName;
            Note.LastUpdated = $"Last updated by {user} on {DateTime.Now:MM-dd-yyyy HH:mm:ss}";

            if (_businessOwnerService.UpsertNote(Note))
            {
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (int.TryParse(parameters.GetValue<string>("NoteId"), out int noteId))
            {
                Note = _businessOwnerService.GetNoteById(noteId);
            }
            else
            {
                IsNew = true;

                var businessId = Guid.Parse(parameters.GetValue<string>("BusinessId"));
                Note = new Note { BusinessId = businessId };
            }
        }

        public event Action<IDialogResult> RequestClose;

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Nothing to do, for now
        }
        #endregion
    }
}
