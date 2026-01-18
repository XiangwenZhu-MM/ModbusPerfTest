using System.Collections.Concurrent;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class ClockDriftService
{
    private readonly ConcurrentQueue<ClockDriftMeasurement> _measurements = new();
    private const int MaxMeasurements = 1000;

    public ClockDriftMeasurement RecordScheduledExecution(
        DateTime expectedTime,
        DateTime actualTime,
        string taskId)
    {
        var driftMs = (actualTime - expectedTime).TotalMilliseconds;
        
        var measurement = new ClockDriftMeasurement
        {
            TaskId = taskId,
            ExpectedTime = expectedTime,
            ActualTime = actualTime,
            DriftMs = driftMs,
            Timestamp = DateTime.UtcNow
        };

        _measurements.Enqueue(measurement);
        
        // Keep only recent measurements
        while (_measurements.Count > MaxMeasurements)
        {
            _measurements.TryDequeue(out _);
        }

        return measurement;
    }

    public ClockDriftStatistics GetStatistics()
    {
        var recentMeasurements = _measurements.ToArray();
        
        if (recentMeasurements.Length == 0)
        {
            return new ClockDriftStatistics
            {
                TotalMeasurements = 0,
                AverageDriftMs = 0,
                MinDriftMs = 0,
                MaxDriftMs = 0,
                StandardDeviationMs = 0
            };
        }

        var drifts = recentMeasurements.Select(m => m.DriftMs).ToArray();
        var average = drifts.Average();
        var min = drifts.Min();
        var max = drifts.Max();
        
        var variance = drifts.Sum(d => Math.Pow(d - average, 2)) / drifts.Length;
        var stdDev = Math.Sqrt(variance);

        return new ClockDriftStatistics
        {
            TotalMeasurements = recentMeasurements.Length,
            AverageDriftMs = average,
            MinDriftMs = min,
            MaxDriftMs = max,
            StandardDeviationMs = stdDev,
            Timestamp = DateTime.UtcNow
        };
    }

    public List<ClockDriftMeasurement> GetRecentMeasurements(int count = 100)
    {
        return _measurements.TakeLast(count).ToList();
    }
}

public class ClockDriftMeasurement
{
    public string TaskId { get; set; } = string.Empty;
    public DateTime ExpectedTime { get; set; }
    public DateTime ActualTime { get; set; }
    public double DriftMs { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ClockDriftStatistics
{
    public int TotalMeasurements { get; set; }
    public double AverageDriftMs { get; set; }
    public double MinDriftMs { get; set; }
    public double MaxDriftMs { get; set; }
    public double StandardDeviationMs { get; set; }
    public DateTime Timestamp { get; set; }
}
