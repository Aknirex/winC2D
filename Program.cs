using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Windows.Forms;

namespace winC2D
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Check if running as administrator
            if (!IsRunningAsAdministrator())
            {
                // Restart as administrator
                RestartAsAdministrator();
                return;
            }

            // Load language preference, default to English
            string lang = Localization.LoadLanguagePreference();
            CultureInfo.CurrentUICulture = new CultureInfo(lang);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

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