using CLab.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    /// <summary>Priorità del promemoria → sfondo tenue della pill (wash semantico per priorità).</summary>
    public class PrioritaPromemoriaASfondoConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value switch
            {
                PrioritaPromemoria.Alta => "BrushDangerLight",
                PrioritaPromemoria.Media => "BrushAccentSelected",
                PrioritaPromemoria.Bassa => "BrushInfoLight",
                _ => "BrushSfondoChiaro"
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}