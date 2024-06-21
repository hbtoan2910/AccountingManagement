using System;

namespace AccountingManagement.Core.Authentication
{
    public class LoginResult
    {
        public LoginResultCode ResultCode { get; set; }
        public Guid UserAccountId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public byte Role { get; set; }

        public LoginResult(LoginResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public LoginResult(LoginResultCode resultCode, Guid userId, string username, string displayName, byte role)
        {
            ResultCode = resultCode;
            UserAccountId = userId;
            Username = username;
            DisplayName = displayName;
            Role = role;
        }
    }
}
