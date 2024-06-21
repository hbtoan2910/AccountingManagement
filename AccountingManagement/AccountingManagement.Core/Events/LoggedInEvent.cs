using System;
using Prism.Events;
using AccountingManagement.Core.Authentication;

namespace AccountingManagement.Core.Events
{
    public class LoggedInEvent : PubSubEvent<LoggedInEventArgs>
    { }

    public class LoggedInEventArgs
    {
        public LoginResult LoginResult { get; set; }

        public LoggedInEventArgs(LoginResult result)
        {
            LoginResult = result;
        }
    }
}
