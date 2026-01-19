using System;
using System.Globalization;
using System.Windows.Data;

namespace Stratum.Desktop.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public static readonly StringToBoolConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}