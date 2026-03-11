using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using winC2D.Core.Services;
using winC2D.Core.Models;
using winC2D.Core.FileSystem;
using winC2D.Infrastructure.Services;

namespace winC2D.Tests.Core;

/// <summary>
/// Unit tests for SoftwareScanner
/// </summary>
public class SoftwareScannerTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ISizeCacheService> _cacheMock;
    private readonly Mock<ILogger<SoftwareScanner>> _loggerMock;

    public SoftwareScannerTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _cacheMock      = new Mock<ISizeCacheService>();
        _loggerMock     = new Mock<ILogger<SoftwareScanner>>();

        // By default cache misses so scanner always performs a full calculation
        SizeCacheEntry dummy = default!;
        _cacheMock.Setup(c => c.TryGet(It.IsAny<string>(), out dummy)).Returns(false);
    }

    private SoftwareScanner CreateScanner() =>
        new SoftwareScanner(_fileSystemMock.Object, _cacheMock.Object, _loggerMock.Object);

    // ── GetDefaultScanDirectories ─────────────────────────────────────────

    [Fact]
    public void GetDefaultScanDirectories_ShouldReturnProgramFiles()
    {
        var scanner      = CreateScanner();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        _fileSystemMock.Setup(f => f.DirectoryExists(programFiles)).Returns(true);

        var dirs = scanner.GetDefaultScanDirectories();

        dirs.Should().NotBeEmpty();
        dirs.Should().Contain(programFiles);
    }

    // ── ScanStreamAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ScanStreamAsync_WithNoDirectories_ShouldYieldNoItems()
    {
        var scanner = CreateScanner();

        var items = new List<SoftwareInfo>();
        await foreach (var item in scanner.ScanStreamAsync(Array.Empty<string>()))
            items.Add(item);

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanStreamAsync_WithValidDirectory_ShouldYieldItems()
    {
        var scanner  = CreateScanner();
        var baseDir  = @"C:\Program Files";
        var subDir   = @"C:\Program Files\TestApp";
        var exeFile  = @"C:\Program Files\TestApp\app.exe";

        _fileSystemMock.Setup(f => f.DirectoryExists(baseDir)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectories(baseDir, "*", false))
                       .Returns(new[] { subDir });
        _fileSystemMock.Setup(f => f.IsSymlink(subDir)).Returns(false);
        _fileSystemMock.Setup(f => f.GetFiles(subDir, "*", true))
                       .Returns(new[] { exeFile });
        _fileSystemMock.Setup(f => f.GetFileSize(exeFile)).Returns(5 * 1024 * 1024); // 5 MB
        _fileSystemMock.Setup(f => f.GetDirectories(subDir, "*", false))
                       .Returns(Array.Empty<string>());

        var items = new List<SoftwareInfo>();
        await foreach (var item in scanner.ScanStreamAsync(new[] { baseDir }))
            items.Add(item);

        items.Should().HaveCount(1);
        items[0].Name.Should().Be("TestApp");
        items[0].Status.Should().Be(SoftwareStatus.Normal);
        items[0].SuspiciousChecked.Should().BeTrue();
    }

    [Fact]
    public async Task ScanStreamAsync_SymlinkDirectory_ShouldBeMarkedAsMigrated()
    {
        var scanner = CreateScanner();
        var baseDir = @"C:\Program Files";
        var subDir  = @"C:\Program Files\MigratedApp";

        _fileSystemMock.Setup(f => f.DirectoryExists(baseDir)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectories(baseDir, "*", false))
                       .Returns(new[] { subDir });
        _fileSystemMock.Setup(f => f.IsSymlink(subDir)).Returns(true);

        var items = new List<SoftwareInfo>();
        await foreach (var item in scanner.ScanStreamAsync(new[] { baseDir }))
            items.Add(item);

        items.Should().HaveCount(1);
        items[0].Status.Should().Be(SoftwareStatus.Migrated);
        items[0].IsSymlink.Should().BeTrue();
    }

    [Fact]
    public async Task ScanStreamAsync_EmptyDirectory_ShouldBeMarkedAsEmpty()
    {
        var scanner = CreateScanner();
        var baseDir = @"C:\Program Files";
        var subDir  = @"C:\Program Files\EmptyApp";

        _fileSystemMock.Setup(f => f.DirectoryExists(baseDir)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectories(baseDir, "*", false))
                       .Returns(new[] { subDir });
        _fileSystemMock.Setup(f => f.IsSymlink(subDir)).Returns(false);
        _fileSystemMock.Setup(f => f.GetFiles(subDir, "*", true))
                       .Returns(Array.Empty<string>());
        _fileSystemMock.Setup(f => f.GetDirectories(subDir, "*", false))
                       .Returns(Array.Empty<string>());

        var items = new List<SoftwareInfo>();
        await foreach (var item in scanner.ScanStreamAsync(new[] { baseDir }))
            items.Add(item);

        items[0].Status.Should().Be(SoftwareStatus.Empty);
    }

    [Fact]
    public async Task ScanStreamAsync_NoExeFound_ShouldBeMarkedAsResidual()
    {
        var scanner  = CreateScanner();
        var baseDir  = @"C:\Program Files";
        var subDir   = @"C:\Program Files\LeftoverApp";
        var dataFile = @"C:\Program Files\LeftoverApp\data.dat";

        _fileSystemMock.Setup(f => f.DirectoryExists(baseDir)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectories(baseDir, "*", false))
                       .Returns(new[] { subDir });
        _fileSystemMock.Setup(f => f.IsSymlink(subDir)).Returns(false);
        _fileSystemMock.Setup(f => f.GetFiles(subDir, "*", true))
                       .Returns(new[] { dataFile });
        _fileSystemMock.Setup(f => f.GetFileSize(dataFile)).Returns(1024);
        _fileSystemMock.Setup(f => f.GetDirectories(subDir, "*", false))
                       .Returns(Array.Empty<string>());

        var items = new List<SoftwareInfo>();
        await foreach (var item in scanner.ScanStreamAsync(new[] { baseDir }))
            items.Add(item);

        items[0].Status.Should().Be(SoftwareStatus.Residual);
    }

    // ── RecalculateSizeAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RecalculateSizeAsync_MissingDirectory_ShouldMarkAsResidual()
    {
        var scanner  = CreateScanner();
        var software = new SoftwareInfo
        {
            Name            = "GhostApp",
            InstallLocation = @"C:\Program Files\GhostApp",
            Status          = SoftwareStatus.Normal
        };

        _fileSystemMock.Setup(f => f.IsSymlink(software.InstallLocation)).Returns(false);
        _fileSystemMock.Setup(f => f.DirectoryExists(software.InstallLocation)).Returns(false);

        var result = await scanner.RecalculateSizeAsync(software);

        result.Status.Should().Be(SoftwareStatus.Residual);
        result.SuspiciousChecked.Should().BeTrue();
    }

    [Fact]
    public async Task RecalculateSizeAsync_Symlink_ShouldMarkAsMigrated()
    {
        var scanner  = CreateScanner();
        var software = new SoftwareInfo
        {
            Name            = "MigratedApp",
            InstallLocation = @"C:\Program Files\MigratedApp"
        };

        _fileSystemMock.Setup(f => f.IsSymlink(software.InstallLocation)).Returns(true);

        var result = await scanner.RecalculateSizeAsync(software);

        result.Status.Should().Be(SoftwareStatus.Migrated);
        result.IsSymlink.Should().BeTrue();
    }

    [Fact]
    public async Task RecalculateSizeAsync_ShouldWriteToCache()
    {
        var scanner  = CreateScanner();
        var path     = @"C:\Program Files\CachedApp";
        var software = new SoftwareInfo { Name = "CachedApp", InstallLocation = path };
        var exeFile  = path + @"\app.exe";

        _fileSystemMock.Setup(f => f.IsSymlink(path)).Returns(false);
        _fileSystemMock.Setup(f => f.DirectoryExists(path)).Returns(true);
        _fileSystemMock.Setup(f => f.GetFiles(path, "*", true)).Returns(new[] { exeFile });
        _fileSystemMock.Setup(f => f.GetFileSize(exeFile)).Returns(20 * 1024 * 1024);

        await scanner.RecalculateSizeAsync(software);

        _cacheMock.Verify(c => c.Set(path, It.IsAny<long>()), Times.Once);
    }
}
