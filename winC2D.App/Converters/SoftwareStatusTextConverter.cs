using System.Globalization;
using System.Windows.Data;
using winC2D.Core.Models;
using winC2D.Infrastructure.Localization;

namespace winC2D.App.Converters;

/// <summary>
/// Converts a <see cref="SoftwareStatus"/> value to a localized display string.
/// The converter is refreshed each time the language changes because the ViewModel
/// fires NotifyStatusTextChanged() on all rows, which re-evaluates the binding.
/// </summary>
public class SoftwareStatusTextConverter : IValueConverter
{
    /// <summary>Singleton instance so it can be used from XAML resources.</summary>
    public static readonly SoftwareStatusTextConverter Instance = new();

    // Injected / set by App.xaml.cs after DI container is built
    public static ILocalizationService? LocalizationService { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SoftwareStatus status || LocalizationService is null)
            return value?.ToString() ?? string.Empty;

        return status switch
        {
            SoftwareStatus.Normal     => LocalizationService.GetString("SoftwareMigration.StatusNormal"),
            SoftwareStatus.Migrated   => LocalizationService.GetString("SoftwareMigration.StatusMigrated"),
            SoftwareStatus.Suspicious => LocalizationService.GetString("SoftwareMigration.StatusSuspicious"),
            SoftwareStatus.Empty      => LocalizationService.GetString("SoftwareMigration.StatusEmpty"),
            SoftwareStatus.Residual   => LocalizationService.GetString("SoftwareMigration.StatusResidual"),
            _                         => status.ToString()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
