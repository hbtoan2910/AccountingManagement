using System;
using System.Windows;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class SimpleErrorDialogViewModel : ViewModelBase, IDialogAware
    {
        #region Bindings and Commands
        private string _headerText = "Confirm Action";
        public string HeaderText
        {
            get { return _headerText; }
            set { SetProperty(ref _headerText, value); }
        }

        private string _messageText = "Confirm action?";
        public string MessageText
        {
            get { return _messageText; }
            set { SetProperty(ref _messageText, value); }
        }

        public DelegateCommand ConfirmCommand { get; private set; }
        #endregion

        public SimpleErrorDialogViewModel()
        {
            ConfirmCommand = new DelegateCommand(Confirm);
        }

        private void Confirm()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        #region IDialogAware
        public string Title => "Error";

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
            // throw new NotImplementedException();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            HeaderText = parameters.GetValue<string>("Header");
            MessageText = parameters.GetValue<string>("Message");
        }
        #endregion
    }
}
