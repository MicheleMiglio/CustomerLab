using System;
using System.Globalization;
using System.Windows.Data;

namespace CLab.Converters
{
    /// <summary>
    /// PASS UI 2.0: larghezza responsive del ModalDialogControl in base alla
    /// larghezza della finestra. Dialog confortevole (600 px) su schermi
    /// normali/grandi, 540 px sotto 1500 px; in ogni caso non supera mai lo
    /// spazio che lascia un margine di 240 px complessivi dai bordi (minimo
    /// assoluto 360 px), così il dialog non esce mai dallo schermo.
    /// Con parameter "Altezza" restituisce l'altezza massima: 80% della
    /// finestra, per far scorrere internamente i form più lunghi.
    /// </summary>
    public class LarghezzaDialogConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double larghezzaFinestra = value is double d && d > 0 ? d : 1200;

            if (parameter is string p && p == "Altezza")
            {
                double altezzaFinestra = value is double h && h > 0 ? h : 800;
                return Math.Max(360, altezzaFinestra * 0.80);
            }

            double target = larghezzaFinestra >= 1500 ? 600 : 540;
            double disponibile = Math.Max(360, larghezzaFinestra - 240);

            return Math.Min(target, disponibile);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
