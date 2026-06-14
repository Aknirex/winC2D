namespace winC2D.Core.Models;

public sealed class MigrationPreflightResult
{
    public bool CanProceed => Blockers.Count == 0;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public long SourceSizeBytes { get; set; }
    public long TargetFreeBytes { get; set; }
    public bool HasEnoughSpace { get; set; }
    public bool IsSourceSymlink { get; set; }
    public List<string> Blockers { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
