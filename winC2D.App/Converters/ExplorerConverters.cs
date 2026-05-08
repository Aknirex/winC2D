using System.Globalization;
using System.Windows.Data;
using winC2D.Core.Models;

namespace winC2D.App.Converters;

/// <summary>
/// Converts a <see cref="FileSystemItemStatus"/> value to a display string.
/// </summary>
public class FileSystemItemStatusConverter : IValueConverter
{
    public static readonly FileSystemItemStatusConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FileSystemItemStatus status)
            return value?.ToString() ?? string.Empty;

        return status switch
        {
            FileSystemItemStatus.Normal => "正常",
            FileSystemItemStatus.Migrated => "已迁移",
            FileSystemItemStatus.Suspicious => "可疑",
            FileSystemItemStatus.Empty => "空",
            FileSystemItemStatus.Residual => "残留",
            FileSystemItemStatus.AccessDenied => "拒绝访问",
            FileSystemItemStatus.Error => "错误",
            _ => status.ToString(),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Converts a bool to Visibility.Visible (true) or Visibility.Collapsed (false).
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public static readonly BooleanToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b) return System.Windows.Visibility.Visible;
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
