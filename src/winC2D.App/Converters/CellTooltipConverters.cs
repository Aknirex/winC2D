using System.Globalization;
using System.Windows.Data;
using winC2D.Core.Models;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.Converters;

/// <summary>
/// Returns a localized tooltip for a SoftwareInfo's Size cell.
/// Binding: {Binding ., Converter={StaticResource SizeCellTooltipConverter}}
/// </summary>
public class SizeCellTooltipConverter : IValueConverter
{
    public static readonly SizeCellTooltipConverter Instance = new();
    public static ILocalizationService? LocalizationService { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SoftwareInfo item || LocalizationService is null)
            return string.Empty;

        if (item.SizeBytes == -1)
            return LocalizationService.GetString("SoftwareMigration.TooltipSizeExceeds");

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Returns a localized tooltip for a SoftwareInfo's Status cell.
/// </summary>
public class StatusCellTooltipConverter : IValueConverter
{
    public static readonly StatusCellTooltipConverter Instance = new();
    public static ILocalizationService? LocalizationService { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SoftwareInfo item || LocalizationService is null)
            return string.Empty;

        return item.Status switch
        {
            SoftwareStatus.Suspicious => LocalizationService.GetString("SoftwareMigration.TooltipSuspicious"),
            SoftwareStatus.Empty      => LocalizationService.GetString("SoftwareMigration.TooltipEmpty"),
            SoftwareStatus.Migrated   => LocalizationService.GetString("SoftwareMigration.TooltipMigrated"),
            _                         => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
