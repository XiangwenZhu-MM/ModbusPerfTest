namespace ModbusPerfTest.Backend.Models;

/// <summary>
/// Configuration settings for the heartbeat monitor.
/// </summary>
public class HeartbeatConfig
{
    /// <summary>
    /// Whether heartbeat monitoring is currently active.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Milliseconds between heartbeat pulses.
    /// </summary>
    public int IntervalMs { get; set; } = 1000;

    /// <summary>
    /// Deviation threshold for triggering warnings (must be >= IntervalMs).
    /// </summary>
    public int ThresholdMs { get; set; } = 2000;

    /// <summary>
    /// Maximum number of warnings retained in memory for API retrieval.
    /// </summary>
    public int MaxWarningsInMemory { get; set; } = 50;

    /// <summary>
    /// Path to the warning log file.
    /// </summary>
    public string LogFilePath { get; set; } = "logs/heartbeat-warnings.log";

    /// <summary>
    /// Maximum log file size in megabytes before rotation.
    /// </summary>
    public int MaxLogFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Validates configuration parameters.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (IntervalMs < 100 || IntervalMs > 60000)
            throw new ArgumentException("IntervalMs must be between 100 and 60000");

        if (ThresholdMs < IntervalMs)
            throw new ArgumentException("ThresholdMs must be >= IntervalMs");

        if (MaxWarningsInMemory < 10 || MaxWarningsInMemory > 1000)
            throw new ArgumentException("MaxWarningsInMemory must be between 10 and 1000");

        if (MaxLogFileSizeMB < 1 || MaxLogFileSizeMB > 1000)
            throw new ArgumentException("MaxLogFileSizeMB must be between 1 and 1000");

        if (string.IsNullOrWhiteSpace(LogFilePath))
            throw new ArgumentException("LogFilePath must not be empty");
    }
}
