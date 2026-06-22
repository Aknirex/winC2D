namespace winC2D.Core.Resilience;

/// <summary>
/// Exponential backoff retry policy with optional circuit breaking.
/// Used for file-system operations that may fail transiently (locked files,
/// transient I/O errors, anti-virus interference).
/// </summary>
public sealed class RetryPolicy
{
    private readonly RetryPolicyOptions _options;
    private int _consecutiveFailures;
    private DateTime? _circuitOpenUntil;

    public RetryPolicy(RetryPolicyOptions? options = null)
    {
        _options = options ?? new RetryPolicyOptions();
    }

    /// <summary>
    /// Whether the circuit breaker is currently open (rejecting requests).
    /// </summary>
    public bool IsCircuitOpen
    {
        get
        {
            if (_circuitOpenUntil == null)
                return false;

            if (DateTime.UtcNow >= _circuitOpenUntil.Value)
            {
                // Circuit half-open: allow one trial request
                _circuitOpenUntil = null;
                return false;
            }

            return true;
        }
    }

    public CircuitBreakerState GetCircuitState()
    {
        if (_circuitOpenUntil == null)
            return CircuitBreakerState.Closed;

        if (DateTime.UtcNow >= _circuitOpenUntil.Value)
            return CircuitBreakerState.HalfOpen;

        return CircuitBreakerState.Open;
    }

    /// <summary>
    /// Execute an operation with retry and circuit-breaker semantics.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Exception, bool>? isTransient = null,
        CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen)
            throw new CircuitBreakerOpenException(
                $"Circuit breaker open. {_consecutiveFailures} consecutive failures. "
                + $"Retry after {_circuitOpenUntil:O}.");

        var effectiveIsTransient = isTransient ?? DefaultIsTransient;

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                var result = await operation(cancellationToken);

                // Success resets failure count
                Interlocked.Exchange(ref _consecutiveFailures, 0);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (effectiveIsTransient(ex) && attempt < _options.MaxRetries)
            {
                var delay = CalculateDelay(attempt);

                Interlocked.Increment(ref _consecutiveFailures);

                if (_consecutiveFailures >= _options.CircuitBreakerThreshold)
                {
                    _circuitOpenUntil = DateTime.UtcNow.Add(_options.CircuitBreakDuration);
                    throw new CircuitBreakerOpenException(
                        $"Circuit breaker tripped after {_consecutiveFailures} consecutive failures. "
                        + $"Open until {_circuitOpenUntil:O}. Last error: {ex.Message}", ex);
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        // Should be unreachable — last attempt throws through
        throw new InvalidOperationException("Retry policy exhausted.");
    }

    /// <summary>
    /// Execute a void operation with retry and circuit-breaker semantics.
    /// </summary>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        Func<Exception, bool>? isTransient = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<byte>(async ct =>
        {
            await operation(ct);
            return 0;
        }, isTransient, cancellationToken);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        // Exponential backoff with jitter: baseDelay * 2^attempt ± 25%
        var baseMs = _options.BaseDelayMs * Math.Pow(2, attempt);
        var jitter = Random.Shared.NextDouble() * 0.5 + 0.75; // 0.75x to 1.25x
        var delayMs = Math.Min(baseMs * jitter, _options.MaxDelayMs);
        return TimeSpan.FromMilliseconds(delayMs);
    }

    private static bool DefaultIsTransient(Exception ex)
    {
        return ex is IOException or UnauthorizedAccessException;
    }
}

/// <summary>
/// Configuration options for the retry policy.
/// </summary>
public sealed class RetryPolicyOptions
{
    /// <summary>Maximum number of retry attempts (not including initial try).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay between retries in milliseconds.</summary>
    public double BaseDelayMs { get; set; } = 200;

    /// <summary>Maximum delay between retries in milliseconds.</summary>
    public double MaxDelayMs { get; set; } = 10_000;

    /// <summary>Number of consecutive failures before circuit breaker trips.</summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>How long the circuit stays open before half-open trial.</summary>
    public TimeSpan CircuitBreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>Normal operation, requests flow through.</summary>
    Closed,

    /// <summary>Too many failures, requests are rejected immediately.</summary>
    Open,

    /// <summary>Trial state after circuit-break duration expires.</summary>
    HalfOpen
}

/// <summary>
/// Thrown when an operation is rejected because the circuit breaker is open.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
    public CircuitBreakerOpenException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Extension methods for common transient error detection.
/// </summary>
public static class TransientErrorDetectors
{
    /// <summary>
    /// Returns true for transient file-system errors (locked files, sharing violations,
    /// anti-virus interference, disk I/O retry).
    /// </summary>
    public static bool IsTransientFileError(Exception ex)
    {
        return ex switch
        {
            IOException ioEx => IsTransientIoError(ioEx),
            UnauthorizedAccessException => true,
            _ => false
        };
    }

    private static bool IsTransientIoError(IOException ex)
    {
        // HRESULT codes for transient file errors
        return ex.HResult switch
        {
            -2147024864 => true, // ERROR_SHARING_VIOLATION (0x80070020)
            -2147024865 => true, // ERROR_LOCK_VIOLATION (0x80070021)
            -2147024891 => true, // ERROR_ACCESS_DENIED (0x80070005) — may be transient
            -2147023782 => true, // ERROR_DISK_FULL (0x80070070) — may clear up
            _ => false
        };
    }
}
