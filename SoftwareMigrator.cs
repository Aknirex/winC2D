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

            string sourcePath = sw.InstallLocation;
            string sourceTempPath = sourcePath + "_migrating_" + Guid.NewGuid().ToString("N");
            string targetPath = Path.Combine(parentTargetPath, sw.Name);
            string tempTargetPath = targetPath + "_temp";
            Console.WriteLine($"检查目标路径: {targetPath}");

            if (Directory.Exists(targetPath) || Directory.Exists(tempTargetPath))
                throw new IOException(string.Format(Localization.T("Msg.TargetDirExistsFmt"), targetPath));
            if (File.Exists(targetPath) || File.Exists(tempTargetPath))
                throw new IOException(string.Format(Localization.T("Msg.TargetPathIsFileFmt"), targetPath));

            bool renamed = false;
            try
            {
                // 1. 尝试重命名源目录，判断是否有文件被占用
                Directory.Move(sourcePath, sourceTempPath);
                renamed = true;

                // 2. 复制到临时目录，遇到异常立即终止
                CopyDirectoryAllOrFail(sourceTempPath, tempTargetPath);

                // 3. 原子重命名临时目录为目标目录
                Directory.Move(tempTargetPath, targetPath);

                // 4. 删除重命名后的源目录
                Directory.Delete(sourceTempPath, true);

                // 5. 创建符号链接
                CreateDirectorySymlink(sourcePath, targetPath);
            }
            catch (Exception ex)
            {
                // 失败时清理临时目录
                if (Directory.Exists(tempTargetPath))
                {
                    try { Directory.Delete(tempTargetPath, true); } catch { }
                }
                // 如果重命名成功但后续失败，尝试恢复原目录名
                if (renamed && Directory.Exists(sourceTempPath) && !Directory.Exists(sourcePath))
                {
                    try { Directory.Move(sourceTempPath, sourcePath); } catch { }
                }
                string msg = string.Format(Localization.T("Msg.MigrateFailedFmt"), sw.Name, ex.Message);
                throw new Exception(msg, ex);
            }
        }

        // 递归复制，遇到任何异常立即抛出，保证原子性
        private static void CopyDirectoryAllOrFail(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, overwrite: false); // 不覆盖，遇到异常直接抛出
            }
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectoryAllOrFail(directory, targetSubDir); // 递归，遇到异常直接抛出
            }
        }

        private static void CreateDirectorySymlink(string linkPath, string targetPath)
        {
            try
            {
#if NET6_0_OR_GREATER
                // .NET 6+ 支持
                Directory.CreateSymbolicLink(linkPath, targetPath);
#else
                // 低版本用cmd
                var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /D \"{linkPath}\" \"{targetPath}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new Exception("mklink failed");
#endif
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to create symlink: {ex.Message}", ex);
            }
        }

        private static bool IsSymlink(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        /// <summary>
        /// 回滚迁移：将新位置内容迁回原位置，处理符号链接，失败时自动恢复符号链接
        /// </summary>
        /// <param name="oldPath">原安装路径</param>
        /// <param name="newPath">迁移后路径</param>
        public static void RollbackSoftware(string oldPath, string newPath)
        {
            bool symlinkDeleted = false;
            // 1. 合法性检查与符号链接处理
            if (Directory.Exists(oldPath))
            {
                if (IsSymlink(oldPath))
                {
                    Directory.Delete(oldPath); // 删除符号链接
                    symlinkDeleted = true;
                }
                else
                {
                    throw new IOException("原路径已存在且不是符号链接，无法回滚");
                }
            }
            try
            {
                // 2. 执行回滚（如将 newPath 内容复制回 oldPath）
                CopyDirectoryAllOrFail(newPath, oldPath);
                Directory.Delete(newPath, true);
            }
            catch (Exception)
            {
                // 3. 回滚失败，恢复符号链接
                if (symlinkDeleted)
                {
                    CreateDirectorySymlink(oldPath, newPath);
                }
                throw;
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