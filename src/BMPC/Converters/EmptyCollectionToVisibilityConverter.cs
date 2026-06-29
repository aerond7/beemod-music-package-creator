using System.Windows.Data;
using System.Windows;

namespace BMPC.Converters
{
    public class EmptyCollectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count)
            {
                bool inverse = parameter is not null
                    && bool.TryParse(parameter.ToString(), out var parsed)
                    && parsed;
                return (count == 0) ? (inverse ? Visibility.Visible : Visibility.Collapsed) : (inverse ? Visibility.Collapsed : Visibility.Visible);
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
