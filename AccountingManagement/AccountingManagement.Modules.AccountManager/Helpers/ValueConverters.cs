using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Helpers
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value != null)
            {
                return Visibility.Visible;
            }

            return (parameter != null && parameter is string isCollapsed
                    && isCollapsed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        ? Visibility.Collapsed
                        : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value == null)
            {
                return Visibility.Visible;
            }

            return (parameter != null && parameter is string isCollapsed
                    && isCollapsed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        ? Visibility.Collapsed
                        : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToDashStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            return value ?? "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if ((bool)value == true)
            {
                return Visibility.Visible;
            }

            return (parameter != null && parameter is string isCollapsed
                    && isCollapsed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        ? Visibility.Collapsed
                        : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class NullableBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value != null && (bool)value == true)
            {
                return Visibility.Visible;
            }

            return (parameter != null && parameter is string isCollapsed
                    && isCollapsed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        ? Visibility.Collapsed
                        : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }



    public class ContentNotEmptyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            return !string.IsNullOrWhiteSpace((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (string.IsNullOrWhiteSpace((string)value) == false)
            {
                return Visibility.Visible;
            }

            return (parameter != null && parameter is string isCollapsed
                    && isCollapsed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        ? Visibility.Collapsed
                        : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class NullableDecimalToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value == null)
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if ((bool)value == false)
            {
                return Visibility.Visible;
            }

            return (parameter != null && parameter is string isCollapsed
                    && isCollapsed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        ? Visibility.Collapsed
                        : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class EmailReminderToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            var emailReminder = (EmailReminder)value;

            if (emailReminder == EmailReminder.None)
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value is bool emailReminderSent && emailReminderSent)
            {
                return EmailReminder.Sent;
            }

            return EmailReminder.None;
        }
    }

    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value == null)
            {
                return false;
            }

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(PayrollStatus), typeof(Brush))]
    public class PayrollStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            var status = (PayrollStatus)value;

            switch (status)
            {
                case PayrollStatus.None:
                case PayrollStatus.InProgress:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF8474"));
                // return new SolidColorBrush(Color.FromArgb(255, 144, 190, 109));
                // return new SolidColorBrush(Color.FromArgb(255, 240, 199, 79));

                case PayrollStatus.Done:
                    return new SolidColorBrush(Colors.LightGray);

                default:
                    return new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            return PayrollStatus.None;
        }
    }

    public class PayrollStatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            var isReverse = parameter is string parameterStr && parameterStr.Equals("Reverse", StringComparison.InvariantCultureIgnoreCase);

            var status = (PayrollStatus)value;

            if (isReverse)
            {
                return status switch
                {
                    PayrollStatus.Done => false,
                    _ => false,
                };
            }
            else
            {
                return status switch
                {
                    PayrollStatus.Done => true,
                    _ => false,
                };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            var isReverse = parameter is string parameterStr && parameterStr.Equals("Reverse", StringComparison.InvariantCultureIgnoreCase);
            var isOn = (bool)value;

            if (isOn == isReverse)
            {
                return PayrollStatus.None;
            }
            else
            {
                return PayrollStatus.Done;
            }

            /*if (isOn)
            {
                if (isReverse)
                {
                    return PayrollStatus.None;
                }
                else
                {
                    return PayrollStatus.Done;
                }
            }
            else
            {
                if (isReverse)
                {
                    return PayrollStatus.Done;
                }
                else
                {
                    return PayrollStatus.None;
                }
            }*/
        }
    }

    public class PayrollPeriodLookupToDisplayString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            if (value is PayrollPeriodLookup ppLookup && parameter != null && parameter is string payrollCycle)
            {
                return parameter switch
                {
                    "Monthly" => $"{ppLookup.MonthlyDueDate:MM-dd-yyyy}",

                    "SemiMonthly" => $"{ppLookup.SemiMonthlyDueDate1:MM-dd-yyyy}       {ppLookup.SemiMonthlyDueDate2:MM-dd-yyyy}",

                    "BiWeekly" => $"{ppLookup.BiWeeklyDueDate1:MM-dd-yyyy}       {ppLookup.BiWeeklyDueDate2:MM-dd-yyyy}       {ppLookup.BiWeeklyDueDate3:MM-dd-yyyy}",

                    _ => "N/A",
                };
            }
            else
            {
                return "N/A";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class PasswordBoxesToPasswordBoxListConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo ci)
        {
            return new PasswordBoxList 
            {
                OldPassword = values[0] as PasswordBox,
                NewPassword = values[1] as PasswordBox,
                ConfirmNewPassword = values[2] as PasswordBox,
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class RadioButtonToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    public class ComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }


    public class TaskStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo ci)
        {
            var status = (TaskStatus)value;

            switch (status)
            {
                case TaskStatus.New:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF8474"));

                case TaskStatus.InProgress:
                    // return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF77ACF1"));
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF58634"));

                case TaskStatus.Done:
                    return new SolidColorBrush(Colors.White);

                case TaskStatus.Closed:
                    return new SolidColorBrush(Colors.LightGray);

                default:
                    // return new SolidColorBrush(Colors.White);
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF8474"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo ci)
        {
            throw new NotImplementedException();
        }
    }

    public class PasswordBoxList
    {
        public PasswordBox OldPassword { get; set; }
        public PasswordBox NewPassword { get; set; }
        public PasswordBox ConfirmNewPassword { get; set; }

        public PasswordBoxList()
        { }
    }
}
