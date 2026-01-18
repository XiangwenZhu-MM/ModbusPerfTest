using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Models;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

/// <summary>
/// API controller for retrieving heartbeat monitor warnings and configuration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HeartbeatController : ControllerBase
{
    private readonly HeartbeatMonitor _heartbeatMonitor;
    private readonly ILogger<HeartbeatController> _logger;

    public HeartbeatController(HeartbeatMonitor heartbeatMonitor, ILogger<HeartbeatController> logger)
    {
        _heartbeatMonitor = heartbeatMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Gets recent heartbeat warnings (newest first).
    /// </summary>
    /// <returns>List of recent drift events.</returns>
    [HttpGet("warnings")]
    public ActionResult<IEnumerable<DriftEvent>> GetWarnings()
    {
        try
        {
            var warnings = _heartbeatMonitor.GetRecentWarnings();
            return Ok(warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve heartbeat warnings");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Gets the current heartbeat monitor configuration.
    /// </summary>
    /// <returns>Heartbeat configuration.</returns>
    [HttpGet("config")]
    public ActionResult<HeartbeatConfig> GetConfig()
    {
        try
        {
            var config = _heartbeatMonitor.GetConfig();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve heartbeat configuration");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }
}
