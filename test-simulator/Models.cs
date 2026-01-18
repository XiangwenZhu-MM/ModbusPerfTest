namespace ModbusPerfTest.TestSimulator;

public class DeviceConfiguration
{
    public List<DeviceConfig> Devices { get; set; } = new();
}

public class DeviceConfig
{
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public byte SlaveId { get; set; }
    public List<Frame> Frames { get; set; } = new();
}

public class Frame
{
    public ushort StartAddress { get; set; }
    public ushort Count { get; set; }
    public int ScanFrequencyMs { get; set; }
}
