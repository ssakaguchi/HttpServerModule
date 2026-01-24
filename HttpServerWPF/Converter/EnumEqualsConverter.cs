using System.Globalization;
using System.Windows.Data;

namespace HttpServerWPF.Converter
{
    public class EnumEqualsConverter : IValueConverter
    {
        // enum -> bool
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // bool -> enum (checked のときだけ反映)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter != null)
            {
                // targetType は AuthenticationMethodType
                return Enum.Parse(targetType, parameter.ToString()!, ignoreCase: true);
            }

            return Binding.DoNothing;
        }
    }
}