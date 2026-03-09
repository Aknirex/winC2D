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
        
        // Act
        var directories = scanner.GetDefaultScanDirectories();
        
        // Assert
        directories.Should().NotBeEmpty();
        directories.Should().Contain(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
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
        
        _fileSystemMock.Setup(f => f.DirectoryExists(testPath)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectories(testPath, "*", false))
            .Returns(new[] { @"C:\Program Files\TestApp" });
        _fileSystemMock.Setup(f => f.IsSymlink(It.IsAny<string>())).Returns(false);
        _fileSystemMock.Setup(f => f.GetFiles(It.IsAny<string>(), "*", false))
            .Returns(Array.Empty<string>());
        _fileSystemMock.Setup(f => f.GetDirectories(It.IsAny<string>(), "*", false))
            .Returns(Array.Empty<string>());
        _fileSystemMock.Setup(f => f.GetDirectorySize(It.IsAny<string>(), default))
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