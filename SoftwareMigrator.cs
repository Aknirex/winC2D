using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace winC2D
{
    public static class SoftwareMigrator
    {
        public static void MigrateSoftware(InstalledSoftware sw, string targetRoot)
        {
            if (!Directory.Exists(sw.InstallLocation))
                throw new DirectoryNotFoundException($"未找到软件目录: {sw.InstallLocation}");
            string targetPath = Path.Combine(targetRoot, Path.GetFileName(sw.InstallLocation));
            if (Directory.Exists(targetPath))
                throw new IOException($"目标目录已存在: {targetPath}");
            // 1. 移动文件夹
            Directory.Move(sw.InstallLocation, targetPath);
            // 2. 修正注册表
            UpdateRegistryInstallLocation(sw.InstallLocation, targetPath);
            // 3. 修正快捷方式
            try { ShortcutHelper.FixShortcuts(sw.InstallLocation, targetPath); } catch { }
        }

    public static void UpdateRegistryInstallLocation(string oldPath, string newPath)
        {
            string[] regPaths = new[] {
                @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                @"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var regPath in regPaths)
                {
                    using (var key = root.OpenSubKey(regPath, writable: true))
                    {
                        if (key == null) continue;
                        foreach (var subName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subName, writable: true))
                            {
                                var loc = subKey.GetValue("InstallLocation") as string;
                                if (!string.IsNullOrEmpty(loc) && string.Equals(loc, oldPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    subKey.SetValue("InstallLocation", newPath);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}