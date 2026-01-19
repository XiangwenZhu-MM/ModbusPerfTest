using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class ClockDriftController : ControllerBase
{
    private readonly ClockDriftService _clockDriftService;

    public ClockDriftController(ClockDriftService clockDriftService)
    {
        _clockDriftService = clockDriftService;
    }

    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        var stats = _clockDriftService.GetStatistics();
        return Ok(stats);
    }

    [HttpGet("measurements")]
    public IActionResult GetMeasurements([FromQuery] int count = 100)
    {
        var measurements = _clockDriftService.GetRecentMeasurements(count);
        return Ok(measurements);
    }
}
