namespace winC2D.Core.Models;

public enum TaskCleanupState
{
    Stale,
    Terminal
}

public sealed class TaskCleanupOptions
{
    public TaskCleanupState State { get; set; }
    public int OlderThanDays { get; set; } = 30;
}
