using System;
using System.Globalization;
using System.Windows.Data;
using CLab.Models;

namespace CLab.Converters
{
    public class StatoBadgeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                StatoCliente.Attivo => "Attivo",
                StatoCliente.StandBy => "Stand by",
                StatoCliente.Cessato => "Cessato",
                _ => value?.ToString() ?? ""
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
