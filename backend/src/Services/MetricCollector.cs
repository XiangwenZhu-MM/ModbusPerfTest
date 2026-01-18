using System.Collections.Concurrent;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class MetricCollector
{
    private readonly ConcurrentQueue<DeviceLevelMetric> _deviceMetrics = new();
    private readonly ConcurrentQueue<long> _taskTimestamps = new();
    private readonly ConcurrentQueue<long> _completedTimestamps = new();
    private readonly ConcurrentDictionary<string, long> _frameDroppedCounts = new(); // Per-frame drop counter
    private readonly ConcurrentDictionary<string, ConcurrentQueue<long>> _frameDroppedTimestamps = new(); // Per-frame drop timestamps
    private const int MaxMetricsToKeep = 1000;
    private const int RollingWindowSeconds = 60;

    public void RecordDeviceMetric(DeviceLevelMetric metric)
    {
        _deviceMetrics.Enqueue(metric);
        
        // Keep only recent metrics
        while (_deviceMetrics.Count > MaxMetricsToKeep)
        {
            _deviceMetrics.TryDequeue(out _);
        }
    }

    public void RecordTaskCreated()
    {
        _taskTimestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public void RecordTaskCompleted()
    {
        _completedTimestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public void RecordTaskDropped(string frameId)
    {
        // Increment frame drop counter
        _frameDroppedCounts.AddOrUpdate(frameId, 1, (key, old) => old + 1);
        
        // Record timestamp for rate calculation
        var timestamps = _frameDroppedTimestamps.GetOrAdd(frameId, _ => new ConcurrentQueue<long>());
        timestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public long GetFrameDroppedCount(string frameId)
    {
        return _frameDroppedCounts.TryGetValue(frameId, out var count) ? count : 0;
    }

    public double GetFrameDroppedTPM(string frameId)
    {
        if (!_frameDroppedTimestamps.TryGetValue(frameId, out var timestamps))
            return 0;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (RollingWindowSeconds * 1000);
        var recentDrops = CountInWindow(timestamps, windowStart, now);
        return (recentDrops / (double)RollingWindowSeconds) * 60;
    }

    public SystemHealthMetric CalculateSystemHealth(ConcurrentQueue<long> droppedTimestamps)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (RollingWindowSeconds * 1000);

        // Calculate Ingress TPM (tasks created)
        var recentCreated = CountInWindow(_taskTimestamps, windowStart, now);
        var ingressTPM = (recentCreated / (double)RollingWindowSeconds) * 60;

        // Calculate Egress TPM (tasks completed)
        var recentCompleted = CountInWindow(_completedTimestamps, windowStart, now);
        var egressTPM = (recentCompleted / (double)RollingWindowSeconds) * 60;

        // Calculate Saturation Index
        var saturationIndex = egressTPM > 0 ? (ingressTPM / egressTPM) * 100 : 0;

        // Calculate Dropped TPM (tasks dropped in rolling window)
        var recentDropped = CountInWindow(droppedTimestamps, windowStart, now);
        var droppedTPM = (recentDropped / (double)RollingWindowSeconds) * 60;

        return new SystemHealthMetric
        {
            IngressTPM = ingressTPM,
            EgressTPM = egressTPM,
            SaturationIndex = saturationIndex,
            DroppedTPM = droppedTPM,
            Timestamp = DateTime.UtcNow
        };
    }

    public List<DeviceLevelMetric> GetRecentDeviceMetrics(int count = 100)
    {
        return _deviceMetrics.TakeLast(count).ToList();
    }

    private int CountInWindow(ConcurrentQueue<long> queue, long windowStart, long windowEnd)
    {
        // Remove old entries
        while (queue.TryPeek(out var timestamp) && timestamp < windowStart)
        {
            queue.TryDequeue(out _);
        }

        return queue.Count(ts => ts >= windowStart && ts <= windowEnd);
    }
}
