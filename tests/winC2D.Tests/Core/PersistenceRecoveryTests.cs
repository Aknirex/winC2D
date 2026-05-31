using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using winC2D.Core.FileSystem;
using winC2D.Core.Models;
using winC2D.Core.Services;
using winC2D.Infrastructure.Services;
using Xunit;

namespace winC2D.Tests.Core;

public class PersistenceRecoveryTests : IDisposable
{
    private readonly string _localAppDataRoot;

    public PersistenceRecoveryTests()
    {
        _localAppDataRoot = Path.Combine(Path.GetTempPath(), "winC2D-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_localAppDataRoot);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _localAppDataRoot);
    }

    [Fact]
    public async Task MigrationEngine_ShouldReloadPersistedTasks_OnNewInstance()
    {
        var fileSystem = new Mock<IFileSystem>();
        var symlinkManager = new Mock<ISymlinkManager>();
        var rollbackManager = new Mock<IRollbackManager>();
        var logger = new Mock<ILogger<MigrationEngine>>();

        var request = new MigrationRequest
        {
            Type = MigrationType.Software,
            Name = "TestApp",
            SourcePath = @"C:\Program Files\TestApp",
            TargetRootPath = @"D:\MigratedApps"
        };

        fileSystem.Setup(f => f.GetDirectorySize(request.SourcePath, default)).Returns(2048);
        fileSystem.Setup(f => f.GetFiles(request.SourcePath, "*", true)).Returns(new[] { "a.exe" });
        fileSystem.Setup(f => f.GetInvalidFileNameChars()).Returns(Path.GetInvalidFileNameChars());
        fileSystem.Setup(f => f.GetFileName(request.SourcePath)).Returns("TestApp");
        fileSystem.Setup(f => f.CombinePath(request.TargetRootPath, "TestApp")).Returns(@"D:\MigratedApps\TestApp");

        var engine1 = new MigrationEngine(fileSystem.Object, symlinkManager.Object, rollbackManager.Object, logger.Object);
        var created = await engine1.CreateTaskAsync(request);

        var engine2 = new MigrationEngine(fileSystem.Object, symlinkManager.Object, rollbackManager.Object, logger.Object);
        var reloaded = await engine2.GetTaskAsync(created.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be("TestApp");
        reloaded.SourcePath.Should().Be(request.SourcePath);
        reloaded.TargetPath.Should().Be(@"D:\MigratedApps\TestApp");
    }

    [Fact]
    public async Task RollbackManager_ShouldReloadPersistedRollbackPoints_OnNewInstance()
    {
        var fileSystem = new Mock<IFileSystem>();
        var symlinkManager = new Mock<ISymlinkManager>();
        var logger = new Mock<ILogger<RollbackManager>>();

        var task = new MigrationTask
        {
            Id = Guid.NewGuid().ToString(),
            Type = MigrationType.Software,
            Name = "PersistedApp",
            SourcePath = @"C:\Program Files\PersistedApp",
            TargetPath = @"D:\MigratedApps\PersistedApp"
        };

        var manager1 = new RollbackManager(fileSystem.Object, symlinkManager.Object, logger.Object);
        var point = await manager1.CreateRollbackPointAsync(task);
        await manager1.RecordStepAsync(point.Id, CompletedStep.SourceRenamed);
        await manager1.SetBackupPathAsync(point.Id, @"C:\Program Files\PersistedApp_migrating_123");
        await manager1.SetTempTargetPathAsync(point.Id, @"D:\MigratedApps\PersistedApp_copying_123");

        var manager2 = new RollbackManager(fileSystem.Object, symlinkManager.Object, logger.Object);
        var reloaded = await manager2.GetRollbackPointAsync(point.Id);

        reloaded.Should().NotBeNull();
        reloaded!.TaskId.Should().Be(task.Id);
        reloaded.BackupPath.Should().Be(@"C:\Program Files\PersistedApp_migrating_123");
        reloaded.TempTargetPath.Should().Be(@"D:\MigratedApps\PersistedApp_copying_123");
        reloaded.CompletedSteps.Should().Contain(CompletedStep.SourceRenamed);
    }

    [Fact]
    public async Task RollbackManager_ShouldRestoreCompletedMigration_FromTargetWhenBackupWasDeleted()
    {
        var fileSystem = new Mock<IFileSystem>();
        var symlinkManager = new Mock<ISymlinkManager>();
        var logger = new Mock<ILogger<RollbackManager>>();

        var source = @"C:\Program Files\CompletedApp";
        var target = @"D:\MigratedApps\CompletedApp";
        var tempTarget = @"D:\MigratedApps\CompletedApp_copying_123";
        var targetFile = @"D:\MigratedApps\CompletedApp\app.exe";

        fileSystem.SetupSequence(f => f.DirectoryExists(source))
            .Returns(true)
            .Returns(false);
        fileSystem.Setup(f => f.DirectoryExists(target)).Returns(true);
        fileSystem.Setup(f => f.DirectoryExists(tempTarget)).Returns(false);
        fileSystem.Setup(f => f.GetFiles(target, "*", false)).Returns(new[] { targetFile });
        fileSystem.Setup(f => f.GetDirectories(target, "*", false)).Returns(Array.Empty<string>());
        fileSystem.Setup(f => f.GetFileName(targetFile)).Returns("app.exe");
        fileSystem.Setup(f => f.CombinePath(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        symlinkManager.Setup(s => s.IsSymlink(source)).Returns(true);
        symlinkManager.Setup(s => s.DeleteSymlinkAsync(source)).ReturnsAsync(true);

        var task = new MigrationTask
        {
            Id = Guid.NewGuid().ToString(),
            Type = MigrationType.Software,
            Name = "CompletedApp",
            SourcePath = source,
            TargetPath = target,
            TempTargetPath = tempTarget
        };

        var manager = new RollbackManager(fileSystem.Object, symlinkManager.Object, logger.Object);
        var point = await manager.CreateRollbackPointAsync(task);
        await manager.SetBackupPathAsync(point.Id, source + "_migrating_123");
        await manager.SetTempTargetPathAsync(point.Id, tempTarget);
        await manager.RecordStepAsync(point.Id, CompletedStep.SourceRenamed);
        await manager.RecordStepAsync(point.Id, CompletedStep.TempFilesCopied);
        await manager.RecordStepAsync(point.Id, CompletedStep.TargetFinalized);
        await manager.RecordStepAsync(point.Id, CompletedStep.SymlinkCreated);
        await manager.RecordStepAsync(point.Id, CompletedStep.BackupDeleted);

        var result = await manager.RollbackAsync(point.Id);

        result.Success.Should().BeTrue();
        fileSystem.Verify(f => f.CopyFilePreserveMetadata(
            targetFile,
            It.Is<string>(p => p.Contains("_rollback_") && p.EndsWith("app.exe")),
            false), Times.Once);
        fileSystem.Verify(f => f.MoveDirectory(It.Is<string>(p => p.Contains("_rollback_")), source), Times.Once);
        fileSystem.Verify(f => f.DeleteDirectory(target, true), Times.Once);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_localAppDataRoot, true);
        }
        catch
        {
            // best effort cleanup for temp test folder
        }
    }
}
