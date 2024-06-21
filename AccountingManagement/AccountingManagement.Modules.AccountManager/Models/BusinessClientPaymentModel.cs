using System;
using System.Windows.Media;
using AccountingManagement.Core.Mvvm;
using AccountingManagement.DataAccess.Entities;
using AccountingManagement.Services;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class BusinessClientPaymentModel : ViewModelBase
    {
        public Business Business { get; set; }

        public ClientPayment ClientPayment { get; set; }

        private string _confirmText;
        public string ConfirmText
        {
            get { return _confirmText; }
            set
            {
                if (SetProperty(ref _confirmText, value))
                {
                    if (string.IsNullOrEmpty(_confirmText) == false)
                    {
                        _paymentHandler?.SaveConfirmationText(ClientPayment, _confirmText);
                    }
                }
            }
        }

        public bool IsSelectedForExport { get; set; }

        public bool IsSelectedForExportEnabled => ClientPayment.TmpReceiptEmailSent;

        public SolidColorBrush ReceiptSentIndicatorColor => ClientPayment.TmpReceiptEmailSent
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.LightGray);

        private readonly IPaymentHandler _paymentHandler;

        public BusinessClientPaymentModel(ClientPayment clientPayment, IPaymentHandler paymentHandler)
        {
            ClientPayment = clientPayment;
            Business = clientPayment.Business;
            ConfirmText = clientPayment.TmpConfirmationText;
            IsSelectedForExport = false;

            _paymentHandler = paymentHandler ?? throw new ArgumentNullException(nameof(paymentHandler)); ;
        }
    }
}
