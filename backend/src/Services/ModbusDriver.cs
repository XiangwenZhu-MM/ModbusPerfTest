using System.Net.Sockets;
using NModbus;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class ModbusDriver : IModbusDriver
{
    private readonly Dictionary<string, TcpClient> _connections = new();
    private readonly Dictionary<string, IModbusMaster> _masters = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<ushort[]?> ReadHoldingRegistersAsync(
        string ipAddress,
        int port,
        byte slaveId,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken)
    {
        var deviceKey = $"{ipAddress}:{port}";
        
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_connections.ContainsKey(deviceKey) || !_connections[deviceKey].Connected)
            {
                var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port, cancellationToken);
                
                var factory = new ModbusFactory();
                var modbusMaster = factory.CreateMaster(client);
                modbusMaster.Transport.ReadTimeout = 5000;
                modbusMaster.Transport.WriteTimeout = 5000;

                _connections[deviceKey] = client;
                _masters[deviceKey] = modbusMaster;
            }

            var master = _masters[deviceKey];
            var data = await Task.Run(() => master.ReadHoldingRegisters(slaveId, startAddress, count), cancellationToken);
            return data;
        }
        catch (Exception)
        {
            // Remove failed connection
            if (_connections.ContainsKey(deviceKey))
            {
                _connections[deviceKey]?.Dispose();
                _connections.Remove(deviceKey);
                _masters.Remove(deviceKey);
            }
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection?.Dispose();
        }
        _connections.Clear();
        _masters.Clear();
        _lock.Dispose();
    }
}
