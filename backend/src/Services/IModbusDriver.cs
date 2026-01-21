namespace ModbusPerfTest.Backend.Services;

public interface IModbusDriver : IDisposable
{
    Task<ushort[]?> ReadHoldingRegistersAsync(
        string ipAddress,
        int port,
        byte slaveId,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Closes all active TCP connections without disposing the driver.
    /// Allows the driver to be reused by creating new connections on next read.
    /// </summary>
    void CloseAllConnections();
}
