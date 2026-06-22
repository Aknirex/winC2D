using winC2D.Core.Resilience;

namespace winC2D.Core.Models;

/// <summary>
/// Engine diagnostics including circuit-breaker and resilience state.
/// Designed for monitoring, debugging, and AI agent observability.
/// </summary>
public sealed class EngineDiagnostics
{
    /// <summary>Total active (non-terminal) tasks.</summary>
    public int ActiveTaskCount { get; set; }

    /// <summary>Total tasks known to the engine.</summary>
    public int TotalTaskCount { get; set; }

    /// <summary>File-copy circuit breaker state.</summary>
    public CircuitBreakerState FileCopyCircuitState { get; set; }

    /// <summary>Whether the file-copy circuit breaker is rejecting requests.</summary>
    public bool IsCircuitOpen { get; set; }

    /// <summary>How many tasks are currently in Copying state.</summary>
    public int CopyingTaskCount { get; set; }

    /// <summary>How many tasks are paused.</summary>
    public int PausedTaskCount { get; set; }

    /// <summary>Timestamp of this diagnostic snapshot (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
