using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class ClientPaymentLog
    {
        public int Id { get; set; }

        public Guid ClientPaymentId { get; set; }

        public ClientPayment ClientPayment { get; set; }

        public Guid BusinessId { get; set; }

        public Business Business { get; set; }

        public decimal PaymentAmount { get; set; }

        public string BankInfo { get; set; }

        public DateTime DueDate { get; set; }

        public string ConfirmationNotes { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid UserAccountId { get; set; }

        public UserAccount UserAccount { get; set; }

        public ClientPaymentLog()
        { }
    }
}
