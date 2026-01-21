using System.Collections.Concurrent;
using System.Net.Sockets;
using NModbus;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class ModbusDriver : IModbusDriver
{
    private readonly ConcurrentDictionary<string, ConnectionContext> _connections = new();
    private readonly ModbusExceptionLogger? _exceptionLogger;

    public ModbusDriver(ModbusExceptionLogger? exceptionLogger = null)
    {
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
        
        // Get or create connection for this specific device
        var context = await GetOrCreateConnectionAsync(deviceKey, ipAddress, port, cancellationToken);
        
        // Lock per-connection, not globally
        await context.Lock.WaitAsync(cancellationToken);
        try
        {
            var data = await Task.Run(() => context.Master.ReadHoldingRegisters(slaveId, startAddress, count), cancellationToken);
            return data;
        }
        catch (Exception ex)
        {
            // Log exception to file
            _exceptionLogger?.LogException(ex, deviceKey, slaveId, startAddress, count, "ReadHoldingRegisters");
            
            // Remove failed connection
            if (_connections.TryRemove(deviceKey, out var ctx))
            {
                ctx.Dispose();
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
        
        // Try to add it; if another thread added one first, use theirs and dispose ours
        var actualContext = _connections.GetOrAdd(deviceKey, newContext);
        
        if (actualContext != newContext)
        {
            // Another thread won the race
            newContext.Dispose();
        }

        return actualContext;
    }

    private async Task<ConnectionContext> CreateConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        await client.ConnectAsync(ipAddress, port, cancellationToken);
        
        var factory = new ModbusFactory();
        var modbusMaster = factory.CreateMaster(client);
        modbusMaster.Transport.ReadTimeout = 5000;
        modbusMaster.Transport.WriteTimeout = 5000;

        return new ConnectionContext(client, modbusMaster);
    }

    public void Dispose()
    {
        foreach (var context in _connections.Values)
        {
            try
            {
                context?.Dispose();
            }
            catch { }
        }
        _connections.Clear();
    }

    public void CloseAllConnections()
    {
        var connectionsToClose = _connections.ToArray();
        _connections.Clear();
        
        foreach (var kvp in connectionsToClose)
        {
            try
            {
                kvp.Value?.Dispose();
            }
            catch { }
        }
    }

    private class ConnectionContext
    {
        public TcpClient Client { get; }
        public IModbusMaster Master { get; }
        public SemaphoreSlim Lock { get; } = new(1, 1); // Per-connection lock

        public ConnectionContext(TcpClient client, IModbusMaster master)
        {
            Client = client;
            Master = master;
        }

        public void Dispose()
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
        }
    }
}
