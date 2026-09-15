using System;
using System.Globalization;
using System.Windows.Data;

namespace CLab.Converters
{
    /// <summary>
    /// FASE 3 CLab 2.0: larghezza dinamica del side panel in base alla larghezza
    /// della finestra. 480 px con finestra ≥ 1600 px, 420 px sotto; in ogni caso
    /// la larghezza non supera lo spazio che lascia 560 px al contenuto principale
    /// (minimo assoluto 320 px) così il pannello non provoca mai clipping.
    /// </summary>
    public class LarghezzaSidePanelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double larghezzaFinestra = value is double d && d > 0 ? d : 1200;

            double target = larghezzaFinestra >= 1600 ? 480 : 420;
            double disponibile = Math.Max(320, larghezzaFinestra - 560);

            return Math.Min(target, disponibile);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
