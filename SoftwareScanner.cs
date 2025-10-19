using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

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
        public static List<InstalledSoftware> GetInstalledSoftwareOnC()
        {
            var result = new List<InstalledSoftware>();
            // 32/64位注册表路径
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
                                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(loc) && loc.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
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
                                    result.Add(new InstalledSoftware { Name = name, InstallLocation = loc, SizeBytes = size });
                                }
                            }
                        }
                    }
                }
            }
            return result;
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