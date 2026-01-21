namespace ModbusPerfTest.Backend.Models;

/// <summary>
/// Specifies which Modbus library implementation to use.
/// </summary>
public enum ModbusLibrary
{
    /// <summary>
    /// Use the standard NModbus library (default).
    /// </summary>
    NModbus,

    /// <summary>
    /// Use the NModbus4.NetCore (NModbusAsync) library for async-first operations.
    /// </summary>
    NModbusAsync
}
