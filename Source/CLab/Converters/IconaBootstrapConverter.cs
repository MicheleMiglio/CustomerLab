using MahApps.Metro.IconPacks;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CLab.Converters
{
    public class IconaBootstrapConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is string nome &&
                Enum.TryParse(
                    nome,
                    out PackIconBootstrapIconsKind kind))
            {
                return kind;
            }

            return PackIconBootstrapIconsKind.Question;
        }


        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}