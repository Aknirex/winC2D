using winC2D.Core.FileSystem;
using winC2D.Core.Models;
using winC2D.Core.Services;

namespace winC2D.Infrastructure.Services;

public sealed class MigrationPreflightService : IMigrationPreflightService
{
    private static readonly string[] Blacklist =
    [
        @"C:\Windows",
        @"C:\Program Files\Windows NT",
        @"C:\Program Files\Common Files",
        @"C:\Program Files\Windows Defender",
        @"C:\Program Files (x86)\Windows NT",
        @"C:\Program Files (x86)\Common Files",
    ];

    private readonly IFileSystem _fileSystem;

    public MigrationPreflightService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<MigrationPreflightResult> ValidateAsync(MigrationRequest request, CancellationToken cancellationToken = default)
    {
        var result = new MigrationPreflightResult
        {
            SourcePath = request.SourcePath,
            TargetPath = DetermineTargetPath(request)
        };

        if (string.IsNullOrWhiteSpace(request.SourcePath))
        {
            result.Blockers.Add("Source path is empty.");
            return Task.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(request.TargetRootPath))
        {
            result.Blockers.Add("Target path is empty.");
            return Task.FromResult(result);
        }

        var sourceExists = _fileSystem.DirectoryExists(request.SourcePath);
        if (!sourceExists)
            result.Blockers.Add($"Source path does not exist: {request.SourcePath}");

        result.IsSourceSymlink = sourceExists && _fileSystem.IsSymlink(request.SourcePath);
        if (result.IsSourceSymlink)
            result.Blockers.Add("Source is already a symbolic link. Use rollback to undo an existing migration first.");

        foreach (var blocked in Blacklist)
        {
            if (request.SourcePath.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
            {
                result.Blockers.Add($"Source path is in the system blacklist: {blocked}");
                break;
            }
        }

        if (sourceExists && !result.IsSourceSymlink)
        {
            try
            {
                result.SourceSizeBytes = _fileSystem.GetDirectorySize(request.SourcePath);
            }
            catch (Exception ex)
            {
                result.Blockers.Add($"Cannot read source directory: {ex.Message}");
            }
        }

        try
        {
            var sourcePath = NormalizeFullPath(request.SourcePath);
            var targetPath = NormalizeFullPath(result.TargetPath);

            if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
                result.Blockers.Add("Target path is the same as the source path.");
            if (IsChildPath(targetPath, sourcePath))
                result.Blockers.Add("Target path cannot be inside the source directory.");
            if (IsChildPath(sourcePath, targetPath))
                result.Blockers.Add("Source path cannot be inside the target directory.");
        }
        catch (Exception ex)
        {
            result.Blockers.Add($"Invalid migration path: {ex.Message}");
        }

        var root = Path.GetPathRoot(result.TargetPath);
        var targetDrive = _fileSystem.GetDrives()
            .FirstOrDefault(d => d.IsReady &&
                                 string.Equals(d.RootDirectory.FullName.TrimEnd('\\'),
                                     root?.TrimEnd('\\'),
                                     StringComparison.OrdinalIgnoreCase));

        if (targetDrive is null)
        {
            result.Blockers.Add($"Target drive not found or not ready: {root}");
        }
        else
        {
            result.TargetFreeBytes = targetDrive.AvailableFreeSpace;
            result.HasEnoughSpace = result.TargetFreeBytes > (long)(result.SourceSizeBytes * 1.1);
            if (!result.HasEnoughSpace)
            {
                result.Blockers.Add(
                    $"Insufficient disk space on {targetDrive.RootDirectory.FullName}. Need {result.SourceSizeBytes / 1_048_576.0:F0} MB, available {result.TargetFreeBytes / 1_048_576.0:F0} MB.");
            }
        }

        if (_fileSystem.DirectoryExists(result.TargetPath) || _fileSystem.FileExists(result.TargetPath))
            result.Blockers.Add($"Target path already exists: {result.TargetPath}");

        // Verify write access to the source directory's parent.
        // Moving a directory requires write/delete permission on the parent.
        if (sourceExists && result.Blockers.Count == 0)
        {
            try
            {
                var parentDir = Path.GetDirectoryName(request.SourcePath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    var testFile = Path.Combine(parentDir, $"_winC2D_preflight_{Guid.NewGuid():N}.tmp");
                    File.WriteAllText(testFile, "winC2D preflight write test");
                    File.Delete(testFile);
                }
            }
            catch (UnauthorizedAccessException)
            {
                result.Blockers.Add(
                    $"Cannot write to '{Path.GetDirectoryName(request.SourcePath)}'. "
                    + "Administrator privileges are required to modify files in this directory. "
                    + "Run the CLI as Administrator (right-click → Run as administrator) or use gsudo.");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Write-access check failed: {ex.Message}");
            }
        }

        return Task.FromResult(result);
    }

    private string DetermineTargetPath(MigrationRequest request)
    {
        var sourceName = _fileSystem.GetFileName(
            request.SourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        var folderName = request.CustomTargetFolderName
            ?? sourceName
            ?? request.Name
            ?? "Unknown";

        foreach (var c in _fileSystem.GetInvalidFileNameChars())
            folderName = folderName.Replace(c, '_');

        folderName = folderName.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(folderName))
            folderName = "MigratedApp";

        return _fileSystem.CombinePath(request.TargetRootPath, folderName);
    }

    private static string NormalizeFullPath(string path)
    {
        var fullPath = Path.GetFullPath(path.Trim());
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsChildPath(string candidateChild, string parent)
    {
        if (string.Equals(candidateChild, parent, StringComparison.OrdinalIgnoreCase))
            return false;

        var parentWithSeparator = parent.EndsWith(Path.DirectorySeparatorChar)
            ? parent
            : parent + Path.DirectorySeparatorChar;

        return candidateChild.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}
