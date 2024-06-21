using System;

namespace AccountingManagement.Core.Exceptions
{
    public class OwnerNotFoundException : Exception
    {
        public OwnerNotFoundException(Guid ownerId)
            : base($"OwnerId:{ownerId} not found")
        { }
    }
}
