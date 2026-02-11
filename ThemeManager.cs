using System;
using System.Drawing;
using System.IO;

namespace winC2D
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public sealed class ThemePalette
    {
        public Color FormBackground { get; init; }
        public Color TabControlBackground { get; init; }
        public Color TabPageBackground { get; init; }
        public Color ControlBackground { get; init; }
        public Color Foreground { get; init; }
        public Color Accent { get; init; }
        public Color ButtonBorder { get; init; }
        public Color ButtonHover { get; init; }
        public Color MenuBackground { get; init; }
        public Color MenuForeground { get; init; }
        public Color MenuItemHover { get; init; }
        public Color MenuItemSelected { get; init; }
        public Color ListViewBackground { get; init; }
        public Color ListViewForeground { get; init; }
    }

    public static class ThemeManager
    {
        private const string ConfigFileName = "theme.config";
        private static AppTheme _currentTheme = AppTheme.Light;

        public static event EventHandler ThemeChanged;

        public static AppTheme CurrentTheme => _currentTheme;

        public static AppTheme LoadPreference()
        {
            try
            {
                string configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    string content = File.ReadAllText(configPath).Trim();
                    if (Enum.TryParse<AppTheme>(content, true, out var parsed))
                    {
                        _currentTheme = parsed;
                        return parsed;
                    }
                }
            }
            catch
            {
            }

            _currentTheme = AppTheme.Light;
            return _currentTheme;
        }

        public static void SetTheme(AppTheme theme)
        {
            if (_currentTheme == theme)
                return;

            _currentTheme = theme;
            SavePreference(theme);
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static ThemePalette GetPalette(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Dark => new ThemePalette
                {
                    FormBackground = Color.FromArgb(20, 24, 31),
                    TabControlBackground = Color.FromArgb(20, 24, 31),
                    TabPageBackground = Color.FromArgb(26, 32, 42),
                    ControlBackground = Color.FromArgb(32, 36, 48),
                    Foreground = Color.FromArgb(233, 233, 239),
                    Accent = Color.FromArgb(0, 120, 215),
                    ButtonBorder = Color.FromArgb(70, 74, 88),
                    ButtonHover = Color.FromArgb(64, 68, 84),
                    MenuBackground = Color.FromArgb(26, 32, 42),
                    MenuForeground = Color.FromArgb(235, 235, 241),
                    MenuItemHover = Color.FromArgb(50, 54, 68),
                    MenuItemSelected = Color.FromArgb(24, 120, 215),
                    ListViewBackground = Color.FromArgb(28, 34, 46),
                    ListViewForeground = Color.FromArgb(236, 236, 242)
                },
                _ => new ThemePalette
                {
                    FormBackground = Color.FromArgb(248, 249, 252),
                    TabControlBackground = Color.FromArgb(248, 249, 252),
                    TabPageBackground = Color.FromArgb(255, 255, 255),
                    ControlBackground = Color.FromArgb(255, 255, 255),
                    Foreground = Color.FromArgb(17, 17, 17),
                    Accent = Color.FromArgb(0, 120, 215),
                    ButtonBorder = Color.FromArgb(210, 220, 230),
                    ButtonHover = Color.FromArgb(229, 231, 235),
                    MenuBackground = Color.FromArgb(250, 250, 252),
                    MenuForeground = Color.FromArgb(32, 32, 33),
                    MenuItemHover = Color.FromArgb(230, 235, 242),
                    MenuItemSelected = Color.FromArgb(204, 228, 255),
                    ListViewBackground = Color.FromArgb(255, 255, 255),
                    ListViewForeground = Color.FromArgb(17, 17, 17)
                }
            };
        }

        private static void SavePreference(AppTheme theme)
        {
            try
            {
                string configPath = GetConfigPath();
                string directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(configPath, theme.ToString());
            }
            catch
            {
            }
        }

        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "winC2D", ConfigFileName);
        }
    }
}
