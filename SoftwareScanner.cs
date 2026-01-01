using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace winC2D
{
    public enum SoftwareStatus
    {
        Directory,
        Symlink,
        Suspicious,
        Empty,
        Residual
    }

    public class InstalledSoftware
    {
        public string Name { get; set; }
        public string InstallLocation { get; set; }
        public long SizeBytes { get; set; }
        public bool IsSymlink { get; set; }
        public SoftwareStatus Status { get; set; }
        public bool SuspiciousChecked { get; set; }
        public string SizeText
        {
            get
            {
                if (SizeBytes == -1)
                    return $"> {SoftwareScanner.SuspiciousSizeThresholdBytes / (1024 * 1024)} MB";
                if ((Status == SoftwareStatus.Empty || SuspiciousChecked) && SizeBytes == 0)
                    return "0 KB";
                if (SizeBytes <= 0) return Localization.T("Msg.UnknownSize");
                if (SizeBytes < 1024 * 1024)
                {
                    var kb = Math.Max(1, SizeBytes / 1024);
                    return kb + " KB";
                }
                return (SizeBytes / (1024 * 1024)) + " MB";
            }
        }
    }

    public static class SoftwareScanner
    {
        internal const long SuspiciousSizeThresholdBytes = 10L * 1024 * 1024; // 10 MB，过小则标为可疑

        // 新增重载，支持传入多个路径（仅目录扫描，不依赖注册表）
        public static List<InstalledSoftware> GetInstalledSoftwareOnC(IEnumerable<string> programFilesDirs)
        {
            var result = new List<InstalledSoftware>();
            var pathSet = new HashSet<string>(programFilesDirs.Where(p => !string.IsNullOrWhiteSpace(p)), StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawBase in pathSet)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(rawBase))
                        continue;
                    var baseDir = NormalizeDir(rawBase);
                    if (!Directory.Exists(baseDir))
                        continue;

                    foreach (var subDir in Directory.EnumerateDirectories(baseDir))
                    {
                        string fullPath;
                        try
                        {
                            fullPath = NormalizeDir(subDir);
                        }
                        catch
                        {
                            continue;
                        }

                        if (visited.Contains(fullPath))
                            continue;
                        visited.Add(fullPath);

                        var info = BuildInstalledSoftwareFromDirectory(fullPath);
                        result.Add(info);
                    }
                }
                catch
                {
                    // 忽略单个根路径的扫描错误，继续其他路径
                }
            }
            return result;
        }

        // 兼容旧用法，自动用系统路径
        public static List<InstalledSoftware> GetInstalledSoftwareOnC()
        {
            var paths = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            if (Environment.Is64BitOperatingSystem)
                paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            return GetInstalledSoftwareOnC(paths);
        }

        private static InstalledSoftware BuildInstalledSoftwareFromDirectory(string fullPath)
        {
            bool isSymlink = false;
            try
            {
                isSymlink = (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0;
            }
            catch { }

            bool hasEntries = false;
            try
            {
                hasEntries = Directory.EnumerateFileSystemEntries(fullPath).Any();
            }
            catch { }

            bool exceeded;
            long quickSize = GetDirectorySizeUntil(fullPath, SuspiciousSizeThresholdBytes, out exceeded);

            var status = SoftwareStatus.Directory;
            if (isSymlink)
                status = SoftwareStatus.Symlink;
            else if (!hasEntries)
                status = SoftwareStatus.Empty;
            else if (!exceeded && quickSize <= SuspiciousSizeThresholdBytes)
                status = SoftwareStatus.Suspicious;

            // 目录较大或已超过阈值，不再继续计算完整大小，避免卡顿
            long displaySize = exceeded ? -1 : quickSize;

            return new InstalledSoftware
            {
                Name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                InstallLocation = fullPath,
                SizeBytes = displaySize,
                IsSymlink = isSymlink,
                Status = status,
                SuspiciousChecked = false
            };
        }

        /// <summary>
        /// 对可疑目录进行深入检查：计算大小，查找exe以判定是否卸载残留
        /// </summary>
        public static void CheckSuspiciousDirectory(InstalledSoftware sw)
        {
            if (sw == null || string.IsNullOrWhiteSpace(sw.InstallLocation)) return;
            var path = sw.InstallLocation;
            if (!Directory.Exists(path))
            {
                sw.Status = SoftwareStatus.Residual;
                sw.SizeBytes = 0;
                sw.SuspiciousChecked = true;
                return;
            }

            bool hasEntry = false;
            bool hasExe = false;
            long size = 0;

            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    hasEntry = true;
                    if (!hasExe && file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        hasExe = true;
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch { }
                }
                if (!hasEntry)
                {
                    foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
                    {
                        hasEntry = true;
                        break; // 有子目录即可认为非空
                    }
                }
            }
            catch
            {
                // 忽略扫描异常，保持已有状态
            }

            sw.SizeBytes = size;
            sw.SuspiciousChecked = true;

            if (!hasEntry)
            {
                sw.Status = SoftwareStatus.Empty;
            }
            else if (!hasExe)
            {
                sw.Status = SoftwareStatus.Residual;
            }
            else
            {
                sw.Status = SoftwareStatus.Directory;
            }
        }

        private static string NormalizeDir(string path)
        {
            return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        /// <summary>
        /// 在达到阈值时提前终止，避免长时间扫描
        /// </summary>
        private static long GetDirectorySizeUntil(string path, long threshold, out bool exceeded)
        {
            long size = 0;
            exceeded = false;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                        if (size > threshold)
                        {
                            exceeded = true;
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}