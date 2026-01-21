using Microsoft.Data.Sqlite;

namespace ModbusPerfTest.Backend.Services;

public class DataPointRepository : IDisposable
{
    private readonly string _connectionString;

    public DataPointRepository(string dbPath = "datapoints.db")
    {
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
                Value INTEGER NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS IX_DataPoints_Timestamp ON DataPoints(Timestamp);
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

    public async Task InsertDataPointAsync(ushort value)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO DataPoints (Timestamp, Value) VALUES ($timestamp, $value)";
        command.Parameters.AddWithValue("$timestamp", timestamp);
        command.Parameters.AddWithValue("$value", value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task InsertDataPointsAsync(ushort[] values)
    {
        if (values == null || values.Length == 0) return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int maxVariables = 999; // SQLite default
        int chunkSize = maxVariables; // 1 variable per value
        int maxRetries = 5;
        int delayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                for (int offset = 0; offset < values.Length; offset += chunkSize)
                {
                    var chunk = values.Skip(offset).Take(chunkSize).ToArray();
                    var valuePlaceholders = string.Join(",", chunk.Select((_, i) => $"({timestamp}, $v{i})"));
                    var sql = $"INSERT INTO DataPoints (Timestamp, Value) VALUES {valuePlaceholders}";
                    var command = connection.CreateCommand();
                    command.CommandText = sql;
                    for (int i = 0; i < chunk.Length; i++)
                    {
                        command.Parameters.AddWithValue($"$v{i}", chunk[i]);
                    }
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY (database is locked)
            {
                if (attempt == maxRetries - 1) throw;
                await Task.Delay(delayMs);
                delayMs *= 2; // Exponential backoff
            }
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
        // No persistent connection to dispose
    }
}

public class TimeRangeCount
{
    public long Count { get; set; }
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
