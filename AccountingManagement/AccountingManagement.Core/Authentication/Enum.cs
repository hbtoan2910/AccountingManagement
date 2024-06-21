using System;

namespace AccountingManagement.Core.Authentication
{
    public enum LoginResultCode
    {
        Unknown = -1,
        Success,
        ChangePassword,
        UsernameOrPasswordEmpty,
        IncorrectUsernameOrPassword,
        AccountLocked,
        DatabaseUnreachable,
    }
}
