using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace winC2D
{
    public class InstalledSoftware
    {
        public string Name { get; set; }
        public string InstallLocation { get; set; }
        public long SizeBytes { get; set; }
        public string SizeText => SizeBytes > 0 ? (SizeBytes / (1024 * 1024)) + " MB" : "未知";
    }

    public static class SoftwareScanner
    {
        // 新增重载，支持传入多个路径
        public static List<InstalledSoftware> GetInstalledSoftwareOnC(IEnumerable<string> programFilesDirs)
        {
            var result = new List<InstalledSoftware>();
            var pathSet = new HashSet<string>(programFilesDirs.Where(p => !string.IsNullOrEmpty(p)), StringComparer.OrdinalIgnoreCase);

            string[] regPaths = new[] {
                @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                @"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var regPath in regPaths)
                {
                    using (var key = root.OpenSubKey(regPath))
                    {
                        if (key == null) continue;
                        foreach (var subName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subName))
                            {
                                var name = subKey.GetValue("DisplayName") as string;
                                var loc = subKey.GetValue("InstallLocation") as string;
                                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(loc))
                                {
                                    // 判断是否在任一 Program Files 路径及其子目录下
                                    if (pathSet.Any(baseDir => IsUnderDirectory(loc, baseDir)))
                                    {
                                        long size = 0;
                                        try
                                        {
                                            if (Directory.Exists(loc))
                                            {
                                                size = GetDirectorySize(loc);
                                            }
                                        }
                                        catch { }
                                        // 避免重复
                                        if (!result.Any(x => string.Equals(x.InstallLocation, loc, StringComparison.OrdinalIgnoreCase)))
                                            result.Add(new InstalledSoftware { Name = name, InstallLocation = loc, SizeBytes = size });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        // 兼容旧用法，自动用系统路径
        public static List<InstalledSoftware> GetInstalledSoftwareOnC()
        {
            var paths = new List<string> { 
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            if (Environment.Is64BitOperatingSystem)
                paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            return GetInstalledSoftwareOnC(paths);
        }

        private static bool IsUnderDirectory(string path, string baseDir)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(baseDir)) return false;
            try
            {
                var fullPath = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var fullBase = Path.GetFullPath(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(file).Length; } catch { }
                }
            }
            catch { }
            return size;
        }
    }
}