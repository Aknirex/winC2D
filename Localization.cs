using System.Globalization;
using System.Resources;

namespace winC2D
{
    public static class Localization
    {
        private static ResourceManager _rm = new ResourceManager("winC2D.Strings", typeof(Localization).Assembly);

        public static string T(string key)
        {
            return _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }
    }
}
