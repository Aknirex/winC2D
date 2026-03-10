using System.Globalization;
using System.Windows.Data;

namespace winC2D.App.Converters;

/// <summary>
/// Converts a null/non-null value to a boolean (non-null = true, null = false).
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
