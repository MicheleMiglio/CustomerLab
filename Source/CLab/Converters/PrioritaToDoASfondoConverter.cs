using CLab.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    /// <summary>Priorità del ToDo → sfondo tenue della pill (stessa logica di PrioritaPromemoriaASfondoConverter).</summary>
    public class PrioritaToDoASfondoConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value switch
            {
                PrioritaToDo.Alta => "BrushDangerLight",
                PrioritaToDo.Media => "BrushAccentSelected",
                PrioritaToDo.Bassa => "BrushInfoLight",
                _ => "BrushSfondoChiaro"
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
