using System;
using System.Globalization;
using System.Windows.Data;
using CLab.Models;

namespace CLab.Converters
{
    public class TipoCampoLabelConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                TipoCampoAttivita.SiNo => "Sì / No",
                TipoCampoAttivita.TestoLibero => "Testo libero",
                TipoCampoAttivita.Numero => "Numero",
                TipoCampoAttivita.Tendina => "Tendina",
                _ => value?.ToString() ?? ""
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}