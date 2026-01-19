namespace ModbusPerfTest.Backend.Models;

public class DeviceLevelMetric
{
    public string DeviceId { get; set; } = string.Empty;
    public string FrameId { get; set; } = string.Empty; // Format: "IP:Port:SlaveId:StartAddress"
    public ushort StartAddress { get; set; }
    public ushort Count { get; set; }
    public int ScanFrequencyMs { get; set; }
    public double QueueDurationMs { get; set; }
    public double DeviceResponseTimeMs { get; set; }
    public double ActualSamplingIntervalMs { get; set; }
    public long DroppedCount { get; set; } // Total dropped tasks for this frame
    public double DroppedTPM { get; set; } // Dropped tasks per minute for this frame
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SystemHealthMetric
{
    public double IngressTPM { get; set; }  // Tasks per minute
    public double EgressTPM { get; set; }   // Tasks per minute
    public double SaturationIndex { get; set; }  // Percentage
    public double DroppedTPM { get; set; }  // Tasks per minute
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DataQualityState
{
    public string DataPointId { get; set; } = string.Empty;
    public QualityStatus Quality { get; set; } = QualityStatus.Good;
    public object? LastKnownValue { get; set; }
    public DateTime? LastSuccessTimestamp { get; set; }
    public int StalenessThresholdMs { get; set; }
}

public enum QualityStatus
{
    Good,
    Stale,
    Uncertain
}
