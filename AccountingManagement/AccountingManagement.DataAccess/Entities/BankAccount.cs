using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class BankAccount
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Website { get; set; }

        public string AccountUsername { get; set; }

        public string AccountPassword { get; set; }

        public string AccountNote { get; set; }

        public bool IsDeleted { get; set; }

        public BankAccount()
        {
            IsDeleted = false;
        }
    }
}
