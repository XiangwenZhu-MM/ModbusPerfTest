using System.Collections.Concurrent;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class DataQualityService
{
    private readonly ConcurrentDictionary<string, DataQualityState> _dataPoints = new();

    public void UpdateDataPoint(string dataPointId, object value, int pollingIntervalMs)
    {
        var now = DateTime.UtcNow;
        var stalenessThresholdMs = pollingIntervalMs * 2;

        _dataPoints.AddOrUpdate(
            dataPointId,
            _ => new DataQualityState
            {
                DataPointId = dataPointId,
                Quality = QualityStatus.Good,
                LastKnownValue = value,
                LastSuccessTimestamp = now,
                StalenessThresholdMs = stalenessThresholdMs
            },
            (_, existing) =>
            {
                existing.Quality = QualityStatus.Good;
                existing.LastKnownValue = value;
                existing.LastSuccessTimestamp = now;
                existing.StalenessThresholdMs = stalenessThresholdMs;
                return existing;
            }
        );
    }

    public void CheckStaleness()
    {
        var now = DateTime.UtcNow;

        foreach (var kvp in _dataPoints)
        {
            var state = kvp.Value;
            
            if (state.LastSuccessTimestamp.HasValue)
            {
                var timeSinceLastUpdate = (now - state.LastSuccessTimestamp.Value).TotalMilliseconds;
                
                if (timeSinceLastUpdate > state.StalenessThresholdMs)
                {
                    // Mark as stale, but preserve last known value and timestamp
                    state.Quality = QualityStatus.Stale;
                }
            }
        }
    }

    public DataQualityState? GetDataPointState(string dataPointId)
    {
        return _dataPoints.TryGetValue(dataPointId, out var state) ? state : null;
    }

    public List<DataQualityState> GetAllDataPoints()
    {
        return _dataPoints.Values.ToList();
    }

    public DataQualitySummary GetSummary()
    {
        var allStates = _dataPoints.Values.ToList();
        
        return new DataQualitySummary
        {
            TotalDataPoints = allStates.Count,
            GoodCount = allStates.Count(s => s.Quality == QualityStatus.Good),
            StaleCount = allStates.Count(s => s.Quality == QualityStatus.Stale),
            UncertainCount = allStates.Count(s => s.Quality == QualityStatus.Uncertain),
            Timestamp = DateTime.UtcNow
        };
    }
}

public class DataQualitySummary
{
    public int TotalDataPoints { get; set; }
    public int GoodCount { get; set; }
    public int StaleCount { get; set; }
    public int UncertainCount { get; set; }
    public DateTime Timestamp { get; set; }
}
