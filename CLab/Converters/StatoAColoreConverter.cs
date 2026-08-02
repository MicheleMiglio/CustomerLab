using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CLab.Converters
{
    public class StatoAColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value?.ToString() switch
            {
                "Compilato" => "BrushSuccess",
                "Ritardo" => "BrushDanger",
                "InCorso" => "BrushPrimary",
                _ => "BrushBordo" // "Futuro" e ogni altro caso: grigio neutro
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatoASfondoConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value?.ToString() switch
            {
                "Compilato" => "BrushSuccessLight",
                "Ritardo" => "BrushDangerLight",
                "InCorso" => "BrushInfoLight",
                _ => "BrushSfondoChiaro"
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Vero/Falso → colore, per il pallino "versato" delle ritenute.</summary>
    public class VersatoAColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool versato = value is bool b && b;
            return Application.Current.Resources[versato ? "BrushSuccess" : "BrushStatoStandBy"] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatoVersamentoTestoConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString() switch
            {
                "Versato" => "Versato",
                "Anomalia" => "Anomalia",
                _ => "Da versare"
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatoVersamentoAColoreConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string chiave = value?.ToString() switch
            {
                "Versato" => "BrushSuccess",
                "Anomalia" => "BrushDanger",
                _ => "BrushStatoStandBy"
            };
            return Application.Current.Resources[chiave] as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}