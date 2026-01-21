using System.Collections.Concurrent;
using System.Net.Sockets;
using NModbus;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// High-performance Modbus driver using NModbus with async operations.
/// Implements Strategy A: One connection per device, multiple async tasks for different polling intervals.
/// </summary>
public class HighPerformanceModbusDriver : IModbusDriver
{
    private readonly ConcurrentDictionary<string, ConnectionContext> _connections = new();
    private readonly ILogger<HighPerformanceModbusDriver>? _logger;
    private readonly ModbusExceptionLogger? _exceptionLogger;
    private bool _disposed;

    public HighPerformanceModbusDriver(ILogger<HighPerformanceModbusDriver>? logger = null, ModbusExceptionLogger? exceptionLogger = null)
    {
        _logger = logger;
        _exceptionLogger = exceptionLogger;
    }

    public async Task<ushort[]?> ReadHoldingRegistersAsync(
        string ipAddress,
        int port,
        byte slaveId,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken)
    {
        var deviceKey = $"{ipAddress}:{port}";
        var context = await GetOrCreateConnectionAsync(deviceKey, ipAddress, port, cancellationToken);

        try
        {
            // Use async method - NModbus handles transaction ID sequencing internally
            var data = await context.Master.ReadHoldingRegistersAsync(slaveId, startAddress, count);
            return data;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Modbus read failed for {DeviceKey} SlaveId={SlaveId} Address={Address} Count={Count}", 
                deviceKey, slaveId, startAddress, count);
            
            // Log exception to file
            _exceptionLogger?.LogException(ex, deviceKey, slaveId, startAddress, count, "ReadHoldingRegisters");
            
            // Remove failed connection - will be recreated on next attempt
            if (_connections.TryRemove(deviceKey, out var ctx))
            {
                await ctx.DisposeAsync();
            }
            
            throw;
        }
    }

    private async Task<ConnectionContext> GetOrCreateConnectionAsync(
        string deviceKey,
        string ipAddress,
        int port,
        CancellationToken cancellationToken)
    {
        // Fast path: connection exists and is healthy
        if (_connections.TryGetValue(deviceKey, out var existingContext) && existingContext.IsConnected)
        {
            return existingContext;
        }

        // Slow path: create new connection
        // Only one thread should create the connection
        var newContext = await CreateConnectionAsync(ipAddress, port, cancellationToken);
        
        // Try to add it; if another thread added one first, use theirs and dispose ours
        var actualContext = _connections.GetOrAdd(deviceKey, newContext);
        
        if (actualContext != newContext)
        {
            // Another thread won the race
            await newContext.DisposeAsync();
        }

        return actualContext;
    }

    private async Task<ConnectionContext> CreateConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken)
    {
        var client = new TcpClient
        {
            NoDelay = true, // Disable Nagle's algorithm for lower latency
            ReceiveBufferSize = 8192,
            SendBufferSize = 8192
        };

        await client.ConnectAsync(ipAddress, port, cancellationToken);

        var factory = new ModbusFactory();
        var master = factory.CreateMaster(client);
        
        // Configure timeouts
        master.Transport.ReadTimeout = 3000;  // 3 seconds
        master.Transport.WriteTimeout = 3000;
        
        _logger?.LogInformation("Created Modbus connection to {IP}:{Port}", ipAddress, port);

        return new ConnectionContext(client, master);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var context in _connections.Values)
        {
            try
            {
                context.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing connection context");
            }
        }

        _connections.Clear();
    }

    public void CloseAllConnections()
    {
        _logger?.LogInformation("Closing all HighPerformance connections ({Count} total)", _connections.Count);

        var connectionsToClose = _connections.ToList();
        _connections.Clear();

        foreach (var kvp in connectionsToClose)
        {
            try
            {
                kvp.Value.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error closing connection for {DeviceKey}", kvp.Key);
            }
        }

        _logger?.LogInformation("All connections closed. New connections will be created on next read.");
    }

    private class ConnectionContext
    {
        private readonly TcpClient _client;
        private readonly IModbusMaster _master;

        public ConnectionContext(TcpClient client, IModbusMaster master)
        {
            _client = client;
            _master = master;
        }

        public IModbusMaster Master => _master;

        public bool IsConnected => _client.Connected;

        public async ValueTask DisposeAsync()
        {
            try
            {
                _master?.Dispose();
            }
            catch { }

            try
            {
                _client?.Close();
                _client?.Dispose();
            }
            catch { }

            await Task.CompletedTask;
        }
    }
}
