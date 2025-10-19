using System;
using System.Diagnostics;
using System.IO;

namespace winC2D
{
    public static class AppDataMigrator
    {
        /// <summary>
        /// 使用 mklink 迁移 AppData 文件夹到目标位置
        /// </summary>
        /// <param name="appName">应用名称（AppData中的文件夹名）</param>
        /// <param name="sourcePath">原始路径</param>
        /// <param name="targetRoot">目标根目录</param>
        public static void MigrateWithMklink(string appName, string sourcePath, string targetRoot)
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
            }

            // 目标路径
            string targetPath = Path.Combine(targetRoot, "AppData", appName);
            
            // 确保目标父目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

            // 如果目标已存在，先删除
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            // 复制文件到目标位置
            CopyDirectory(sourcePath, targetPath);

            // 备份原始目录
            string backupPath = sourcePath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Directory.Move(sourcePath, backupPath);

            // 创建符号链接 (使用 mklink /D)
            CreateSymbolicLink(sourcePath, targetPath);

            // 验证链接是否成功
            if (!Directory.Exists(sourcePath))
            {
                // 如果失败，恢复备份
                Directory.Move(backupPath, sourcePath);
                throw new Exception("Failed to create symbolic link. Backup restored.");
            }

            // 删除备份（成功后）
            try
            {
                Directory.Delete(backupPath, true);
            }
            catch
            {
                // 备份删除失败不影响主流程
            }
        }

        /// <summary>
        /// 使用 mklink /D 创建目录符号链接
        /// </summary>
        private static void CreateSymbolicLink(string linkPath, string targetPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /D \"{linkPath}\" \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"mklink failed: {error}\n{output}");
                }
            }
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(dir, destDir);
            }
        }

        /// <summary>
        /// 获取目录大小（递归）
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            long size = 0;
            try
            {
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        size += fi.Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        public static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class AppDataInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public string SizeText => AppDataMigrator.FormatSize(Size);
        public string Type { get; set; } // Roaming, Local, LocalLow
    }
}
