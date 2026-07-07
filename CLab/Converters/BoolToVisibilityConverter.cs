using CLab.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool risultato = value switch
            {
                bool b => b,
                int i => i > 0,
                string s => !string.IsNullOrEmpty(s),
                _ => value != null
            };

            if (parameter is string p && p == "Inverti")
                risultato = !risultato;

            return risultato ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PrincipaleColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? Brushes.Goldenrod : Brushes.LightGray;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatoBadgeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                StatoCliente.Attivo => "Attivo",
                StatoCliente.StandBy => "Stand by",
                StatoCliente.Cessato => "Cessato",
                _ => value?.ToString() ?? ""
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatoBadgeForeConverter : IValueConverter
    {
        private static readonly SolidColorBrush Verde = new(Color.FromRgb(0x16, 0x65, 0x34)); // #166534
        private static readonly SolidColorBrush Ambra = new(Color.FromRgb(0x85, 0x4D, 0x0E)); // #854D0E
        private static readonly SolidColorBrush Rosso = new(Color.FromRgb(0x99, 0x1B, 0x1B)); // #991B1B

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                StatoCliente.Attivo => Verde,
                StatoCliente.StandBy => Ambra,
                StatoCliente.Cessato => Rosso,
                _ => Brushes.Gray
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatoBadgeBackConverter : IValueConverter
    {
        private static readonly SolidColorBrush BgVerde = new(Color.FromRgb(0xDC, 0xFC, 0xE7)); // #DCFCE7
        private static readonly SolidColorBrush BgAmbra = new(Color.FromRgb(0xFE, 0xF9, 0xC3)); // #FEF9C3
        private static readonly SolidColorBrush BgRosso = new(Color.FromRgb(0xFE, 0xE2, 0xE2)); // #FEE2E2

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                StatoCliente.Attivo => BgVerde,
                StatoCliente.StandBy => BgAmbra,
                StatoCliente.Cessato => BgRosso,
                _ => Brushes.LightGray
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}