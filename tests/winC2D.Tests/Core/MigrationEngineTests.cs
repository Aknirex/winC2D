using Xunit;
using FluentAssertions;
using Moq;
using winC2D.Core.Services;
using winC2D.Core.Models;
using winC2D.Core.FileSystem;
using winC2D.Infrastructure.Services;

namespace winC2D.Tests.Core;

/// <summary>
/// Unit tests for MigrationEngine
/// </summary>
public class MigrationEngineTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ISymlinkManager> _symlinkManagerMock;
    private readonly Mock<IRollbackManager> _rollbackManagerMock;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<MigrationEngine>> _loggerMock;
    
    public MigrationEngineTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _symlinkManagerMock = new Mock<ISymlinkManager>();
        _rollbackManagerMock = new Mock<IRollbackManager>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MigrationEngine>>();
    }
    
    [Fact]
    public async Task CreateTaskAsync_ShouldCreateValidTask()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);
        
        var request = new MigrationRequest
        {
            Type = MigrationType.Software,
            Name = "TestApp",
            SourcePath = @"C:\Program Files\TestApp",
            TargetRootPath = @"D:\MigratedApps"
        };
        
        // Setup file system mocks
        _fileSystemMock.Setup(f => f.DirectoryExists(request.SourcePath)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectorySize(request.SourcePath, default)).Returns(1024 * 1024 * 100); // 100 MB
        _fileSystemMock.Setup(f => f.GetFiles(request.SourcePath, "*", true)).Returns(new[] { "file1.exe", "file2.dll" });
        
        // Act
        var task = await engine.CreateTaskAsync(request);
        
        // Assert
        task.Should().NotBeNull();
        task.Id.Should().NotBeNullOrEmpty();
        task.Name.Should().Be("TestApp");
        task.SourcePath.Should().Be(request.SourcePath);
        task.State.Should().Be(MigrationState.Pending);
    }
    
    [Fact]
    public async Task GetTaskAsync_WhenTaskExists_ShouldReturnTask()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);
        
        var request = new MigrationRequest
        {
            Type = MigrationType.Software,
            Name = "TestApp",
            SourcePath = @"C:\Program Files\TestApp",
            TargetRootPath = @"D:\MigratedApps"
        };
        
        _fileSystemMock.Setup(f => f.DirectoryExists(request.SourcePath)).Returns(true);
        _fileSystemMock.Setup(f => f.GetDirectorySize(request.SourcePath, default)).Returns(1024);
        _fileSystemMock.Setup(f => f.GetFiles(request.SourcePath, "*", true)).Returns(Array.Empty<string>());
        
        var createdTask = await engine.CreateTaskAsync(request);
        
        // Act
        var retrievedTask = await engine.GetTaskAsync(createdTask.Id);
        
        // Assert
        retrievedTask.Should().NotBeNull();
        retrievedTask!.Id.Should().Be(createdTask.Id);
    }
    
    [Fact]
    public async Task GetTaskAsync_WhenTaskDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);
        
        // Act
        var task = await engine.GetTaskAsync("non-existent-id");
        
        // Assert
        task.Should().BeNull();
    }
}