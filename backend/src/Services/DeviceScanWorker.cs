using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class DeviceScanWorker
{
    private readonly string _deviceId;
    private readonly DeviceConfig _config;
    private readonly IModbusDriver _driver;
    private readonly ScanTaskQueue _taskQueue;
    private readonly ScanTaskQueue _globalQueue; // For metrics only
    private readonly MetricCollector _metricCollector;
    private readonly ClockDriftService _clockDriftService;
    private readonly DataQualityService _dataQualityService;
    private readonly SemaphoreSlim _deviceLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _workerTask;

    public DeviceScanWorker(
        string deviceId,
        DeviceConfig config,
        IModbusDriver driver,
        ScanTaskQueue taskQueue,
        ScanTaskQueue globalQueue,
        MetricCollector metricCollector,
        ClockDriftService clockDriftService,
        DataQualityService dataQualityService)
    {
        _deviceId = deviceId;
        _config = config;
        _driver = driver;
        _taskQueue = taskQueue;
        _globalQueue = globalQueue;
        _metricCollector = metricCollector;
        _clockDriftService = clockDriftService;
        _dataQualityService = dataQualityService;
    }

    public ScanTaskQueue TaskQueue => _taskQueue;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(() => WorkerLoop(_cts.Token));
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_workerTask != null)
        {
            await _workerTask;
        }
    }

    private async Task WorkerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var task = await _taskQueue.DequeueAsync(cancellationToken);
                if (task == null)
                    continue;

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
