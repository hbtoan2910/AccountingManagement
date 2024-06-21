using AccountingManagement.Modules.AccountManager.Views;
using Prism.Services.Dialogs;
using Serilog;
using System;

namespace AccountingManagement.Modules.AccountManager.Utilities
{
    public static class DialogServiceExtensions
    {
        public static void ShowError(this IDialogService dialogService, Exception exception, string header, string message)
        {
            Log.Error(exception, header + "\r\n" + message);

            dialogService.ShowDialog(nameof(SimpleErrorDialog),
                new DialogParameters($"DialogType=Error&Header={header}&Message={message}"),
                d => { new DialogResult(ButtonResult.OK); });
        }

        public static void ShowError(this IDialogService dialogService, Exception exception, string header, string message, Action<IDialogResult> callback)
        {
            Log.Error(exception, header + "\r\n" + message);

            dialogService.ShowDialog(nameof(SimpleErrorDialog),
                new DialogParameters($"DialogType=Error&Header={header}&Message={message}"), callback);
        }

        public static void ShowConfirmation(this IDialogService dialogService, string header, string message, Action<IDialogResult> callback)
        {
            dialogService.ShowDialog(nameof(SimpleInputDialog),
                new DialogParameters($"DialogType=Confirmation&Header={header}&Message={message}"), callback);
        }

        public static bool ShowConfirmation(this IDialogService dialogService, string header, string message)
        {
            ButtonResult dialogResult = ButtonResult.None;

            dialogService.ShowDialog(nameof(SimpleInputDialog), new DialogParameters($"DialogType=Confirmation&Header={header}&Message={message}"),
                (dResult) => 
                {
                    dialogResult = dResult.Result;
                });

            if (dialogResult == ButtonResult.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ShowInformation(this IDialogService dialogService, string header, string message)
        {
            dialogService.ShowDialog(nameof(SimpleInputDialog),
                new DialogParameters($"DialogType=Information&Header={header}&Message={message}"), 
                d => { new DialogResult(ButtonResult.OK); });
        }

        public static void PromptUserInput(this IDialogService dialogService, string header, string message, Action<IDialogResult> callback)
        {
            dialogService.ShowDialog(nameof(SimpleInputDialog),
                new DialogParameters($"DialogType=UserInput&Header={header}&Message={message}"), callback);
        }
    }

}
