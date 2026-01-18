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
}
