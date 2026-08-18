using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CLab.Models;

namespace CLab.Converters
{
    /// <summary>
    /// Colore del testo/pallino del badge di stato cliente.
    /// Attinge dai BrushStato* del tema (Colors.xaml): un solo punto
    /// dove i colori di stato sono definiti per tutta l'app.
    /// </summary>
    public class StatoBadgeForeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var chiave = value switch
            {
                StatoCliente.Attivo => "BrushStatoAttivo",
                StatoCliente.StandBy => "BrushStatoStandBy",
                StatoCliente.Cessato => "BrushStatoCessato",
                _ => null
            };

            return chiave is not null
                ? Application.Current.Resources[chiave] as Brush ?? Brushes.Gray
                : Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
