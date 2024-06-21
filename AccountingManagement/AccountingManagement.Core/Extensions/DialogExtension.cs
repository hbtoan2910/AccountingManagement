using System;
using Prism.Services.Dialogs;

namespace AccountingManagement.Core.Extensions
{
    public static class DialogExtension
    {
        public static void ShowNotification(this IDialogService dialogService, string message,
            Action<IDialogResult> callback)
        {
            var p = new DialogParameters
            {
                { "Message", message }
            };

            dialogService.ShowDialog(nameof(Core.Common.Views.MessageDialog), p, callback);
        }
    }
}
