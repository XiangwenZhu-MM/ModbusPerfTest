using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class ThreadPoolController : ControllerBase
{
    private readonly ScadaHealthMonitor _healthMonitor;

    public ThreadPoolController(ScadaHealthMonitor healthMonitor)
    {
        _healthMonitor = healthMonitor;
    }

    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        var metrics = _healthMonitor.GetCurrentMetrics();
        return Ok(metrics);
    }
}
