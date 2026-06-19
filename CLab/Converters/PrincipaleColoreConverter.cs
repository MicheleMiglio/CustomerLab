using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    public class PrincipaleColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return Brushes.Gold;
            }
            return Brushes.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}