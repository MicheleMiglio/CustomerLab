using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    /// <summary>
    /// Colora la stella "principale" (telefono/email) con l'accent del tema
    /// quando è selezionata, con il bordo neutro quando non lo è.
    /// </summary>
    public class PrincipaleColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var chiave = value is bool b && b ? "BrushAccent" : "BrushBordo";
            return Application.Current.Resources[chiave] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
