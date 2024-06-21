using System;
using System.Collections.Generic;
using System.Text;

namespace AccountingManagement.Modules.AccountManager.Helpers
{
    public static class FilterHelper
    {
        public static bool StringContainsFilterText(string input, string filterText)
        {
            return string.IsNullOrWhiteSpace(input) == false && input.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
