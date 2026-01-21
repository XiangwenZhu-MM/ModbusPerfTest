using System;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace ModbusPerfTest.Backend.Services;

public class InfluxDataPointRepository : IDataPointRepository, IDisposable
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;

    public InfluxDataPointRepository(string url, string token, string bucket, string org)
    {
        _client = new InfluxDBClient(url, token);
        _bucket = bucket;
        _org = org;
    }

    public async Task InsertDataPointsAsync(IEnumerable<DataPointEntry> entries)
    {
        var writeApi = _client.GetWriteApiAsync();
        var points = new List<PointData>();
        foreach (var entry in entries)
        {
            // Tag name: deviceName+FrameName+suffix (e.g. Port3Frame100001)
            var suffix = entry.IndexInFrame.ToString("D5");
            var tagName = $"{entry.DeviceName}{entry.FrameName}{suffix}";

            var point = PointData.Measurement("datapoint")
                .Tag("device", entry.DeviceName)
                .Tag("tagname", tagName)
                .Field("value", entry.Value)
                .Timestamp(entry.Timestamp, WritePrecision.Ms);
            points.Add(point);
        }
        await writeApi.WritePointsAsync(points, _bucket, _org);
    }

    public async Task<DataPointCountsResult> GetDataPointCountsAsync()
    {
        var now = DateTime.UtcNow;
        var fluxQueries = new[] {
            ("LastMinute", "-1m"),
            ("Last10Minutes", "-10m"),
            ("LastHour", "-1h"),
            ("Last2Hours", "-2h")
        };
        
        var result = new DataPointCountsResult();
        foreach (var (label, range) in fluxQueries)
        {
            // Use sum of counts across all series to get total data points
            var flux = $@"from(bucket: ""{_bucket}"")
  |> range(start: {range})
  |> filter(fn: (r) => r._measurement == ""datapoint"" and r._field == ""value"")
  |> count()
  |> group()
  |> sum()";

            var tables = await _client.GetQueryApi().QueryAsync(flux, _org);
            long count = 0;
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        var val = record.GetValue();
                        if (val != null)
                        {
                            count += Convert.ToInt64(val);
                        }
                    }
                }
            }

            var startTime = range switch {
                "-1m" => now.AddMinutes(-1),
                "-10m" => now.AddMinutes(-10),
                "-1h" => now.AddHours(-1),
                "-2h" => now.AddHours(-2),
                _ => now
            };

            var timeRangeCount = new TimeRangeCount {
                Count = count,
                StartTime = startTime,
                EndTime = now
            };

            switch (label) {
                case "LastMinute": result.LastMinute = timeRangeCount; break;
                case "Last10Minutes": result.Last10Minutes = timeRangeCount; break;
                case "LastHour": result.LastHour = timeRangeCount; break;
                case "Last2Hours": result.Last2Hours = timeRangeCount; break;
            }
        }
        return result;
    }

    public async Task<List<DeviceCountResult>> GetDeviceCountsAsync(DateTime start, DateTime end)
    {
        var results = new List<DeviceCountResult>();
        try
        {
            var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endStr = end.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var flux = $@"from(bucket: ""{_bucket}"")
  |> range(start: {startStr}, stop: {endStr})
  |> filter(fn: (r) => r._measurement == ""datapoint"" and r._field == ""value"")
  |> group(columns: [""device""])
  |> count()";

            var tables = await _client.GetQueryApi().QueryAsync(flux, _org);
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        var device = record.GetValueByKey("device")?.ToString() ?? "Unknown";
                        var count = Convert.ToInt64(record.GetValue());
                        results.Add(new DeviceCountResult(device, count));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error querying device counts: {ex.Message}");
        }

        return results.OrderBy(x => x.DeviceName).ToList();
    }

    public async Task ClearAllDataAsync()
    {
        // Delete all datapoint measurement data in bucket using DeleteApi (preserves bucket and token)
        var deleteApi = _client.GetDeleteApi();
        var start = DateTime.MinValue;
        var stop = DateTime.UtcNow.AddMinutes(1); // ensure all data is covered
        var predicate = "_measurement=\"datapoint\"";
        await deleteApi.Delete(start, stop, predicate, _bucket, _org);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
