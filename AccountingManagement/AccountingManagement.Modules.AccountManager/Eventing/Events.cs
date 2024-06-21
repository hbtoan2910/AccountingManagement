using System;
using Prism.Events;

namespace AccountingManagement.Modules.AccountManager.Events
{
    public class BusinessUpsertedEvent : PubSubEvent<Guid>
    { }

    public class BusinessDeletedEvent : PubSubEvent<Guid>
    { }

    public class UserAccountUpsertedEvent : PubSubEvent<Guid>
    { }
}
