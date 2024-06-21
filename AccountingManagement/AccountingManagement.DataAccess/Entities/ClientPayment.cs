using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class ClientPayment
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public ClientPaymentType PaymentType { get; set; }

        public ClientPaymentCycle PaymentCycle { get; set; }

        public decimal PaymentAmount { get; set; }

        public DateTime DueDate { get; set; }

        public string BankInfo { get; set; }

        public string Notes { get; set; }

        /// <summary>
        /// Re-purpose TmpConfirmationText to ProgressNote
        /// </summary>
        public string TmpConfirmationText { get; set; }

        public bool TmpReceiptEmailSent { get; set; }

        public bool IsActive { get; set; }

        public ClientPayment()
        {
            IsActive = true;
        }
    }
}
