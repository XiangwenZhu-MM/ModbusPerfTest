using System.Collections.Concurrent;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class ScanTaskQueue
{
    private readonly ConcurrentQueue<ScanTask> _queue = new();
    private readonly ConcurrentQueue<long> _droppedTimestamps = new();
    private readonly ConcurrentDictionary<string, bool> _queuedFrames = new(); // Track which frames have tasks queued (deviceId:frameIndex)
    private readonly SemaphoreSlim _signal = new(0);
    private long _enqueuedCount = 0;
    private long _dequeuedCount = 0;
    private long _droppedCount = 0;
    private const int MaxQueueSize = 10000;

    public int Count => _queue.Count;
    public long EnqueuedCount => _enqueuedCount;
    public long DequeuedCount => _dequeuedCount;
    public long DroppedCount => _droppedCount;
    public ConcurrentQueue<long> DroppedTimestamps => _droppedTimestamps;

    public bool TryEnqueue(ScanTask task)
    {
        // Create unique key for this frame (deviceId:frameIndex)
        var frameKey = $"{task.DeviceId}:{task.FrameIndex}";
        
        // Check if a task for this frame is already queued
        if (_queuedFrames.ContainsKey(frameKey))
        {
            Interlocked.Increment(ref _droppedCount);
            _droppedTimestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return false; // Drop the task - frame already has a pending task
        }

        if (_queue.Count >= MaxQueueSize)
        {
            Interlocked.Increment(ref _droppedCount);
            _droppedTimestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return false;
        }

        _queuedFrames.TryAdd(frameKey, true);
        _queue.Enqueue(task);
        Interlocked.Increment(ref _enqueuedCount);
        _signal.Release();
        return true;
    }

    public async Task<ScanTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        
        if (_queue.TryDequeue(out var task))
        {
            var frameKey = $"{task.DeviceId}:{task.FrameIndex}";
            _queuedFrames.TryRemove(frameKey, out _); // Remove frame from tracked set
            Interlocked.Increment(ref _dequeuedCount);
            task.DequeuedAt = DateTime.UtcNow;
            return task;
        }

        return null;
    }

    public void ResetCounters()
    {
        Interlocked.Exchange(ref _enqueuedCount, 0);
        Interlocked.Exchange(ref _dequeuedCount, 0);
        Interlocked.Exchange(ref _droppedCount, 0);
    }
}
