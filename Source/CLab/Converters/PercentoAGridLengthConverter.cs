using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CLab.Converters
{
    /// <summary>
    /// Converte una frazione 0..1 in una GridLength proporzionale (star):
    /// usato dalla barra di avanzamento segmentata della Home (FASE 4).
    /// </summary>
    public class PercentoAGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = 0;
            if (value is double d) v = d;
            else if (value is int i) v = i;

            if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
            v = Math.Max(0, Math.Min(1, v));

            return new GridLength(v, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
