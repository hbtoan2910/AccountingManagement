using System;

namespace AccountingManagement.Core.Exceptions
{
    public class BusinessNotFoundException : Exception
    {
        public BusinessNotFoundException(Guid businessId)
            : base($"BusinessId:{businessId} not found")
        { }
    }
}
