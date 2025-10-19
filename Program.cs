using System;
using System.Windows.Forms;

namespace winC2D
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // 可选：根据系统或外部参数设置界面语言
            try
            {
                var lang = Environment.GetEnvironmentVariable("WINC2D_UI_LANG");
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    var culture = new System.Globalization.CultureInfo(lang);
                    System.Globalization.CultureInfo.CurrentUICulture = culture;
                }
            }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}