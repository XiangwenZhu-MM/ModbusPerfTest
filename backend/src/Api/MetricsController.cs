using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly MetricCollector _metricCollector;
    private readonly DeviceScanManager _scanManager;

    public MetricsController(
        MetricCollector metricCollector,
        DeviceScanManager scanManager)
    {
        _metricCollector = metricCollector;
        _scanManager = scanManager;
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
}
