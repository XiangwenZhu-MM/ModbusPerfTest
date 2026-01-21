using System;
using System.Threading.Tasks;

namespace ModbusPerfTest.Backend.Services;

public record DataPointEntry(
    DateTime Timestamp,
    ushort Value,
    string DeviceName,
    string FrameName,
    ushort RegisterAddress,
    int IndexInFrame
);

public record DeviceCountResult(string DeviceName, long Count);

public interface IDataPointRepository : IDisposable
{
    Task InsertDataPointsAsync(IEnumerable<DataPointEntry> points);
    Task<DataPointCountsResult> GetDataPointCountsAsync();
    Task<List<DeviceCountResult>> GetDeviceCountsAsync(DateTime start, DateTime end);
    Task ClearAllDataAsync();
}
