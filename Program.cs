using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Windows.Forms;
using winC2D.Core;    // ThemeManager v2

namespace winC2D
{
    static class Program
    {
        // 设为 true 使用全新 UI；false 保持旧版
        private const bool UseNewUI = true;

        [STAThread]
        static void Main()
        {
            // 全局异常捕获，防止静默崩溃
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.ToString(), "未处理的异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show(e.ExceptionObject?.ToString(), "致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // 管理员权限检查：未以管理员运行时尝试提权（仅 Release 模式强制）
#if !DEBUG
            if (!IsRunningAsAdministrator())
            {
                RestartAsAdministrator();
                return;
            }
#endif

            // 加载语言偏好
            string lang = Localization.LoadLanguagePreference();
            CultureInfo.CurrentUICulture = new CultureInfo(lang);

            // 加载主题偏好（新版 ThemeManager）
            winC2D.Core.ThemeManager.LoadPreference();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                if (UseNewUI)
                    Application.Run(new MainForm2());
                else
                    Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void RestartAsAdministrator()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas" // Request administrator privileges
                };

                Process.Start(startInfo);
            }
            catch
            {
                // User cancelled UAC prompt or other error
                MessageBox.Show(
                    "This application requires administrator privileges to run.\nPlease restart the application as administrator.",
                    "Administrator Privileges Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}