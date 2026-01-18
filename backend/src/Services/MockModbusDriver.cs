namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// Mock Modbus driver for testing without real devices or simulator.
/// Generates realistic mock data with configurable latency.
/// </summary>
public class MockModbusDriver : IModbusDriver
{
    private readonly Random _random = new();
    private readonly int _minLatencyMs;
    private readonly int _maxLatencyMs;
    private readonly Dictionary<string, ushort> _registerValues = new();

    public MockModbusDriver(int minLatencyMs = 20, int maxLatencyMs = 120)
    {
        _minLatencyMs = minLatencyMs;
        _maxLatencyMs = maxLatencyMs;
    }

    public async Task<ushort[]?> ReadHoldingRegistersAsync(
        string ipAddress,
        int port,
        byte slaveId,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken)
    {
        // Simulate I/O latency
        var latency = _random.Next(_minLatencyMs, _maxLatencyMs + 1);
        await Task.Delay(latency, cancellationToken);

        // Generate mock data
        var data = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            var registerAddress = (ushort)(startAddress + i);
            var deviceKey = $"{ipAddress}:{port}:{slaveId}:{registerAddress}";
            
            // Get or initialize register value
            if (!_registerValues.ContainsKey(deviceKey))
            {
                // Initialize with random value between 0-1000
                _registerValues[deviceKey] = (ushort)_random.Next(0, 1001);
            }
            
            // Simulate value changes (±0-10 each read to show variation)
            var currentValue = _registerValues[deviceKey];
            var change = _random.Next(-10, 11);
            var newValue = Math.Clamp(currentValue + change, 0, 65535);
            _registerValues[deviceKey] = (ushort)newValue;
            
            data[i] = (ushort)newValue;
        }

        return data;
    }

    public void Dispose()
    {
        _registerValues.Clear();
    }
}

/// <summary>
/// Configuration class for MockModbusDriver settings.
/// </summary>
public class MockModbusDriverOptions
{
    /// <summary>
    /// Minimum simulated I/O latency in milliseconds. Default: 20ms
    /// </summary>
    public int MinLatencyMs { get; set; } = 20;

    /// <summary>
    /// Maximum simulated I/O latency in milliseconds. Default: 120ms
    /// </summary>
    public int MaxLatencyMs { get; set; } = 120;
}
