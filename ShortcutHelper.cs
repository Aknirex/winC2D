using System;
using System.IO;

namespace winC2D
{
    public static class ShortcutHelper
    {
        // 修正所有快捷方式（桌面、开始菜单）指向新路径
        public static void FixShortcuts(string oldPath, string newPath)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            FixShortcutsInFolder(desktop, oldPath, newPath);
            FixShortcutsInFolder(startMenu, oldPath, newPath);
        }

        private static void FixShortcutsInFolder(string folder, string oldPath, string newPath)
        {
            foreach (var lnk in Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    var shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType == null) continue;
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(lnk);
                    string target = shortcut.TargetPath as string;
                    if (!string.IsNullOrEmpty(target) && target.StartsWith(oldPath, StringComparison.OrdinalIgnoreCase))
                    {
                        shortcut.TargetPath = target.Replace(oldPath, newPath);
                        shortcut.Save();
                    }
                }
                catch { }
            }
        }
    }
}