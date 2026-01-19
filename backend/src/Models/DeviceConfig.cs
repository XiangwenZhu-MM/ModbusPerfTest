using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModbusPerfTest.Backend.Models;

public class DeviceConfig
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; }
    public List<FrameConfig> Frames { get; set; } = new();
    
    /// <summary>
    /// If true, allows concurrent reads of different frames on the same device.
    /// If false (default), frames are read sequentially for maximum compatibility.
    /// </summary>
    public bool AllowConcurrentFrameReads { get; set; } = false;
}

public class FrameConfig
{
    public string Name { get; set; } = string.Empty;
    
    [JsonConverter(typeof(ModbusAddressConverter))]
    public ushort StartAddress { get; set; }
    
    public ushort Count { get; set; }
    public int ScanFrequencyMs { get; set; }
}

public class DeviceConfiguration
{
    public List<DeviceConfig> Devices { get; set; } = new();
}

/// <summary>
/// Converts Modbus 40xxxx notation (e.g., 400001) to 0-based register addresses (e.g., 0).
/// </summary>
public class ModbusAddressConverter : JsonConverter<ushort>
{
    public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetInt32();
        
        // Auto-convert Modbus 40xxxx notation to 0-based register address
        // Modbus notation: 400001 = register 0, 400051 = register 50
        if (value >= 40000)
        {
            return (ushort)((value - 400001) & 0xFFFF);
        }
        
        return (ushort)value;
    }

    public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
