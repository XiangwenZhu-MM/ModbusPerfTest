namespace ModbusPerfTest.Backend.Models;

public class SystemResourceMetrics
{
    public double CpuPercentage { get; set; }
    public double MemoryUsageMB { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
