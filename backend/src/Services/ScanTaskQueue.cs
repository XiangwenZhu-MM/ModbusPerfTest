using System.Collections.Concurrent;
using System.Threading.Channels;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class ScanTaskQueue
{
    private readonly Channel<ScanTask> _channel;
    private readonly ConcurrentQueue<long> _droppedTimestamps = new();
    private readonly ConcurrentDictionary<string, bool> _queuedFrames = new();
    private long _enqueuedCount = 0;
    private long _dequeuedCount = 0;
    private long _droppedCount = 0;
    private const int MaxQueueSize = 10000;

    public ScanTaskQueue()
    {
        _channel = Channel.CreateBounded<ScanTask>(new BoundedChannelOptions(MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    public int Count => _channel.Reader.Count;
    public long EnqueuedCount => _enqueuedCount;
    public long DequeuedCount => _dequeuedCount;
    public long DroppedCount => _droppedCount;
    public ConcurrentQueue<long> DroppedTimestamps => _droppedTimestamps;

    public bool TryEnqueue(ScanTask task)
    {
        var frameKey = $"{task.DeviceId}:{task.FrameIndex}";
        
        if (_queuedFrames.ContainsKey(frameKey))
        {
            Interlocked.Increment(ref _droppedCount);
            _droppedTimestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return false;
        }

        _queuedFrames.TryAdd(frameKey, true);
        
        if (_channel.Writer.TryWrite(task))
        {
            Interlocked.Increment(ref _enqueuedCount);
            return true;
        }
        else
        {
            _queuedFrames.TryRemove(frameKey, out _);
            Interlocked.Increment(ref _droppedCount);
            _droppedTimestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return false;
        }
    }

    public async Task<ScanTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        var task = await _channel.Reader.ReadAsync(cancellationToken);
        
        var frameKey = $"{task.DeviceId}:{task.FrameIndex}";
        _queuedFrames.TryRemove(frameKey, out _);
        Interlocked.Increment(ref _dequeuedCount);
        task.DequeuedAt = DateTime.UtcNow;
        return task;
    }

    public void ResetCounters()
    {
        Interlocked.Exchange(ref _enqueuedCount, 0);
        Interlocked.Exchange(ref _dequeuedCount, 0);
        Interlocked.Exchange(ref _droppedCount, 0);
    }
}
