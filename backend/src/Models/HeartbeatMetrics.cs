namespace ModbusPerfTest.Backend.Models;

/// <summary>
/// Represents the current real-time heartbeat metrics.
/// </summary>
public class HeartbeatMetrics
{
    /// <summary>
    /// The last measured monotonic elapsed time (from Stopwatch) in milliseconds.
    /// </summary>
    public long LastMonoElapsedMs { get; set; }

    /// <summary>
    /// The last measured wall clock elapsed time in milliseconds.
    /// </summary>
    public double LastWallElapsedMs { get; set; }

    /// <summary>
    /// When the last heartbeat measurement was taken.
    /// </summary>
    public DateTime LastCheckedAt { get; set; }

    /// <summary>
    /// The configured expected interval in milliseconds.
    /// </summary>
    public int ExpectedIntervalMs { get; set; }

    /// <summary>
    /// Current system latency (how much longer than expected the heartbeat took).
    /// 0 if on time or faster than expected.
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Current clock drift (divergence between monotonic and wall clock time).
    /// </summary>
    public double ClockDriftMs { get; set; }

    /// <summary>
    /// Whether the system is currently healthy (no threshold violations).
    /// </summary>
    public bool IsHealthy => LatencyMs == 0 && ClockDriftMs < 500;
}
