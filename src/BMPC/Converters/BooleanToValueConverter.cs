using System.Globalization;
using System.Windows.Data;

namespace BMPC.Converters
{
    public class BooleanToValueConverter : IValueConverter
    {
        public int DesiredValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value ? DesiredValue : 0;
            }

            throw new InvalidOperationException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
