using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BMPC.Converters
{
    public class InverseImportVisibleToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Visibility.Collapsed;
            bool buttonsVisible = values[0] is bool b && b;
            bool isImportVisible = values[1] is bool i && i;

            return buttonsVisible && !isImportVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
