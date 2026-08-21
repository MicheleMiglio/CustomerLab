using CLab.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    /// <summary>Priorità del ToDo → colore testo/bordo della pill (stessa logica di PrioritaPromemoriaAColoreConverter).</summary>
    public class PrioritaToDoAColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value switch
            {
                PrioritaToDo.Alta => "BrushDanger",
                PrioritaToDo.Media => "BrushAccent",
                PrioritaToDo.Bassa => "BrushPrimaryLight",
                _ => "BrushBordo"
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
