using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly MetricCollector _metricCollector;
    private readonly DeviceScanManager _scanManager;
    private readonly DeviceConfigService _configService;

    public MetricsController(
        MetricCollector metricCollector,
        DeviceScanManager scanManager,
        DeviceConfigService configService)
    {
        _metricCollector = metricCollector;
        _scanManager = scanManager;
        _configService = configService;
    }

    [HttpGet("device")]
    public IActionResult GetDeviceMetrics([FromQuery] int count = 100)
    {
        var metrics = _metricCollector.GetRecentDeviceMetrics(count);
        return Ok(metrics);
    }

    [HttpGet("system")]
    public IActionResult GetSystemHealth()
    {
        var droppedTimestamps = _scanManager.GetAllDroppedTimestamps();
        var health = _metricCollector.CalculateSystemHealth(droppedTimestamps);
        return Ok(health);
    }

    [HttpGet("queue")]
    public IActionResult GetQueueStats()
    {
        return Ok(_scanManager.GetQueueStats());
    }

    [HttpGet("frames")]
    public IActionResult GetAllFrames()
    {
        var devices = _configService.GetDevices();
        var recentMetrics = _metricCollector.GetRecentDeviceMetrics(1000);
        
        var frames = new List<object>();
        
        foreach (var device in devices)
        {
            for (int frameIndex = 0; frameIndex < device.Frames.Count; frameIndex++)
            {
                var frame = device.Frames[frameIndex];
                var frameId = $"{device.IpAddress}:{device.Port}:{device.SlaveId}:{frame.StartAddress}:{frameIndex}";
                
                // Get metrics for this frame
                var frameMetrics = recentMetrics.Where(m => m.FrameId == frameId).ToList();
                
                var frameData = new
                {
                    DeviceName = device.Name,
                    FrameName = frame.Name,
                    FrameId = frameId,
                    IpAddress = device.IpAddress,
                    Port = device.Port,
                    SlaveId = device.SlaveId,
                    FrameIndex = frameIndex,
                    StartAddress = frame.StartAddress,
                    Count = frame.Count,
                    ScanFrequencyMs = frame.ScanFrequencyMs,
                    HasMetrics = frameMetrics.Any(),
                    MetricsCount = frameMetrics.Count,
                    LatestMetric = frameMetrics.LastOrDefault(),
                    MeanMetrics = frameMetrics.Any() ? new
                    {
                        QueueDurationMs = frameMetrics.Average(m => m.QueueDurationMs),
                        DeviceResponseTimeMs = frameMetrics.Average(m => m.DeviceResponseTimeMs),
                        ActualSamplingIntervalMs = frameMetrics.Average(m => m.ActualSamplingIntervalMs)
                    } : null,
                    DroppedCount = _metricCollector.GetFrameDroppedCount(frameId),
                    DroppedTPM = _metricCollector.GetFrameDroppedTPM(frameId)
                };
                
                frames.Add(frameData);
            }
        }
        
        return Ok(frames);
    }
}
