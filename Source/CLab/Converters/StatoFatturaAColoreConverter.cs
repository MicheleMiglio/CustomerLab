using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    public class StatoFatturaAColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value?.ToString() switch
            {
                "Pagata" => "BrushSuccess",
                "Scaduta" => "BrushDanger",
                "Annullata" => "BrushTestoSecondario",
                _ => "BrushStatoStandBy" // Emessa
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}