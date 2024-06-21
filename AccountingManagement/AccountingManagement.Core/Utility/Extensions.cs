using System;
using System.Text;

namespace AccountingManagement.Core.Utility
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty(this Guid? id)
        {
            return id == null || id == Guid.Empty;
        }
    }
}
