namespace ModbusPerfTest.Backend.Models;

/// <summary>
/// Represents a detected system anomaly (either internal latency or system drift).
/// </summary>
public class DriftEvent
{
    /// <summary>
    /// Type of drift detected.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// When the event was detected (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Elapsed time measured by monotonic timer (Stopwatch) in milliseconds.
    /// </summary>
    public long MonoElapsedMs { get; init; }

    /// <summary>
    /// Elapsed time measured by system wall clock (DateTime.UtcNow) in milliseconds.
    /// </summary>
    public double WallElapsedMs { get; init; }

    /// <summary>
    /// The configured heartbeat interval in milliseconds.
    /// </summary>
    public int ExpectedIntervalMs { get; init; }

    /// <summary>
    /// Magnitude of deviation in milliseconds.
    /// For PERFORMANCE_DEGRADED: MonoElapsedMs - ExpectedIntervalMs
    /// For CLOCK_SHIFT: |MonoElapsedMs - WallElapsedMs|
    /// </summary>
    public double DeviationMs => EventType == "PERFORMANCE_DEGRADED"
        ? MonoElapsedMs - ExpectedIntervalMs
        : Math.Abs(MonoElapsedMs - WallElapsedMs);

    /// <summary>
    /// Human-readable description of the event.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Event type constant for performance degradation.
    /// </summary>
    public const string TypePerformanceDegraded = "PERFORMANCE_DEGRADED";

    /// <summary>
    /// Event type constant for clock shift.
    /// </summary>
    public const string TypeClockShift = "CLOCK_SHIFT";
}
