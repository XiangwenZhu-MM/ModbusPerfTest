using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class DeviceScanManager
{
    private readonly IModbusDriver _driver;
    private readonly ScanTaskQueue _globalQueue; // For metrics aggregation
    private readonly MetricCollector _metricCollector;
    private readonly ClockDriftService _clockDriftService;
    private readonly DataQualityService _dataQualityService;
    private readonly DataPointBuffer _dataPointBuffer;
    private readonly RuntimeConfigService _runtimeConfigService;
    private readonly Dictionary<string, DeviceScanWorker> _workers = new();
    private readonly Dictionary<string, ScanTaskQueue> _deviceQueues = new();
    private readonly Dictionary<string, List<Task>> _taskGenerators = new();
    private Task? _stalenessCheckTask;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    public bool IsRunning => _isRunning;

    public DeviceScanManager(
        IModbusDriver driver,
        ScanTaskQueue taskQueue,
        MetricCollector metricCollector,
        ClockDriftService clockDriftService,
        DataQualityService dataQualityService,
        DataPointBuffer dataPointBuffer,
        RuntimeConfigService runtimeConfigService)
    {
        _driver = driver;
        _globalQueue = taskQueue;
        _metricCollector = metricCollector;
        _clockDriftService = clockDriftService;
        _dataQualityService = dataQualityService;
        _dataPointBuffer = dataPointBuffer;
        _runtimeConfigService = runtimeConfigService;
    }

    public void StartMonitoring(List<DeviceConfig> devices)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Monitoring is already running");
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _isRunning = true;

        // Get global AllowConcurrentFrameReads setting from runtime config
        var runtimeConfig = _runtimeConfigService.GetConfig();
        var allowConcurrentFrameReads = runtimeConfig.AllowConcurrentFrameReads;

        foreach (var device in devices)
        {
            var deviceId = $"{device.IpAddress}:{device.Port}:{device.SlaveId}";
            
            // Create dedicated queue for this device
            var deviceQueue = new ScanTaskQueue();
            _deviceQueues[deviceId] = deviceQueue;
            
            // Override device config with global runtime setting
            var deviceConfigCopy = new DeviceConfig
            {
                Name = device.Name,
                IpAddress = device.IpAddress,
                Port = device.Port,
                SlaveId = device.SlaveId,
                Frames = device.Frames,
                AllowConcurrentFrameReads = allowConcurrentFrameReads // Use global setting
            };
            
            // Create and start worker for this device
            var worker = new DeviceScanWorker(
                deviceId,
                deviceConfigCopy,
                _driver,
                deviceQueue,
                _globalQueue,
                _metricCollector,
                _clockDriftService,
                _dataQualityService,
                _dataPointBuffer
            );
            worker.Start();
            _workers[deviceId] = worker;

            // Create periodic task generators for each frame using PeriodicTimer
            var tasks = new List<Task>();
            for (int frameIndex = 0; frameIndex < device.Frames.Count; frameIndex++)
            {
                var frame = device.Frames[frameIndex];
                var index = frameIndex; // Capture for closure
                
                var task = Task.Run(async () =>
                {
                    using var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(frame.ScanFrequencyMs));
                    
                    // Generate first task immediately
                    GenerateScanTask(deviceId, device.SlaveId, frame, index);
                    
                    // Then wait for periodic ticks
                    while (await periodicTimer.WaitForNextTickAsync(token))
                    {
                        GenerateScanTask(deviceId, device.SlaveId, frame, index);
                    }
                }, token);
                
                tasks.Add(task);
            }
            _taskGenerators[deviceId] = tasks;
        }
        
        // Start staleness check using PeriodicTimer (every 1 second)
        _stalenessCheckTask = Task.Run(async () =>
        {
            using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await periodicTimer.WaitForNextTickAsync(token))
            {
                _dataQualityService.CheckStaleness();
        if (!_isRunning)
        {
            return;
        }

            }
        }, token);
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        // Cancel all periodic timers
        _cts?.Cancel();
        
        // Wait for staleness check task to complete with timeout
        if (_stalenessCheckTask != null)
        {
            try 
            { 
                await Task.WhenAny(_stalenessCheckTask, Task.Delay(2000)); 
            } 
            catch (OperationCanceledException) { }
            _stalenessCheckTask = null;
        }
        
        // Wait for all periodic timer tasks to complete with timeout
        var allTimerTasks = _taskGenerators.Values.SelectMany(x => x).ToList();
        if (allTimerTasks.Any())
        {
            try
            {
                await Task.WhenAny(Task.WhenAll(allTimerTasks), Task.Delay(2000));
            }
            catch (OperationCanceledException) { }
        }
        _taskGenerators.Clear();

        // Stop all workers with timeout
        var stopTasks = _workers.Values.Select(w => w.StopAsync()).ToList();
        if (stopTasks.Any())
        {
            try
            {
                await Task.WhenAny(Task.WhenAll(stopTasks), Task.Delay(5000));
            }
            catch { }
        }
        _workers.Clear();
        
        // Clear device queues
        _deviceQueues.Clear();
        
        // Close all TCP connections - they will be recreated on next start
        _driver?.CloseAllConnections();
        
        // Dispose cancellation token source
        _cts?.Dispose();
        _cts = null;
    }

    private void GenerateScanTask(string deviceId, byte slaveId, FrameConfig frame, int frameIndex)
    {
        var task = new ScanTask
        {
            DeviceId = deviceId,
            SlaveId = slaveId,
            StartAddress = frame.StartAddress,
            Count = frame.Count,
            FrameIndex = frameIndex,
            CreatedAt = DateTime.UtcNow
        };

        _metricCollector.RecordTaskCreated();
        
        // Get the frame-specific queue (or shared queue if concurrent reads disabled)
        if (_workers.TryGetValue(deviceId, out var worker))
        {
            var queue = worker.GetQueueForFrame(frameIndex);
            if (!queue.TryEnqueue(task))
            {
                // Task was dropped - record it for this specific frame
                var frameId = $"{deviceId}:{frame.StartAddress}:{frameIndex}";
                _metricCollector.RecordTaskDropped(frameId);
                Console.WriteLine($"Task dropped for frame {frameId}");
            }
        }
    }

    public System.Collections.Concurrent.ConcurrentQueue<long> GetAllDroppedTimestamps()
    {
        var allDropped = new System.Collections.Concurrent.ConcurrentQueue<long>();
        foreach (var worker in _workers.Values)
        {
            foreach (var queue in worker.GetAllQueues())
            {
                foreach (var timestamp in queue.DroppedTimestamps)
                {
                    allDropped.Enqueue(timestamp);
                }
            }
        }
        return allDropped;
    }

    public object GetQueueStats()
    {
        long totalSize = 0;
        long totalEnqueued = 0;
        long totalDequeued = 0;
        long totalDropped = 0;

        foreach (var worker in _workers.Values)
        {
            foreach (var queue in worker.GetAllQueues())
            {
                totalSize += queue.Count;
                totalEnqueued += queue.EnqueuedCount;
                totalDequeued += queue.DequeuedCount;
                totalDropped += queue.DroppedCount;
            }
        }

        return new
        {
            currentSize = totalSize,
            totalEnqueued = totalEnqueued,
            totalDequeued = totalDequeued,
            totalDropped = totalDropped
        };
    }
}
