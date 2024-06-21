using System;
using System.Windows;
using Prism.Commands;
using Prism.Services.Dialogs;
using AccountingManagement.Core.Mvvm;

namespace AccountingManagement.Modules.AccountManager.ViewModels
{
    public class SimpleInputDialogViewModel : ViewModelBase, IDialogAware
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

        private string _userInput;
        public string UserInput
        {
            get { return _userInput; }
            set { SetProperty(ref _userInput, value); }
        }

        private Visibility _userInputVisibility = Visibility.Collapsed;
        public Visibility UserInputVisibility
        {
            get { return _userInputVisibility; }
            set { SetProperty(ref _userInputVisibility, value); }
        }

        private Visibility _cancelCommandVisibility = Visibility.Visible;
        public Visibility CancelCommandVisibility
        {
            get { return _cancelCommandVisibility; }
            set { SetProperty(ref _cancelCommandVisibility, value); }
        }

        private string _errorText;
        public string ErrorText
        {
            get { return _errorText; }
            set { SetProperty(ref _errorText, value); }
        }

        private Visibility _errorTextVisibility = Visibility.Collapsed;
        public Visibility ErrorTextVisibility
        {
            get { return _errorTextVisibility; }
            set { SetProperty(ref _errorTextVisibility, value); }
        }


        public DelegateCommand ConfirmCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }
        #endregion

        private DialogType _dialogType = DialogType.Undefined;

        public SimpleInputDialogViewModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            ConfirmCommand = new DelegateCommand(Confirm);
            CancelCommand = new DelegateCommand(Cancel);
        }

        private void Confirm()
        {
            switch (_dialogType)
            {
                case DialogType.Confirmation:
                    RaiseRequestClose(new DialogResult(ButtonResult.OK));
                    break;

                case DialogType.UserInput:
                    if (string.IsNullOrWhiteSpace(UserInput))
                    {
                        ErrorText = "Must provide a value";
                        ErrorTextVisibility = Visibility.Visible;
                        return;
                    }
                    var parameters = new DialogParameters($"UserInput={UserInput}");
                    RaiseRequestClose(new DialogResult(ButtonResult.OK, parameters));
                    break;

                case DialogType.Error:
                case DialogType.Information:
                default:
                    RaiseRequestClose(new DialogResult(ButtonResult.OK));
                    break;
            }
        }

        private void Cancel()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        #region IDialogAware
        public string Title => string.Empty;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (Enum.TryParse(parameters.GetValue<string>("DialogType"), out _dialogType))
            {
                switch (_dialogType)
                {
                    case DialogType.Confirmation:
                        break;

                    case DialogType.UserInput:
                        UserInputVisibility = Visibility.Visible;
                        break;

                    case DialogType.Information:
                        CancelCommandVisibility = Visibility.Collapsed;
                        break;

                    case DialogType.Error:
                        break;
                }

                HeaderText = parameters.GetValue<string>("Header");
                MessageText = parameters.GetValue<string>("Message");
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
            // throw new NotImplementedException();
        }
        #endregion
    }

    public enum DialogType
    {
        Undefined = -1,
        Confirmation = 0,
        UserInput = 1,
        Information = 2,
        Error = 10,
    }
}
