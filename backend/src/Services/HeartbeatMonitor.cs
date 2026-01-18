using System.Collections.Concurrent;
using System.Diagnostics;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// Background service that monitors system health by detecting performance degradation
/// and clock synchronization issues.
/// </summary>
public class HeartbeatMonitor : BackgroundService
{
    private readonly HeartbeatConfig _config;
    private readonly ILogger<HeartbeatMonitor> _logger;
    private readonly HeartbeatLogger _heartbeatLogger;
    private readonly ConcurrentQueue<DriftEvent> _recentWarnings = new();

    public HeartbeatMonitor(HeartbeatConfig config, ILogger<HeartbeatMonitor> logger, HeartbeatLogger heartbeatLogger)
    {
        _config = config;
        _logger = logger;
        _heartbeatLogger = heartbeatLogger;
    }

    /// <summary>
    /// Gets recent warnings for API retrieval (newest first).
    /// </summary>
    public IEnumerable<DriftEvent> GetRecentWarnings()
    {
        return _recentWarnings.Reverse();
    }

    /// <summary>
    /// Gets the current heartbeat configuration.
    /// </summary>
    public HeartbeatConfig GetConfig()
    {
        return _config;
    }

    /// <summary>
    /// Background execution loop for heartbeat monitoring.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Heartbeat monitor is disabled");
            return;
        }

        _logger.LogInformation(
            "Heartbeat monitor started: Interval={IntervalMs}ms, Threshold={ThresholdMs}ms",
            _config.IntervalMs,
            _config.ThresholdMs
        );

        try
        {
            await RunHeartbeatLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Heartbeat monitor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat monitor encountered an error");
        }
    }

    /// <summary>
    /// Main heartbeat monitoring loop using PeriodicTimer.
    /// </summary>
    private async Task RunHeartbeatLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_config.IntervalMs));

        // Initialize reference counters
        var stopwatch = Stopwatch.StartNew();
        var lastWallClock = DateTime.UtcNow;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Capture current deltas
            long monoElapsedMs = stopwatch.ElapsedMilliseconds;
            DateTime currentWall = DateTime.UtcNow;
            double wallElapsedMs = (currentWall - lastWallClock).TotalMilliseconds;

            // Evaluate for drift conditions
            EvaluateDrift(monoElapsedMs, wallElapsedMs, currentWall);

            // Reset baselines for next cycle (realignment)
            stopwatch.Restart();
            lastWallClock = currentWall;
        }
    }

    /// <summary>
    /// Evaluates drift conditions and records warnings if thresholds are exceeded.
    /// </summary>
    private void EvaluateDrift(long monoMs, double wallMs, DateTime timestamp)
    {
        // Detection 1: Internal Latency (Process/OS Stall)
        if (monoMs > _config.ThresholdMs)
        {
            var message = $"Expected {_config.IntervalMs}ms, took {monoMs}ms (Potential CPU/GC spike)";
            var driftEvent = new DriftEvent
            {
                EventType = DriftEvent.TypePerformanceDegraded,
                Timestamp = timestamp,
                MonoElapsedMs = monoMs,
                WallElapsedMs = wallMs,
                ExpectedIntervalMs = _config.IntervalMs,
                Message = message
            };

            RecordWarning(driftEvent);
            LogWarning(DriftEvent.TypePerformanceDegraded, message);
        }

        // Detection 2: System Clock Jump (NTP/Manual Change)
        // Look for significant divergence between Stopwatch and Wall Clock
        const int ClockDriftThresholdMs = 500;
        double divergence = Math.Abs(monoMs - wallMs);

        if (divergence > ClockDriftThresholdMs)
        {
            var message = $"System clock changed externally. Mono: {monoMs}ms vs Wall: {wallMs:F1}ms";
            var driftEvent = new DriftEvent
            {
                EventType = DriftEvent.TypeClockShift,
                Timestamp = timestamp,
                MonoElapsedMs = monoMs,
                WallElapsedMs = wallMs,
                ExpectedIntervalMs = _config.IntervalMs,
                Message = message
            };

            RecordWarning(driftEvent);
            LogWarning(DriftEvent.TypeClockShift, message);
        }
    }

    /// <summary>
    /// Records a warning to the in-memory cache for API retrieval.
    /// </summary>
    private void RecordWarning(DriftEvent evt)
    {
        // Log to file
        _heartbeatLogger.LogDriftEvent(evt);

        // Add to in-memory cache
        _recentWarnings.Enqueue(evt);

        // Maintain bounded cache size
        while (_recentWarnings.Count > _config.MaxWarningsInMemory)
        {
            _recentWarnings.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    private void LogWarning(string type, string message)
    {
        _logger.LogWarning("[{Type}] {Message}", type, message);
    }
}
