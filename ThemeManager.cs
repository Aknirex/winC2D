using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

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
        public Color ListViewHeaderBackground { get; init; }
        public Color ListViewHeaderForeground { get; init; }
        public Color ListViewRowBackground { get; init; }
        public Color ListViewRowAlternateBackground { get; init; }
        public Color ListViewRowHoverBackground { get; init; }
        public Color ListViewRowSelectedBackground { get; init; }
        public Color ListViewGridColor { get; init; }
    }

    public static class ThemeManager
    {
        private const string ConfigFileName = "theme.config";
        private static AppTheme _currentTheme = AppTheme.Light;

        #region Dark Mode P/Invoke
        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern int SetPreferredAppMode(int appMode); // 1 = AllowDark
        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern int SetPreferredAppMode1903(int appMode); // different signature/enum might apply but 1 usually works for "AllowDark" or "ForceDark"

        // PreferredAppMode enum for newer Windows
        private enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        };
        
        [DllImport("uxtheme.dll", EntryPoint = "#135", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SetPreferredAppMode(PreferredAppMode appMode);

        public static void ApplySystemDarkMode(bool isDark)
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 10)
                {
                    // Attempt to set preferred app mode for scrollbars/common controls
                    // Using value 2 (ForceDark) or 1 (AllowDark)
                    SetPreferredAppMode(isDark ? PreferredAppMode.ForceDark : PreferredAppMode.ForceLight);
                }
            }
            catch
            {
                try
                {
                    // Fallback for older Win10 builds
                    SetPreferredAppMode(isDark ? 2 : 0);
                }
                catch { }
            }
        }
        #endregion

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
                        ApplySystemDarkMode(_currentTheme == AppTheme.Dark);
                        return parsed;
                    }
                }
            }
            catch
            {
            }

            _currentTheme = AppTheme.Light;
            ApplySystemDarkMode(false);
            return _currentTheme;
        }

        public static void SetTheme(AppTheme theme)
        {
            if (_currentTheme == theme)
                return;

            _currentTheme = theme;
            ApplySystemDarkMode(theme == AppTheme.Dark);
            SavePreference(theme);
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static ThemePalette GetPalette(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Dark => new ThemePalette
                {
                    FormBackground = Color.FromArgb(32, 32, 32),
                    TabControlBackground = Color.FromArgb(32, 32, 32),
                    TabPageBackground = Color.FromArgb(32, 32, 32),
                    ControlBackground = Color.FromArgb(45, 45, 45),
                    Foreground = Color.FromArgb(255, 255, 255),
                    Accent = Color.FromArgb(96, 205, 255),
                    ButtonBorder = Color.FromArgb(60, 60, 60),
                    ButtonHover = Color.FromArgb(55, 55, 55),
                    MenuBackground = Color.FromArgb(32, 32, 32),
                    MenuForeground = Color.FromArgb(255, 255, 255),
                    MenuItemHover = Color.FromArgb(55, 55, 55),
                    MenuItemSelected = Color.FromArgb(65, 65, 65),
                    ListViewBackground = Color.FromArgb(43, 43, 43),
                    ListViewForeground = Color.FromArgb(255, 255, 255),
                    ListViewHeaderBackground = Color.Transparent,
                    ListViewHeaderForeground = Color.FromArgb(160, 160, 160),
                    ListViewRowBackground = Color.FromArgb(50, 50, 50),
                    ListViewRowAlternateBackground = Color.FromArgb(56, 56, 56),
                    ListViewRowHoverBackground = Color.FromArgb(63, 63, 63),
                    ListViewRowSelectedBackground = Color.FromArgb(69, 69, 69),
                    ListViewGridColor = Color.FromArgb(64, 64, 64)
                },
                _ => new ThemePalette
                {
                    FormBackground = Color.FromArgb(248, 249, 252),
                    TabControlBackground = Color.FromArgb(248, 249, 252),
                    TabPageBackground = Color.FromArgb(255, 255, 255),
                    ControlBackground = Color.FromArgb(255, 255, 255),
                    Foreground = Color.FromArgb(17, 17, 17),
                    Accent = Color.FromArgb(0, 120, 215),
                    ButtonBorder = Color.FromArgb(208, 208, 208), // Slightly darker for visibility
                    ButtonHover = Color.FromArgb(240, 240, 240),
                    MenuBackground = Color.FromArgb(248, 249, 252),
                    MenuForeground = Color.FromArgb(17, 17, 17),
                    MenuItemHover = Color.FromArgb(230, 230, 230),
                    MenuItemSelected = Color.FromArgb(0, 120, 215),
                    ListViewBackground = Color.White,
                    ListViewForeground = Color.Black,
                    ListViewHeaderBackground = Color.White,
                    ListViewHeaderForeground = Color.FromArgb(17, 17, 17),
                    ListViewRowBackground = Color.White,
                    ListViewRowAlternateBackground = Color.FromArgb(245, 245, 245),
                    ListViewRowHoverBackground = Color.FromArgb(235, 235, 235),
                    ListViewRowSelectedBackground = Color.FromArgb(204, 228, 247),
                    ListViewGridColor = Color.FromArgb(220, 220, 220)
                }
            };
        }

        private static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        private static void SavePreference(AppTheme theme)
        {
            try
            {
                File.WriteAllText(GetConfigPath(), theme.ToString());
            }
            catch { }
        }
    }
}
