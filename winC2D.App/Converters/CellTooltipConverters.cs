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

        // SizeBytes == 0 with a non-Empty status means the size has not yet been
        // measured (displayed as "—" in the size column). Show a tooltip to explain.
        if (item.SizeBytes == 0 && item.Status != SoftwareStatus.Empty)
            return LocalizationService.GetString("SoftwareMigration.TooltipSizeNotMeasured");

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
