using System;

namespace AccountingManagement.Core.Authentication
{
    public class UserSession
    {
        public Guid UserAccountId { get; set; }
        public string Username { get; set; }
        public string UserDisplayName { get; set; }
        public AccountRole Role { get; set; }
        public DateTime LastLogin { get; set; }

        public UserSession(Guid accountId, string username, string displayName, byte role)
        {
            UserAccountId = accountId;
            Username = username;
            UserDisplayName = displayName;
            Role = (AccountRole)role;
        }
    }

    public enum AccountRole : byte
    {
        None = 0,
        Administator = 1,
        User = 9,
    }
}
