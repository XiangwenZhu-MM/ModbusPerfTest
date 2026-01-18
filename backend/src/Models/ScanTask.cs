namespace ModbusPerfTest.Backend.Models;

public class ScanTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public byte SlaveId { get; set; }
    public ushort StartAddress { get; set; }
    public ushort Count { get; set; }
    public int FrameIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DequeuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public string? ErrorMessage { get; set; }
}
