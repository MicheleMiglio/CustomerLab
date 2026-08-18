using CLab.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    /// <summary>Priorità del promemoria → colore del bordo/accento sinistro del post-it.</summary>
    public class PrioritaPromemoriaAColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value switch
            {
                PrioritaPromemoria.Alta => "BrushDanger",
                PrioritaPromemoria.Media => "BrushAccent",
                PrioritaPromemoria.Bassa => "BrushPrimaryLight",
                _ => "BrushBordo"
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}