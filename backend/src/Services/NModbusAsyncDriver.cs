using System.Collections.Concurrent;
using System.Net.Sockets;
using NModbus;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// High-performance Modbus driver using NModbusAsync (wolf8196) with async operations.
/// Implements Strategy A: One connection per device, multiple async tasks for different polling intervals.
/// </summary>
public class NModbusAsyncDriver : IModbusDriver
{
    private readonly ConcurrentDictionary<string, ConnectionContext> _connections = new();
    private readonly ILogger<NModbusAsyncDriver>? _logger;
    private readonly ModbusExceptionLogger? _exceptionLogger;
    private bool _disposed;

    public NModbusAsyncDriver(ILogger<NModbusAsyncDriver>? logger = null, ModbusExceptionLogger? exceptionLogger = null)
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

        // Serialize access to the Modbus master to prevent concurrent read issues
        await context.Lock.WaitAsync(cancellationToken);
        try
        {
            // Use NModbusAsync async method - handles transaction ID sequencing internally
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
        finally
        {
            context.Lock.Release();
        }
    }

    private async Task<ConnectionContext> GetOrCreateConnectionAsync(
        string deviceKey,
        string ipAddress,
        int port,
        CancellationToken cancellationToken)
    {
        // Fast path: connection exists and is healthy
        if (_connections.TryGetValue(deviceKey, out var existingContext) && 
            existingContext.Client.Connected)
        {
            return existingContext;
        }

        // Slow path: create new connection
        var newContext = await CreateConnectionAsync(ipAddress, port, cancellationToken);

        // Try to add the new connection, or use the one added by another thread
        if (!_connections.TryAdd(deviceKey, newContext))
        {
            // Another thread won the race
            await newContext.DisposeAsync();
            return _connections[deviceKey];
        }

        return newContext;
    }

    private async Task<ConnectionContext> CreateConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Creating new Modbus TCP connection to {IpAddress}:{Port}", ipAddress, port);

        var client = new TcpClient();
        client.NoDelay = true; // Disable Nagle's algorithm for lower latency
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;

        await client.ConnectAsync(ipAddress, port, cancellationToken);

        var factory = new ModbusFactory();
        var master = factory.CreateMaster(client);

        return new ConnectionContext(client, master);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger?.LogInformation("Disposing NModbusAsyncDriver - closing {Count} connections", _connections.Count);

        foreach (var kvp in _connections)
        {
            try
            {
                // Synchronously wait for disposal with timeout
                kvp.Value.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing connection for {DeviceKey}", kvp.Key);
            }
        }

        _connections.Clear();
    }

    public void CloseAllConnections()
    {
        _logger?.LogInformation("Closing all NModbusAsync connections ({Count} total)", _connections.Count);

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

    private class ConnectionContext : IAsyncDisposable
    {
        public TcpClient Client { get; }
        public IModbusMaster Master { get; }
        public SemaphoreSlim Lock { get; } = new(1, 1); // Serialize access to Master for thread safety

        public ConnectionContext(TcpClient client, IModbusMaster master)
        {
            Client = client;
            Master = master;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                Master?.Dispose();
            }
            catch { }

            try
            {
                Client?.Close();
                Client?.Dispose();
            }
            catch { }

            Lock?.Dispose();

            await Task.CompletedTask;
        }
    }
}
