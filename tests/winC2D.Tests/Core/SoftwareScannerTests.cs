using Xunit;
using FluentAssertions;
using Moq;
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
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<SoftwareScanner>> _loggerMock;
    
    public SoftwareScannerTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SoftwareScanner>>();
    }
    
    [Fact]
    public void GetDefaultScanDirectories_ShouldReturnProgramFiles()
    {
        // Arrange
        var scanner = new SoftwareScanner(_fileSystemMock.Object, _loggerMock.Object);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        _fileSystemMock.Setup(f => f.DirectoryExists(programFiles)).Returns(true);
        
        // Act
        var directories = scanner.GetDefaultScanDirectories();
        
        // Assert
        directories.Should().NotBeEmpty();
        directories.Should().Contain(programFiles);
    }
    
    [Fact]
    public async Task ScanAsync_WithEmptyDirectories_ShouldReturnEmptyList()
    {
        // Arrange
        var scanner = new SoftwareScanner(_fileSystemMock.Object, _loggerMock.Object);
        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        
        // Act
        var result = await scanner.ScanAsync(Array.Empty<string>());
        
        // Assert
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task ScanAsync_WithValidDirectory_ShouldReturnSoftwareList()
    {
        // Arrange
        var scanner = new SoftwareScanner(_fileSystemMock.Object, _loggerMock.Object);
        var testPath = @"C:\Program Files";
        var subDir = @"C:\Program Files\TestApp";
        var testFile = @"C:\Program Files\TestApp\app.exe";
        
        _fileSystemMock.Setup(f => f.DirectoryExists(testPath)).Returns(true);
        // ScanAsync calls GetDirectories on the base directory
        _fileSystemMock.Setup(f => f.GetDirectories(testPath, "*", false))
            .Returns(new[] { subDir });
        _fileSystemMock.Setup(f => f.IsSymlink(subDir)).Returns(false);
        // BuildSoftwareInfoAsync checks for files/dirs (non-recursive) to determine hasEntries
        _fileSystemMock.Setup(f => f.GetFiles(subDir, "*", false))
            .Returns(new[] { testFile });
        _fileSystemMock.Setup(f => f.GetDirectories(subDir, "*", false))
            .Returns(Array.Empty<string>());
        // GetDirectorySizeUntilThresholdAsync calls GetFiles (recursive) and GetFileSize
        _fileSystemMock.Setup(f => f.GetFiles(subDir, "*", true))
            .Returns(new[] { testFile });
        _fileSystemMock.Setup(f => f.GetFileSize(testFile))
            .Returns(1024 * 1024); // 1 MB
        
        // Act
        var result = await scanner.ScanAsync(new[] { testPath });
        
        // Assert
        result.Should().NotBeEmpty();
        result.First().Name.Should().Be("TestApp");
    }
    
    [Fact]
    public async Task CheckSuspiciousAsync_WithEmptyDirectory_ShouldReturnEmptyStatus()
    {
        // Arrange
        var scanner = new SoftwareScanner(_fileSystemMock.Object, _loggerMock.Object);
        var software = new SoftwareInfo
        {
            Name = "TestApp",
            InstallLocation = @"C:\Program Files\TestApp",
            Status = SoftwareStatus.Suspicious
        };
        
        _fileSystemMock.Setup(f => f.DirectoryExists(software.InstallLocation)).Returns(false);
        
        // Act
        var result = await scanner.CheckSuspiciousAsync(software);
        
        // Assert
        result.Status.Should().Be(SoftwareStatus.Residual);
        result.SuspiciousChecked.Should().BeTrue();
    }
}