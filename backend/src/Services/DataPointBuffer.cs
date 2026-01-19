using System.Collections.Concurrent;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// Non-blocking buffer service that batches data points and periodically flushes to database
/// </summary>
public class DataPointBuffer : IHostedService, IDisposable
{
    private readonly DataPointRepository _repository;
    private readonly ConcurrentQueue<(DateTime Timestamp, ushort Value)> _queue;
    private readonly Timer _flushTimer;
    private readonly ILogger<DataPointBuffer> _logger;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
    private readonly int _batchSize = 10000;
    private bool _isDisposed;

    public DataPointBuffer(DataPointRepository repository, ILogger<DataPointBuffer> logger)
    {
        _repository = repository;
        _logger = logger;
        _queue = new ConcurrentQueue<(DateTime, ushort)>();
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
    /// Non-blocking method to enqueue data points
    /// </summary>
    public void EnqueueBatch(DateTime timestamp, ushort[] values)
    {
        foreach (var value in values)
        {
            _queue.Enqueue((timestamp, value));
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
            var batch = new List<ushort>();
            
            // Dequeue up to batchSize items
            while (batch.Count < _batchSize && _queue.TryDequeue(out var item))
            {
                batch.Add(item.Value);
            }

            if (batch.Count > 0)
            {
                await _repository.InsertDataPointsAsync(batch.ToArray());
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
