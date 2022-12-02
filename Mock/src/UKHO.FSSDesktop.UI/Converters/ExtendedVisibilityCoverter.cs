namespace UKHO.FSSDesktop.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class ExtendedBoolToVisibilityConverter : IValueConverter
    {
        public ExtendedBoolToVisibilityConverter()
        {
            // set defaults
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }

        public Visibility TrueValue { get; set; }

        public Visibility FalseValue { get; set; }

        public object? Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is not bool b)
            {
                return null;
            }

            return b ? TrueValue : FalseValue;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
            {
                return true;
            }

            if (Equals(value, FalseValue))
            {
                return false;
            }

            return null;
        }
    }
}