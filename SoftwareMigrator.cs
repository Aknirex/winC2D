using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace winC2D
{
    public static class SoftwareMigrator
    {
        public static void MigrateSoftware(InstalledSoftware sw, string parentTargetPath)
        {
            if (!Directory.Exists(sw.InstallLocation))
                throw new DirectoryNotFoundException($"未找到软件目录: {sw.InstallLocation}");

            // 在用户选择的路径下创建以软件名称命名的子目录
            string targetPath = Path.Combine(parentTargetPath, sw.Name);

            // 调试路径拼接
            Console.WriteLine($"检查目标路径: {targetPath}");

            // 检查目标路径是否为文件夹
            if (Directory.Exists(targetPath))
            {
                throw new IOException($"目标子目录已存在: {targetPath}");
            }
            else if (File.Exists(targetPath))
            {
                throw new IOException($"目标路径是一个文件，而非文件夹: {targetPath}");
            }

            try
            {
                // 1. 复制文件夹内容到目标路径
                CopyDirectory(sw.InstallLocation, targetPath);

                // 2. 删除源文件夹
                Directory.Delete(sw.InstallLocation, true);

                // 3. 修正注册表
                UpdateRegistryInstallLocation(sw.InstallLocation, targetPath);

                // 4. 修正快捷方式
                try { ShortcutHelper.FixShortcuts(sw.InstallLocation, targetPath); } catch { }
            }
            catch (Exception ex)
            {
                // 如果迁移失败，清理目标路径
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                throw new Exception($"迁移失败: {ex.Message}", ex);
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, targetSubDir);
            }
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