using winC2D.Core.Models;

namespace winC2D.Core.Services;

public interface IMigrationTaskStore
{
    Task<MigrationTask?> GetAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MigrationTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(MigrationTask task, bool immediate = true, CancellationToken cancellationToken = default);
    Task SaveProgressAsync(MigrationTask task, CancellationToken cancellationToken = default);
    Task RefreshHeartbeatAsync(string taskId, CancellationToken cancellationToken = default);
    Task RequestPauseAsync(string taskId, CancellationToken cancellationToken = default);
    Task RequestResumeAsync(string taskId, CancellationToken cancellationToken = default);
    Task RequestCancelAsync(string taskId, bool rollback, CancellationToken cancellationToken = default);
    bool IsStale(MigrationTask task, DateTime utcNow);
    string GetStaleReason(MigrationTask task, DateTime utcNow);
    bool IsWorkerAlive(MigrationTask task);
    Task<int> CleanupAsync(TaskCleanupOptions options, CancellationToken cancellationToken = default);
}
