using System;
using System.Globalization;
using System.IO;
using System.Resources;

namespace winC2D
{
    public static class Localization
    {
        private static ResourceManager _rm = new ResourceManager("winC2D.Strings", typeof(Localization).Assembly);
        private static string _currentLanguage = "en"; // Default to English

        public static event EventHandler LanguageChanged;

        public static string CurrentLanguage => _currentLanguage;

        public static string T(string key)
        {
            return _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }

        public static void SetLanguage(string cultureCode)
        {
            if (_currentLanguage == cultureCode)
                return;

            _currentLanguage = cultureCode;
            CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
            SaveLanguagePreference(cultureCode);
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string LoadLanguagePreference()
        {
            try
            {
                string configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    string lang = File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrWhiteSpace(lang))
                    {
                        _currentLanguage = lang;
                        return lang;
                    }
                }
            }
            catch { }
            return "en"; // Default to English
        }

        private static void SaveLanguagePreference(string languageCode)
        {
            try
            {
                string configPath = GetConfigPath();
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                File.WriteAllText(configPath, languageCode);
            }
            catch { }
        }

        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "winC2D", "language.config");
        }
    }
}
