using Microsoft.Data.Sqlite;

namespace ModbusPerfTest.Backend.Services;

public class DataPointRepository : IDataPointRepository
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
    private readonly ILogger<DataPointRepository> _logger;

    public DataPointRepository(ILogger<DataPointRepository> logger, string dbPath = "datapoints.db")
    {
        _logger = logger;
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
        EnableWalMode();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS DataPoints (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp INTEGER NOT NULL,
                DeviceName TEXT,
                TagName TEXT,
                Value INTEGER NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS IX_DataPoints_Timestamp ON DataPoints(Timestamp);
            CREATE INDEX IF NOT EXISTS IX_DataPoints_DeviceName ON DataPoints(DeviceName);
        ";
        command.ExecuteNonQuery();
    }

    private void EnableWalMode()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
    }

    public async Task InsertDataPointsAsync(IEnumerable<DataPointEntry> entries)
    {
        if (entries == null || !entries.Any()) return;

        int maxVariables = 999; 
        int maxRetries = 5;
        int delayMs = 100;
        var entryList = entries.ToList();

        // Serialize writes to prevent SQLITE_BUSY under high load
        await _dbLock.WaitAsync();
        try
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var connection = new SqliteConnection(_connectionString);
                    await connection.OpenAsync();
                    using var transaction = connection.BeginTransaction();

                    // SQLite has a limit on parameters, so we chunk the entries
                    // Each entry uses 4 parameters: Timestamp, DeviceName, TagName, Value
                    int entriesPerChunk = maxVariables / 4;

                    for (int offset = 0; offset < entryList.Count; offset += entriesPerChunk)
                    {
                        var chunk = entryList.Skip(offset).Take(entriesPerChunk).ToList();
                        var valuePlaceholders = string.Join(",", chunk.Select((_, i) => $"($t{i}, $d{i}, $n{i}, $v{i})"));
                        var sql = $"INSERT INTO DataPoints (Timestamp, DeviceName, TagName, Value) VALUES {valuePlaceholders}";
                        
                        var command = connection.CreateCommand();
                        command.CommandText = sql;
                        command.Transaction = transaction;

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            var entry = chunk[i];
                            var timestampMs = new DateTimeOffset(entry.Timestamp).ToUnixTimeMilliseconds();
                            var tagName = $"{entry.DeviceName}{entry.FrameName}{entry.IndexInFrame:D5}";
                            
                            command.Parameters.AddWithValue($"$t{i}", timestampMs);
                            command.Parameters.AddWithValue($"$d{i}", entry.DeviceName);
                            command.Parameters.AddWithValue($"$n{i}", tagName);
                            command.Parameters.AddWithValue($"$v{i}", entry.Value);
                        }
                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    return;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
                {
                    if (attempt == maxRetries - 1) throw;
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
            }
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<DataPointCountsResult> GetDataPointCountsAsync()
    {
        // Use 1-minute delay to ensure buffered data has been flushed
        var endTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var endTimeMs = endTime.ToUnixTimeMilliseconds();
        
        var oneMinuteAgo = endTimeMs - (60 * 1000);
        var tenMinutesAgo = endTimeMs - (10 * 60 * 1000);
        var oneHourAgo = endTimeMs - (60 * 60 * 1000);
        var twoHoursAgo = endTimeMs - (2 * 60 * 60 * 1000);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        return new DataPointCountsResult
        {
            LastMinute = new TimeRangeCount
            {
                Count = await GetCountBetweenAsync(connection, oneMinuteAgo, endTimeMs),
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(oneMinuteAgo).UtcDateTime,
                EndTime = endTime.UtcDateTime
            },
            Last10Minutes = new TimeRangeCount
            {
                Count = await GetCountBetweenAsync(connection, tenMinutesAgo, endTimeMs),
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(tenMinutesAgo).UtcDateTime,
                EndTime = endTime.UtcDateTime
            },
            LastHour = new TimeRangeCount
            {
                Count = await GetCountBetweenAsync(connection, oneHourAgo, endTimeMs),
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(oneHourAgo).UtcDateTime,
                EndTime = endTime.UtcDateTime
            },
            Last2Hours = new TimeRangeCount
            {
                Count = await GetCountBetweenAsync(connection, twoHoursAgo, endTimeMs),
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(twoHoursAgo).UtcDateTime,
                EndTime = endTime.UtcDateTime
            }
        };
    }

    private async Task<long> GetCountBetweenAsync(SqliteConnection connection, long startTimestamp, long endTimestamp)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM DataPoints WHERE Timestamp >= $start AND Timestamp <= $end";
        command.Parameters.AddWithValue("$start", startTimestamp);
        command.Parameters.AddWithValue("$end", endTimestamp);
        
        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    public async Task<List<DeviceCountResult>> GetDeviceCountsAsync(DateTime start, DateTime end)
    {
        var results = new List<DeviceCountResult>();
        try
        {
            var startMs = new DateTimeOffset(start).ToUnixTimeMilliseconds();
            var endMs = new DateTimeOffset(end).ToUnixTimeMilliseconds();

            await _dbLock.WaitAsync();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT DeviceName, COUNT(*) FROM DataPoints WHERE Timestamp >= @start AND Timestamp <= @end GROUP BY DeviceName ORDER BY DeviceName";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@start", startMs);
            command.Parameters.AddWithValue("@end", endMs);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DeviceCountResult(reader.GetString(0), reader.GetInt64(1)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device counts from SQLite");
        }
        finally
        {
            _dbLock.Release();
        }
        return results;
    }

    public async Task ClearAllDataAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DataPoints";
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _dbLock.Dispose();
    }
}

public class TimeRangeCount
{
    public long Count { get; set; }
    public long TheoreticalCount { get; set; }
    public double MissingRate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class DataPointCountsResult
{
    public TimeRangeCount LastMinute { get; set; } = null!;
    public TimeRangeCount Last10Minutes { get; set; } = null!;
    public TimeRangeCount LastHour { get; set; } = null!;
    public TimeRangeCount Last2Hours { get; set; } = null!;
}
