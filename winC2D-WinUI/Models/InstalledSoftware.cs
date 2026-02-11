using System;

namespace winC2D_WinUI.Models
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
        
        // Formatted size string for UI binding
        public string SizeText
        {
            get
            {
                if (SizeBytes == -1)
                    return "> 10 MB";
                if ((Status == SoftwareStatus.Empty || SuspiciousChecked) && SizeBytes == 0)
                    return "0 KB";
                if (SizeBytes <= 0) return "Unknown";
                if (SizeBytes < 1024 * 1024)
                {
                    var kb = Math.Max(1, SizeBytes / 1024);
                    return kb + " KB";
                }
                return (SizeBytes / (1024 * 1024)) + " MB";
            }
        }
    }
}
