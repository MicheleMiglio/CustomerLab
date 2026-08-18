using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CLab.Models;

namespace CLab.Converters
{
    /// <summary>
    /// Sfondo tenue del badge di stato cliente: stessa tinta di
    /// StatoBadgeForeConverter, solo più chiara (vedi *Light in Colors.xaml).
    /// </summary>
    public class StatoBadgeBackConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var chiave = value switch
            {
                StatoCliente.Attivo => "BrushStatoAttivoLight",
                StatoCliente.StandBy => "BrushStatoStandByLight",
                StatoCliente.Cessato => "BrushStatoCessatoLight",
                _ => null
            };

            return chiave is not null
                ? Application.Current.Resources[chiave] as Brush ?? Brushes.LightGray
                : Brushes.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
