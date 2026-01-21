using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Models;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly RuntimeConfigService _configService;
    private readonly DeviceScanManager _scanManager;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        RuntimeConfigService configService,
        DeviceScanManager scanManager,
        ILogger<ConfigurationController> logger)
    {
        _configService = configService;
        _scanManager = scanManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetConfiguration()
    {
        try
        {
            var config = _configService.GetConfig();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return StatusCode(500, new { message = "Failed to get configuration", error = ex.Message });
        }
    }

    [HttpPost("apply")]
    public IActionResult ApplyConfiguration([FromBody] RuntimeConfig config)
    {
        try
        {
            // Validate that scanning is stopped
            if (_scanManager.IsRunning)
            {
                return BadRequest(new { message = "Cannot apply configuration while scanning is running. Please stop scanning first." });
            }

            // Validate configuration
            if (string.IsNullOrWhiteSpace(config.DataStorageBackend))
            {
                return BadRequest(new { message = "DataStorageBackend cannot be empty" });
            }

            if (config.DataStorageBackend != "SQLite" && config.DataStorageBackend != "InfluxDB")
            {
                return BadRequest(new { message = "DataStorageBackend must be either 'SQLite' or 'InfluxDB'" });
            }

            if (config.MinWorkerThreads < 1 || config.MinWorkerThreads > 1000)
            {
                return BadRequest(new { message = "MinWorkerThreads must be between 1 and 1000" });
            }

            var currentConfig = _configService.GetConfig();
            bool requiresRestart = config.UseAsyncRead != currentConfig.UseAsyncRead ||
                                 config.UseAsyncNModbus != currentConfig.UseAsyncNModbus ||
                                 config.DataStorageBackend != currentConfig.DataStorageBackend;

            // Update configuration
            _configService.UpdateConfig(config);
            _logger.LogInformation("Configuration updated: UseAsyncRead={UseAsyncRead}, UseAsyncNModbus={UseAsyncNModbus}, AllowConcurrentFrameReads={AllowConcurrentFrameReads}, DataStorageBackend={DataStorageBackend}, MinWorkerThreads={MinWorkerThreads}",
                config.UseAsyncRead, config.UseAsyncNModbus, config.AllowConcurrentFrameReads, config.DataStorageBackend, config.MinWorkerThreads);

            return Ok(new 
            { 
                message = "Configuration applied successfully." + (requiresRestart ? " Note: Driver and Backend settings require scan restart to take effect." : ""),
                requiresRestart = requiresRestart
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying configuration");
            return StatusCode(500, new { message = "Failed to apply configuration", error = ex.Message });
        }
    }
}
