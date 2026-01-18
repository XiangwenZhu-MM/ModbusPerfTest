namespace ModbusPerfTest.Backend.Models;

public class DeviceConfig
{
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; }
    public List<FrameConfig> Frames { get; set; } = new();
}

public class FrameConfig
{
    public ushort StartAddress { get; set; }
    public ushort Count { get; set; }
    public int ScanFrequencyMs { get; set; }
}

public class DeviceConfiguration
{
    public List<DeviceConfig> Devices { get; set; } = new();
}
