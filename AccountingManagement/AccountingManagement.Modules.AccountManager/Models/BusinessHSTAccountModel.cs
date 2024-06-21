using System;
using System.Windows;
using System.Windows.Media;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class BusinessHSTAccountModel
    {
        public static readonly int HSTDueDateWarningThreshold = 10;
        public static readonly int HSTEndingPeriodWarningThreshold = 15;

        public Business Business { get; set; }

        public HSTAccount HSTAccount { get; set; }

        public string HSTConfirmText { get; set; } = string.Empty;

        public string InstalmentConfirmText { get; set; } = string.Empty;

        public SolidColorBrush HSTDueDateForeground =>
            (HSTAccount.HSTDueDate - DateTime.Now).Days <= HSTDueDateWarningThreshold
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Black);

        public FontWeight HSTDueDateFontWeight =>
            (HSTAccount.HSTDueDate - DateTime.Now).Days <= HSTDueDateWarningThreshold
                ? FontWeights.Bold
                : FontWeights.Normal;

        public SolidColorBrush HSTEndingPeriodForeground =>
            (DateTime.Now - HSTAccount.HSTEndingPeriod).Days >= HSTEndingPeriodWarningThreshold
                ? new SolidColorBrush(Colors.Blue)
                : new SolidColorBrush(Colors.Black);

        public FontWeight HSTEndingPeriodFontWeight =>
            (DateTime.Now - HSTAccount.HSTEndingPeriod).Days >= HSTEndingPeriodWarningThreshold
                ? FontWeights.Bold
                : FontWeights.Normal;

        public BusinessHSTAccountModel(HSTAccount account)
        {
            HSTAccount = account;
            Business = account.Business;
        }
    }
}
