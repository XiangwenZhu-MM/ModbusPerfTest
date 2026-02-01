using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class DeviceScanManager
{
    private readonly IModbusDriver _driver;
    private readonly ScanTaskQueue _globalQueue; // For metrics aggregation
    private readonly MetricCollector _metricCollector;
    private readonly ClockDriftService _clockDriftService;
    private readonly DataQualityService _dataQualityService;
    private readonly Dictionary<string, DeviceScanWorker> _workers = new();
    private readonly Dictionary<string, ScanTaskQueue> _deviceQueues = new();
    private readonly Dictionary<string, List<Timer>> _taskGenerators = new();
    private Timer? _stalenessCheckTimer;

    public DeviceScanManager(
        IModbusDriver driver,
        ScanTaskQueue taskQueue,
        MetricCollector metricCollector,
        ClockDriftService clockDriftService,
        DataQualityService dataQualityService)
    {
        _driver = driver;
        _globalQueue = taskQueue;
        _metricCollector = metricCollector;
        _clockDriftService = clockDriftService;
        _dataQualityService = dataQualityService;
    }

    public void StartMonitoring(List<DeviceConfig> devices)
    {
        if (devices == null || devices.Count == 0)
        {
            return;
        }

        // Clear any existing state first
        foreach (var queue in _deviceQueues.Values)
        {
            queue.Clear();
        }
        _deviceQueues.Clear();

        foreach (var device in devices)
        {
            var deviceId = $"{device.IpAddress}:{device.Port}:{device.SlaveId}";
            
            // Create dedicated queue for this device
            var deviceQueue = new ScanTaskQueue();
            _deviceQueues[deviceId] = deviceQueue;
            
            // Create and start worker for this device
            var worker = new DeviceScanWorker(
                deviceId,
                device,
                _driver,
                deviceQueue,
                _globalQueue,
                _metricCollector,
                _clockDriftService,
                _dataQualityService
            );
            worker.Start();
            _workers[deviceId] = worker;

            // Create task generators for each frame
            var timers = new List<Timer>();
            for (int frameIndex = 0; frameIndex < device.Frames.Count; frameIndex++)
            {
                var frame = device.Frames[frameIndex];
                var index = frameIndex; // Capture for closure
                var timer = new Timer(
                    _ => GenerateScanTask(deviceId, device.SlaveId, frame, index),
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(frame.ScanFrequencyMs)
                );
                timers.Add(timer);
            }
            _taskGenerators[deviceId] = timers;
        }
        
        // Start staleness check timer (every 1 second)
        _stalenessCheckTimer = new Timer(
            _ => _dataQualityService.CheckStaleness(),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1)
        );
    }

    public async Task StopMonitoringAsync()
    {
        bool wasActive = _stalenessCheckTimer != null || _workers.Count > 0;
        
        // Stop staleness check timer
        _stalenessCheckTimer?.Dispose();
        _stalenessCheckTimer = null;
        
        // Stop all timers
        foreach (var timers in _taskGenerators.Values)
        {
            foreach (var timer in timers)
            {
                timer.Dispose();
            }
        }
        _taskGenerators.Clear();

        // Stop all workers
        foreach (var worker in _workers.Values)
        {
            await worker.StopAsync();
        }
        _workers.Clear();
        
        // Clear device queues
        foreach (var queue in _deviceQueues.Values)
        {
            queue.Clear();
        }
        _deviceQueues.Clear();
        
        if (!wasActive)
        {
            throw new NullReferenceException("Monitoring is not active");
        }
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
        
        // Get the device-specific queue
        if (_deviceQueues.TryGetValue(deviceId, out var deviceQueue))
        {
            if (!deviceQueue.TryEnqueue(task))
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
        foreach (var queue in _deviceQueues.Values)
        {
            foreach (var timestamp in queue.DroppedTimestamps)
            {
                allDropped.Enqueue(timestamp);
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

        foreach (var queue in _deviceQueues.Values)
        {
            totalSize += queue.Count;
            totalEnqueued += queue.EnqueuedCount;
            totalDequeued += queue.DequeuedCount;
            totalDropped += queue.DroppedCount;
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
