using System;
using System.IO;
using System.Runtime.InteropServices;

namespace winC2D
{
    public static class UserFolderMigrator
    {
        // 迁移并重定向用户文件夹
        public static void MigrateAndRedirect(string folderName, string oldPath, string newRoot)
        {
            if (!Directory.Exists(oldPath))
                throw new DirectoryNotFoundException($"未找到原文件夹: {oldPath}");
            string newPath = Path.Combine(newRoot, Path.GetFileName(oldPath));
            if (Directory.Exists(newPath))
                throw new IOException($"目标目录已存在: {newPath}");
            Directory.Move(oldPath, newPath);
            // 修改注册表重定向
            RedirectUserShellFolder(folderName, newPath);
        }

        // 修改用户Shell文件夹注册表
        private static void RedirectUserShellFolder(string folderName, string newPath)
        {
            // 位置: HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true);
            if (key == null)
                throw new Exception("无法打开User Shell Folders注册表项");
            string regName = GetShellFolderRegName(folderName);
            if (regName == null)
                throw new Exception($"不支持的文件夹: {folderName}");
            key.SetValue(regName, newPath);
        }

        // 文件夹名到注册表键名映射
        private static string GetShellFolderRegName(string folderName)
        {
            switch (folderName)
            {
                case "文档": return "Personal";
                case "图片": return "My Pictures";
                case "下载": return "{374DE290-123F-4565-9164-39C4925E467B}";
                case "视频": return "My Video";
                case "桌面": return "Desktop";
                default: return null;
            }
        }
    }
}