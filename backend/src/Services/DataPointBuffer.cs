using System.Collections.Concurrent;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// Non-blocking buffer service that batches data points and periodically flushes to database
/// </summary>
public class DataPointBuffer : IHostedService, IDisposable
{
    private readonly IDataPointRepository _repository;
    private readonly ConcurrentQueue<DataPointEntry> _queue;
    private readonly Timer _flushTimer;
    private readonly ILogger<DataPointBuffer> _logger;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
    private bool _isDisposed;

    public DataPointBuffer(IDataPointRepository repository, ILogger<DataPointBuffer> logger)
    {
        _repository = repository;
        _logger = logger;
        _queue = new ConcurrentQueue<DataPointEntry>();
        _flushTimer = new Timer(FlushCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DataPointBuffer starting with {FlushInterval}s flush interval", _flushInterval.TotalSeconds);
        _flushTimer.Change(_flushInterval, _flushInterval);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DataPointBuffer stopping, flushing remaining data");
        _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
        await FlushAsync();
    }

    /// <summary>
    /// Non-blocking method to enqueue data points with metadata for storage
    /// </summary>
    public void EnqueueBatch(DateTime timestamp, ushort[] values, string deviceName, string frameName, ushort startAddress)
    {
        for (int i = 0; i < values.Length; i++)
        {
            _queue.Enqueue(new DataPointEntry(
                timestamp,
                values[i],
                deviceName,
                frameName,
                (ushort)(startAddress + i),
                i + 1 // 1-based index (00001, 00002...)
            ));
        }
    }

    private void FlushCallback(object? state)
    {
        _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        if (_queue.IsEmpty) return;

        try
        {
            var batch = new List<DataPointEntry>();
            
            // Dequeue ALL available items
            while (_queue.TryDequeue(out var item))
            {
                batch.Add(item);
            }

            if (batch.Count > 0)
            {
                await _repository.InsertDataPointsAsync(batch);
                
                _logger.LogDebug("Flushed {Count} data points to database, {Remaining} remaining in queue", 
                    batch.Count, _queue.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing data points to database");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _flushTimer?.Dispose();
    }
}
