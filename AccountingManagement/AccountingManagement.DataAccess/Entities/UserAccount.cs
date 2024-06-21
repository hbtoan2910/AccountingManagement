using System;

namespace AccountingManagement.DataAccess.Entities
{
    public class UserAccount
    {
        public Guid Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string DisplayName { get; set; }

        public string PhoneNumber { get; set; }

        public byte Role { get; set; }

        public byte FailedLoginAttempt { get; set; }

        public bool ChangePassword { get; set; }

        public bool Active { get; set; }

        public bool IsDeleted { get; set; }

        public UserAccount()
        {
            Id = Guid.NewGuid();
            Role = 0;
            FailedLoginAttempt = 0;
            ChangePassword = false;
            Active = true;
            IsDeleted = false;
        }
    }
}
