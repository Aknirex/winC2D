using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using winC2D.Cli;
using winC2D.Core.Models;
using winC2D.Core.Services;
using Xunit;

namespace winC2D.Tests.Cli;

public class CliApplicationTests
{
    [Fact]
    public async Task PrivilegeStatus_ShouldWriteSingleJsonObject()
    {
        var result = await RunCliAsync(["privilege-status"]);

        result.ExitCode.Should().Be((int)CliExitCode.Success);
        result.Lines.Should().HaveCount(1);
        result.Root.GetProperty("success").GetBoolean().Should().BeTrue();
        result.Root.GetProperty("privilegeLevel").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnknownOption_ShouldReturnArgumentErrorJson()
    {
        var result = await RunCliAsync(["list", "--wat"]);

        result.ExitCode.Should().Be((int)CliExitCode.ArgumentError);
        result.Root.GetProperty("success").GetBoolean().Should().BeFalse();
        result.Root.GetProperty("error").GetString().Should().Be("ARGUMENT_ERROR");
    }

    [Fact]
    public async Task MigrateWithoutYes_ShouldReturnArgumentErrorBeforeWriteOperation()
    {
        var result = await RunCliAsync([
            "migrate",
            "--source", @"C:\Program Files\TestApp",
            "--target", @"D:\Program Files"
        ]);

        result.ExitCode.Should().Be((int)CliExitCode.ArgumentError);
        result.Root.GetProperty("error").GetString().Should().Be("CONFIRMATION_REQUIRED");
    }

    [Fact]
    public async Task MigrateDryRun_WhenSourceMissing_ShouldReturnValidationJson()
    {
        var engine = new Mock<IMigrationEngine>();
        engine.Setup(e => e.ValidateAsync(It.IsAny<MigrationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationPreflightResult
            {
                SourcePath = @"C:\Program Files\MissingApp",
                TargetPath = @"D:\Program Files\MissingApp",
                Blockers = ["Source path does not exist: C:\\Program Files\\MissingApp"]
            });

        var services = new ServiceCollection()
            .AddSingleton(engine.Object)
            .BuildServiceProvider();

        var result = await RunCliAsync([
            "migrate",
            "--source", @"C:\Program Files\MissingApp",
            "--target", @"D:\Program Files",
            "--dry-run"
        ], services);

        result.ExitCode.Should().Be((int)CliExitCode.BusinessFailure);
        result.Root.GetProperty("success").GetBoolean().Should().BeFalse();
        result.Root.GetProperty("dryRun").GetBoolean().Should().BeTrue();
        result.Root.GetProperty("blockers").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Status_WhenTaskMissing_ShouldReturnTaskNotFound()
    {
        var engine = new Mock<IMigrationEngine>();
        engine.Setup(e => e.GetTaskAsync("missing")).ReturnsAsync((MigrationTask?)null);

        var services = new ServiceCollection()
            .AddSingleton(engine.Object)
            .BuildServiceProvider();

        var result = await RunCliAsync(["status", "--task-id", "missing"], services);

        result.ExitCode.Should().Be((int)CliExitCode.TaskNotFound);
        result.Root.GetProperty("error").GetString().Should().Be("TASK_NOT_FOUND");
    }

    [Fact]
    public async Task Status_WhenTaskIsStale_ShouldIncludeStaleReason()
    {
        var task = new MigrationTask
        {
            Id = "stale",
            Name = "Old Pending",
            State = MigrationState.Pending,
            SourcePath = @"C:\A",
            TargetPath = @"D:\Program Files\A",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var engine = new Mock<IMigrationEngine>();
        engine.Setup(e => e.GetTaskAsync("stale")).ReturnsAsync(task);
        var store = new Mock<IMigrationTaskStore>();
        store.Setup(s => s.IsStale(task, It.IsAny<DateTime>())).Returns(true);
        store.Setup(s => s.GetStaleReason(task, It.IsAny<DateTime>())).Returns("No worker process was recorded.");

        var services = new ServiceCollection()
            .AddSingleton(engine.Object)
            .AddSingleton(store.Object)
            .BuildServiceProvider();

        var result = await RunCliAsync(["status", "--task-id", "stale"], services);

        result.ExitCode.Should().Be((int)CliExitCode.Success);
        result.Root.GetProperty("isStale").GetBoolean().Should().BeTrue();
        result.Root.GetProperty("staleReason").GetString().Should().Be("No worker process was recorded.");
    }

    [Fact]
    public async Task Status_WhenTaskWasCancelled_ShouldNotReportTaskSuccess()
    {
        var engine = new Mock<IMigrationEngine>();
        engine.Setup(e => e.GetTaskAsync("cancelled")).ReturnsAsync(new MigrationTask
        {
            Id = "cancelled",
            Name = "Cancelled",
            State = MigrationState.Cancelled,
            SourcePath = @"C:\A",
            TargetPath = @"D:\Program Files\A",
            CreatedAt = DateTime.UtcNow
        });

        var services = new ServiceCollection()
            .AddSingleton(engine.Object)
            .BuildServiceProvider();

        var result = await RunCliAsync(["status", "--task-id", "cancelled"], services);

        result.ExitCode.Should().Be((int)CliExitCode.Success);
        result.Root.GetProperty("success").GetBoolean().Should().BeFalse();
        result.Root.GetProperty("state").GetString().Should().Be(nameof(MigrationState.Cancelled));
    }

    [Fact]
    public async Task List_ShouldFilterCompletedTasks()
    {
        var engine = new Mock<IMigrationEngine>();
        engine.Setup(e => e.GetAllTasksAsync()).ReturnsAsync([
            new MigrationTask
            {
                Id = "completed",
                Name = "Done",
                State = MigrationState.Completed,
                SourcePath = @"C:\A",
                TargetPath = @"D:\MigratedApps\A",
                CreatedAt = DateTime.UtcNow
            },
            new MigrationTask
            {
                Id = "running",
                Name = "Running",
                State = MigrationState.Copying,
                SourcePath = @"C:\B",
                TargetPath = @"D:\MigratedApps\B",
                CreatedAt = DateTime.UtcNow
            }
        ]);

        var services = new ServiceCollection()
            .AddSingleton(engine.Object)
            .BuildServiceProvider();

        var result = await RunCliAsync(["list", "--state", "completed"], services);

        result.ExitCode.Should().Be((int)CliExitCode.Success);
        result.Root.GetProperty("count").GetInt32().Should().Be(1);
        result.Root.GetProperty("tasks")[0].GetProperty("taskId").GetString().Should().Be("completed");
    }

    [Fact]
    public async Task ListStale_ShouldIncludeStaleReason()
    {
        var task = new MigrationTask
        {
            Id = "stale",
            Name = "Old Pending",
            State = MigrationState.Pending,
            SourcePath = @"C:\A",
            TargetPath = @"D:\Program Files\A",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var engine = new Mock<IMigrationEngine>();
        engine.Setup(e => e.GetAllTasksAsync()).ReturnsAsync([task]);
        var store = new Mock<IMigrationTaskStore>();
        store.Setup(s => s.IsStale(task, It.IsAny<DateTime>())).Returns(true);
        store.Setup(s => s.GetStaleReason(task, It.IsAny<DateTime>())).Returns("No worker process was recorded.");

        var services = new ServiceCollection()
            .AddSingleton(engine.Object)
            .AddSingleton(store.Object)
            .BuildServiceProvider();

        var result = await RunCliAsync(["list", "--state", "stale"], services);

        result.ExitCode.Should().Be((int)CliExitCode.Success);
        result.Root.GetProperty("count").GetInt32().Should().Be(1);
        var listed = result.Root.GetProperty("tasks")[0];
        listed.GetProperty("taskId").GetString().Should().Be("stale");
        listed.GetProperty("isStale").GetBoolean().Should().BeTrue();
        listed.GetProperty("staleReason").GetString().Should().Be("No worker process was recorded.");
    }

    [Fact]
    public async Task BuiltCliProcess_ShouldSupportPrivilegeStatusWithRedirectedStdout()
    {
        var repoRoot = FindRepoRoot();
        var debugExe = Path.Combine(repoRoot, "winC2D.Cli", "bin", "Debug", "net8.0-windows", "winC2D.Cli.exe");
        var releaseExe = Path.Combine(repoRoot, "winC2D.Cli", "bin", "Release", "net8.0-windows", "winC2D.Cli.exe");
#if DEBUG
        var candidates = new[] { debugExe, releaseExe };
#else
        var candidates = new[] { releaseExe, debugExe };
#endif
        var exe = candidates.FirstOrDefault(File.Exists);
        if (exe is null)
            return;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("privilege-status");

        process.Start().Should().BeTrue();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var completed = await Task.Run(() => process.WaitForExit(15_000));
        completed.Should().BeTrue("the CLI smoke test should complete quickly");

        var stdout = (await stdoutTask).Trim();
        var stderr = await stderrTask;
        process.ExitCode.Should().Be((int)CliExitCode.Success, stderr);
        stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(1);

        using var document = JsonDocument.Parse(stdout);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("privilegeLevel").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<CliRunResult> RunCliAsync(string[] args, IServiceProvider? services = null)
    {
        services ??= new ServiceCollection().BuildServiceProvider();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(args, services, stdout, stderr);
        var text = stdout.ToString().Trim();
        var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        using var document = JsonDocument.Parse(text);

        return new CliRunResult(exitCode, document.RootElement.Clone(), lines, stderr.ToString());
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "winC2D.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed record CliRunResult(int ExitCode, JsonElement Root, string[] Lines, string Stderr);
}
