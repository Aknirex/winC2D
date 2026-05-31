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
public class MigrationEngineTests : IDisposable
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ISymlinkManager> _symlinkManagerMock;
    private readonly Mock<IRollbackManager> _rollbackManagerMock;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<MigrationEngine>> _loggerMock;
    private readonly string _localAppDataRoot;
    
    public MigrationEngineTests()
    {
        _localAppDataRoot = Path.Combine(Path.GetTempPath(), "winC2D-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_localAppDataRoot);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _localAppDataRoot);

        _fileSystemMock = new Mock<IFileSystem>();
        _symlinkManagerMock = new Mock<ISymlinkManager>();
        _rollbackManagerMock = new Mock<IRollbackManager>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MigrationEngine>>();

        _fileSystemMock
            .Setup(f => f.GetInvalidFileNameChars())
            .Returns(Path.GetInvalidFileNameChars());
        _fileSystemMock
            .Setup(f => f.GetFileName(It.IsAny<string>()))
            .Returns((string path) => Path.GetFileName(path));
        _fileSystemMock
            .Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        _rollbackManagerMock
            .Setup(r => r.SetBackupPathAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _rollbackManagerMock
            .Setup(r => r.SetTempTargetPathAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _rollbackManagerMock
            .Setup(r => r.RecordStepAsync(It.IsAny<string>(), It.IsAny<CompletedStep>()))
            .Returns(Task.CompletedTask);
        _rollbackManagerMock
            .Setup(r => r.DeleteRollbackPointAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
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

    [Fact]
    public async Task ExecuteAsync_ShouldCopyToTemporaryTargetBeforeFinalizing()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);

        var source = @"C:\Program Files\TestApp";
        var targetRoot = @"D:\MigratedApps";
        var target = @"D:\MigratedApps\TestApp";
        var sourceFile = @"C:\Program Files\TestApp_migrating_123\app.exe";
        var rollbackPoint = new RollbackPoint { Id = "rp-1" };

        _fileSystemMock.Setup(f => f.GetDirectorySize(source, default)).Returns(100);
        _fileSystemMock.Setup(f => f.GetFiles(source, "*", true)).Returns(new[] { @"C:\Program Files\TestApp\app.exe" });
        _fileSystemMock.Setup(f => f.DirectoryExists(source)).Returns(true);
        _fileSystemMock.Setup(f => f.DirectoryExists(target)).Returns(false);
        _fileSystemMock.Setup(f => f.FileExists(target)).Returns(false);
        _fileSystemMock.Setup(f => f.IsSymlink(source)).Returns(false);
        _fileSystemMock.Setup(f => f.GetFiles(It.Is<string>(p => p.Contains("_migrating_")), "*", false))
            .Returns(new[] { sourceFile });
        _fileSystemMock.Setup(f => f.GetDirectories(It.Is<string>(p => p.Contains("_migrating_")), "*", false))
            .Returns(Array.Empty<string>());
        _fileSystemMock.Setup(f => f.GetFileSize(It.IsAny<string>())).Returns(100);

        _rollbackManagerMock.Setup(r => r.CreateRollbackPointAsync(It.IsAny<MigrationTask>()))
            .ReturnsAsync(rollbackPoint);
        _symlinkManagerMock.Setup(s => s.CreateDirectorySymlinkAsync(source, target))
            .ReturnsAsync(true);

        var task = await engine.CreateTaskAsync(new MigrationRequest
        {
            Type = MigrationType.Software,
            Name = "TestApp",
            SourcePath = source,
            TargetRootPath = targetRoot
        });

        // Act
        var result = await engine.ExecuteAsync(task);

        // Assert
        result.Success.Should().BeTrue();
        task.TempTargetPath.Should().NotBeNull();
        task.TempTargetPath.Should().Contain("_copying_");

        _fileSystemMock.Verify(f => f.MoveDirectory(source, It.Is<string>(p => p.Contains("_migrating_"))), Times.Once);
        _fileSystemMock.Verify(f => f.CopyFilePreserveMetadata(
            sourceFile,
            It.Is<string>(p => p.Contains("_copying_") && p.EndsWith("app.exe")),
            false), Times.Once);
        _fileSystemMock.Verify(f => f.MoveDirectory(It.Is<string>(p => p.Contains("_copying_")), target), Times.Once);
        _rollbackManagerMock.Verify(r => r.RecordStepAsync(rollbackPoint.Id, CompletedStep.TempFilesCopied), Times.Once);
        _rollbackManagerMock.Verify(r => r.RecordStepAsync(rollbackPoint.Id, CompletedStep.TargetFinalized), Times.Once);
        _rollbackManagerMock.Verify(r => r.RecordStepAsync(rollbackPoint.Id, CompletedStep.BackupDeleted), Times.Once);
        _rollbackManagerMock.Verify(r => r.RecordStepAsync(rollbackPoint.Id, CompletedStep.FilesCopied), Times.Never);
        _rollbackManagerMock.Verify(r => r.DeleteRollbackPointAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetInsideSource_ShouldFailBeforeMovingSource()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);

        var source = @"C:\Data";
        var targetRoot = @"C:\Data\Migrated";

        _fileSystemMock.Setup(f => f.GetDirectorySize(source, default)).Returns(100);
        _fileSystemMock.Setup(f => f.GetFiles(source, "*", true)).Returns(new[] { @"C:\Data\file.txt" });
        _fileSystemMock.Setup(f => f.DirectoryExists(source)).Returns(true);
        _fileSystemMock.Setup(f => f.IsSymlink(source)).Returns(false);

        var task = await engine.CreateTaskAsync(new MigrationRequest
        {
            Type = MigrationType.Generic,
            Name = "Data",
            SourcePath = source,
            TargetRootPath = targetRoot
        });

        // Act
        var result = await engine.ExecuteAsync(task);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inside the source");
        _fileSystemMock.Verify(f => f.MoveDirectory(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _rollbackManagerMock.Verify(r => r.CreateRollbackPointAsync(It.IsAny<MigrationTask>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSymlinkCreationFails_ShouldRollbackFinalizedTarget()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);

        var source = @"C:\Program Files\TestApp";
        var targetRoot = @"D:\MigratedApps";
        var target = @"D:\MigratedApps\TestApp";
        var rollbackPoint = new RollbackPoint { Id = "rp-2" };

        _fileSystemMock.Setup(f => f.GetDirectorySize(source, default)).Returns(100);
        _fileSystemMock.Setup(f => f.GetFiles(source, "*", true)).Returns(new[] { @"C:\Program Files\TestApp\app.exe" });
        _fileSystemMock.Setup(f => f.DirectoryExists(source)).Returns(true);
        _fileSystemMock.Setup(f => f.DirectoryExists(target)).Returns(false);
        _fileSystemMock.Setup(f => f.FileExists(target)).Returns(false);
        _fileSystemMock.Setup(f => f.IsSymlink(source)).Returns(false);
        _fileSystemMock.Setup(f => f.GetFiles(It.Is<string>(p => p.Contains("_migrating_")), "*", false))
            .Returns(new[] { @"C:\Program Files\TestApp_migrating_123\app.exe" });
        _fileSystemMock.Setup(f => f.GetDirectories(It.Is<string>(p => p.Contains("_migrating_")), "*", false))
            .Returns(Array.Empty<string>());
        _fileSystemMock.Setup(f => f.GetFileSize(It.IsAny<string>())).Returns(100);

        _rollbackManagerMock.Setup(r => r.CreateRollbackPointAsync(It.IsAny<MigrationTask>()))
            .ReturnsAsync(rollbackPoint);
        _rollbackManagerMock.Setup(r => r.RollbackAsync(rollbackPoint.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RollbackResult { Success = true });
        _symlinkManagerMock.Setup(s => s.CreateDirectorySymlinkAsync(source, target))
            .ReturnsAsync(false);

        var task = await engine.CreateTaskAsync(new MigrationRequest
        {
            Type = MigrationType.Software,
            Name = "TestApp",
            SourcePath = source,
            TargetRootPath = targetRoot
        });

        // Act
        var result = await engine.ExecuteAsync(task);

        // Assert
        result.Success.Should().BeFalse();
        result.FinalState.Should().Be(MigrationState.RolledBack);
        result.WasRolledBack.Should().BeTrue();
        _rollbackManagerMock.Verify(r => r.RecordStepAsync(rollbackPoint.Id, CompletedStep.TargetFinalized), Times.Once);
        _rollbackManagerMock.Verify(r => r.RollbackAsync(rollbackPoint.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_WhenRollbackSucceeds_ShouldUpdateTaskState()
    {
        // Arrange
        var engine = new MigrationEngine(
            _fileSystemMock.Object,
            _symlinkManagerMock.Object,
            _rollbackManagerMock.Object,
            _loggerMock.Object);

        var source = @"C:\Program Files\TestApp";
        _fileSystemMock.Setup(f => f.GetDirectorySize(source, default)).Returns(100);
        _fileSystemMock.Setup(f => f.GetFiles(source, "*", true)).Returns(new[] { @"C:\Program Files\TestApp\app.exe" });

        var task = await engine.CreateTaskAsync(new MigrationRequest
        {
            Type = MigrationType.Software,
            Name = "TestApp",
            SourcePath = source,
            TargetRootPath = @"D:\MigratedApps"
        });
        task.State = MigrationState.Completed;
        task.RollbackPoint = new RollbackPoint { Id = "rp-3", TaskId = task.Id };

        _rollbackManagerMock.Setup(r => r.RollbackAsync("rp-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RollbackResult { Success = true });

        // Act
        var result = await engine.RollbackAsync(task.Id);

        // Assert
        result.Success.Should().BeTrue();
        task.State.Should().Be(MigrationState.RolledBack);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_localAppDataRoot, true);
        }
        catch
        {
            // best effort cleanup
        }
    }
}
