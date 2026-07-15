using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CLab.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool risultato = value switch
            {
                bool b => b,
                int i => i > 0,
                string s => !string.IsNullOrEmpty(s),
                _ => value != null
            };

            if (parameter is string p && p == "Inverti")
                risultato = !risultato;

            return risultato ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
