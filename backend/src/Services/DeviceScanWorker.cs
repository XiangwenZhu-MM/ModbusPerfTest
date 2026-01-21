using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class DeviceScanWorker
{
    private readonly string _deviceId;
    private readonly DeviceConfig _config;
    private readonly IModbusDriver _driver;
    private readonly List<ScanTaskQueue> _frameQueues; // One queue per frame (or single shared queue)
    private readonly ScanTaskQueue _globalQueue; // For metrics only
    private readonly MetricCollector _metricCollector;
    private readonly ClockDriftService _clockDriftService;
    private readonly DataQualityService _dataQualityService;
    private readonly DataPointBuffer _dataPointBuffer;
    private readonly bool _allowConcurrentFrameReads;
    private readonly SemaphoreSlim _deviceLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private List<Task>? _workerTasks;

    public DeviceScanWorker(
        string deviceId,
        DeviceConfig config,
        IModbusDriver driver,
        ScanTaskQueue taskQueue,
        ScanTaskQueue globalQueue,
        MetricCollector metricCollector,
        ClockDriftService clockDriftService,
        DataQualityService dataQualityService,
        DataPointBuffer dataPointBuffer)
    {
        _deviceId = deviceId;
        _config = config;
        _driver = driver;
        _globalQueue = globalQueue;
        _metricCollector = metricCollector;
        _clockDriftService = clockDriftService;
        _dataQualityService = dataQualityService;
        _dataPointBuffer = dataPointBuffer;
        _allowConcurrentFrameReads = config.AllowConcurrentFrameReads;
        
        // Create per-frame queues if concurrent reads enabled, otherwise use single shared queue
        _frameQueues = new List<ScanTaskQueue>();
        if (_allowConcurrentFrameReads)
        {
            // One queue per frame for true concurrency
            for (int i = 0; i < config.Frames.Count; i++)
            {
                _frameQueues.Add(new ScanTaskQueue());
            }
        }
        else
        {
            // Single queue for sequential processing
            _frameQueues.Add(taskQueue);
        }
    }

    public ScanTaskQueue TaskQueue => _frameQueues[0]; // For backward compatibility
    
    public ScanTaskQueue GetQueueForFrame(int frameIndex)
    {
        return _allowConcurrentFrameReads ? _frameQueues[frameIndex] : _frameQueues[0];
    }
    
    public IReadOnlyList<ScanTaskQueue> GetAllQueues()
    {
        return _frameQueues.AsReadOnly();
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        
        // If concurrent frame reads are enabled, start multiple workers (one per frame)
        // Otherwise, start a single worker for sequential processing
        int workerCount = _allowConcurrentFrameReads ? _config.Frames.Count : 1;
        _workerTasks = new List<Task>();
        
        for (int i = 0; i < workerCount; i++)
        {
            int workerIndex = i; // Capture for closure
            var queue = _frameQueues[workerIndex];
            _workerTasks.Add(WorkerLoop(queue, _cts.Token));
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_workerTasks != null && _workerTasks.Any())
        {
            // Wait for workers to stop with a timeout
            try
            {
                await Task.WhenAny(Task.WhenAll(_workerTasks), Task.Delay(3000));
            }
            catch { }
        }
    }

    private async Task WorkerLoop(ScanTaskQueue queue, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var task = await queue.DequeueAsync(cancellationToken);
                if (task == null)
                    continue;

                // Use device lock only if concurrent frame reads are disabled
                if (_allowConcurrentFrameReads)
                {
                    // High-performance mode: concurrent frame reads
                    await ExecuteTaskAsync(task, cancellationToken);
                }
                else
                {
                    // Compatibility mode: sequential frame reads per device
                    await _deviceLock.WaitAsync(cancellationToken);
                    try
                    {
                        await ExecuteTaskAsync(task, cancellationToken);
                    }
                    finally
                    {
                        _deviceLock.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error (simplified for MVP)
                Console.WriteLine($"Error in worker for {_deviceId}: {ex.Message}");
            }
        }
    }

    private async Task ExecuteTaskAsync(ScanTask task, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var expectedExecutionTime = task.CreatedAt;
        var actualExecutionTime = task.DequeuedAt ?? DateTime.UtcNow;

        // Record clock drift
        _clockDriftService.RecordScheduledExecution(
            expectedExecutionTime,
            actualExecutionTime,
            task.Id.ToString()
        );

        try
        {
            var data = await _driver.ReadHoldingRegistersAsync(
                _config.IpAddress,
                _config.Port,
                task.SlaveId,
                task.StartAddress,
                task.Count,
                cancellationToken
            );

            var completedTime = DateTime.UtcNow;
            task.CompletedAt = completedTime;
            task.IsCompleted = true;

            // Calculate metrics
            var queueDuration = (task.DequeuedAt!.Value - task.CreatedAt).TotalMilliseconds;
            var responseTime = (completedTime - task.DequeuedAt.Value).TotalMilliseconds;
            var totalDeviation = (completedTime - task.CreatedAt).TotalMilliseconds;

            // Get frame details for this task
            var frame = _config.Frames.FirstOrDefault(f => f.StartAddress == task.StartAddress);
            var frameId = $"{_config.IpAddress}:{_config.Port}:{task.SlaveId}:{task.StartAddress}:{task.FrameIndex}";

            var metric = new DeviceLevelMetric
            {
                DeviceId = _deviceId,
                FrameId = frameId,
                StartAddress = task.StartAddress,
                Count = task.Count,
                ScanFrequencyMs = frame?.ScanFrequencyMs ?? 1000,
                QueueDurationMs = queueDuration,
                DeviceResponseTimeMs = responseTime,
                ActualSamplingIntervalMs = totalDeviation,
                DroppedCount = _metricCollector.GetFrameDroppedCount(frameId),
                DroppedTPM = _metricCollector.GetFrameDroppedTPM(frameId),
                Timestamp = completedTime
            };

            _metricCollector.RecordDeviceMetric(metric);
            _metricCollector.RecordTaskCompleted();
            
            // Enqueue data points to buffer (non-blocking, batched writes)
            if (data != null)
            {
                var frameName = frame?.Name ?? "UnknownFrame";
                _dataPointBuffer.EnqueueBatch(DateTime.UtcNow, data, _config.Name, frameName, task.StartAddress);
            }
            
            // Update data quality for each register read
            if (frame != null && data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var dataPointId = $"{_deviceId}:{task.StartAddress + i}";
                    _dataQualityService.UpdateDataPoint(dataPointId, data[i], frame.ScanFrequencyMs);
                }
            }
        }
        catch (Exception ex)
        {
            task.IsFailed = true;
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.UtcNow;
        }
    }
}
