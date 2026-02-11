using System;
using System.Diagnostics;
using System.IO;

namespace winC2D_WinUI.Services
{
    public static class AppDataMigrator
    {
        public static void MigrateWithMklink(string appName, string sourcePath, string targetRoot)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            string targetPath = Path.Combine(targetRoot, "AppData", appName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

            if (Directory.Exists(targetPath))
                Directory.Delete(targetPath, true);

            CopyDirectory(sourcePath, targetPath);

            string backupPath = sourcePath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Directory.Move(sourcePath, backupPath);

            CreateSymbolicLink(sourcePath, targetPath);

            if (!Directory.Exists(sourcePath))
            {
                Directory.Move(backupPath, sourcePath);
                throw new Exception("Failed to create symbolic link. Backup restored.");
            }

            try { Directory.Delete(backupPath, true); } catch { }
        }

        private static void CreateSymbolicLink(string linkPath, string targetPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /D \"{linkPath}\" \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p.WaitForExit();
            if (p.ExitCode != 0) throw new Exception("mklink failed");
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
            }
        }
    }
}
