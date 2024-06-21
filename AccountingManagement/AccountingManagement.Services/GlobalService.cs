using System;
using AccountingManagement.Core.Authentication;

namespace AccountingManagement.Services
{
    public interface IGlobalService
    {
        UserSession CurrentSession { get; }

        void SetCurrentSession(Guid accountId, string username, string displayName, byte role);
        void ValidateCurrentSession();
    }

    public class GlobalService : IGlobalService
    {
        public UserSession CurrentSession { get; private set; }

        public GlobalService()
        { }

        public void SetCurrentSession(Guid accountId, string username, string displayName, byte role)
        {
            CurrentSession = new UserSession(accountId, username, displayName, role)
            {
                LastLogin = DateTime.Now,
            };
        }

        public void ValidateCurrentSession()
        {
            if (CurrentSession == null)
            {
                // TODO: redirect to Login page
            }
        }
    }
}
