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
                throw new DirectoryNotFoundException(string.Format(Localization.T("Msg.SourceDirNotFoundFmt"), sw.InstallLocation));

            // 在用户选择的路径下创建以软件名称命名的子目录
            string targetPath = Path.Combine(parentTargetPath, sw.Name);

            // 调试路径拼接
            Console.WriteLine($"检查目标路径: {targetPath}");

            // 检查目标路径是否为文件夹
            if (Directory.Exists(targetPath))
            {
                throw new IOException(string.Format(Localization.T("Msg.TargetDirExistsFmt"), targetPath));
            }
            else if (File.Exists(targetPath))
            {
                throw new IOException(string.Format(Localization.T("Msg.TargetPathIsFileFmt"), targetPath));
            }

            try
            {
                // 1. 复制文件夹内容到目标路径
                CopyDirectory(sw.InstallLocation, targetPath);

                // 2. 修正注册表
                UpdateRegistryInstallLocation(sw.InstallLocation, targetPath);

                // 3. 修正快捷方式
                try { ShortcutHelper.FixShortcuts(sw.InstallLocation, targetPath); } catch { }

                // 4. 所有步骤成功后，删除源文件夹
                Directory.Delete(sw.InstallLocation, true);
            }
            catch (Exception ex)
            {
                // 只要迁移失败，尝试清理目标路径（包括部分复制的文件夹和文件）
                if (Directory.Exists(targetPath))
                {
                    try { Directory.Delete(targetPath, true); } catch { }
                }
                // 全部用本地化字符串
                string msg = string.Format(Localization.T("Msg.MigrateFailedFmt"), sw.Name, ex.Message);
                throw new Exception(msg, ex);
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