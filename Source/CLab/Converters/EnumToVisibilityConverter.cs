using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CLab.Converters
{
    /// <summary>
    /// Visibile solo se il valore legato coincide con il parametro passato
    /// (es. Visibility="{Binding FormTipoCampo, Converter={StaticResource EnumToVisibility}, ConverterParameter=Tendina}").
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}