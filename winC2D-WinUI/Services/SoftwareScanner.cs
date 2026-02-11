using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using winC2D_WinUI.Models;

namespace winC2D_WinUI.Services
{
    public static class SoftwareScanner
    {
        internal const long SuspiciousSizeThresholdBytes = 10L * 1024 * 1024;

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
                    string baseDir = rawBase; // Simplified normalization for now
                    if (!Directory.Exists(baseDir))
                        continue;

                    foreach (var subDir in Directory.EnumerateDirectories(baseDir))
                    {
                        if (visited.Contains(subDir))
                            continue;
                        visited.Add(subDir);

                        var info = BuildInstalledSoftwareFromDirectory(subDir);
                        result.Add(info);
                    }
                }
                catch { }
            }
            return result;
        }

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
                var attr = File.GetAttributes(fullPath);
                isSymlink = (attr & FileAttributes.ReparsePoint) != 0;
            }
            catch { }

            long size = -1;
            var status = SoftwareStatus.Directory;

            if (isSymlink)
            {
                status = SoftwareStatus.Symlink;
                size = 0; 
            }
            else
            {
                try
                {
                    size = GetDirectorySize(fullPath);
                    if (size < SuspiciousSizeThresholdBytes) 
                    {
                        status = SoftwareStatus.Suspicious;
                        if (size == 0 && !Directory.EnumerateFileSystemEntries(fullPath).Any())
                            status = SoftwareStatus.Empty;
                    }
                }
                catch 
                {
                    size = 0; // Error reading size
                }
            }

            return new InstalledSoftware
            {
                Name = Path.GetFileName(fullPath),
                InstallLocation = fullPath,
                SizeBytes = size,
                IsSymlink = isSymlink,
                Status = status
            };
        }
        
        private static long GetDirectorySize(string folderPath)
        {
            long size = 0;
            var dir = new DirectoryInfo(folderPath);
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
            return size;
        }
    }
}
