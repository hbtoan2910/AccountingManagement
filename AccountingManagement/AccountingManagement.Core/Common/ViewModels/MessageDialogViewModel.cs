using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountingManagement.Core.Common.ViewModels
{
    public class MessageDialogViewModel : BindableBase, IDialogAware
    {
        private string _dialogMessage;
        public string DialogMessage
        {
            get { return _dialogMessage; }
            set { SetProperty(ref _dialogMessage, value); }
        }

        public event Action<IDialogResult> RequestClose;

        public DelegateCommand CloseDialogCommand { get; private set; }

        public string Title => string.Empty;

        public MessageDialogViewModel()
        {
            CloseDialogCommand = new DelegateCommand(CloseDialog);
        }

        private void CloseDialog()
        {
            var result = ButtonResult.OK;

            var p = new DialogParameters
            {
                { "myParam", "The dialog was closed by user" }
            };

            RequestClose?.Invoke(new DialogResult(result, p));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            DialogMessage = parameters.GetValue<string>("message");
        }
    }
}
