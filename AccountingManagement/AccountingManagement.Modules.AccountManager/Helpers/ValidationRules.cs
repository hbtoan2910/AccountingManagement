using System;
using System.Windows.Controls;
using System.Globalization;

namespace AccountingManagement.Modules.AccountManager.Helpers
{
    public class PhoneNumberRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var phoneNumberText = (string)value;
            if (phoneNumberText.Length > 10)
            {
                return new ValidationResult(false, "Cannot exceeds 10 characters.");
            }

            if (Int32.TryParse((string)value, out int _) == false)
            {
                return new ValidationResult(false, "Not a numeric value.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
