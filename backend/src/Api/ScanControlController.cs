using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class ScanControlController : ControllerBase
{
    private readonly DeviceScanManager _scanManager;
    private readonly DeviceConfigService _configService;
    private readonly RuntimeConfigService _runtimeConfigService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScanControlController> _logger;

    public ScanControlController(
        DeviceScanManager scanManager,
        DeviceConfigService configService,
        RuntimeConfigService runtimeConfigService,
        IConfiguration configuration,
        ILogger<ScanControlController> logger)
    {
        _scanManager = scanManager;
        _configService = configService;
        _runtimeConfigService = runtimeConfigService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("start")]
    public IActionResult StartScanning()
    {
        try
        {
            if (_scanManager.IsRunning)
            {
                return BadRequest(new { message = "Scanning is already running" });
            }

            var devices = _configService.GetDevices();
            if (devices.Count == 0)
            {
                return BadRequest(new { message = "No devices configured. Upload device configuration first." });
            }

            // Get current configuration
            var config = _runtimeConfigService.GetConfig();
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIoThreads);
            
            // Log active configuration
            _logger.LogInformation("========================================");
            _logger.LogInformation("Starting Device Scan with Configuration:");
            _logger.LogInformation("  UseAsyncRead: {UseAsyncRead}", config.UseAsyncRead);
            _logger.LogInformation("  UseAsyncNModbus: {UseAsyncNModbus}", config.UseAsyncNModbus);
            _logger.LogInformation("  AllowConcurrentFrameReads: {AllowConcurrent}", config.AllowConcurrentFrameReads);
            _logger.LogInformation("  Min Worker Threads: {MinWorkerThreads}", minWorkerThreads);
            _logger.LogInformation("  Data Storage Backend: {Backend}", config.DataStorageBackend);
            _logger.LogInformation("  Device Count: {DeviceCount}", devices.Count);
            _logger.LogInformation("========================================");
            
            _scanManager.StartMonitoring(devices);
            _logger.LogInformation("Device scanning started via API");
            
            return Ok(new 
            { 
                message = "Device scanning started successfully", 
                deviceCount = devices.Count,
                configuration = new
                {
                    useAsyncRead = config.UseAsyncRead,
                    useAsyncNModbus = config.UseAsyncNModbus,
                    allowConcurrentFrameReads = config.AllowConcurrentFrameReads,
                    minWorkerThreads = minWorkerThreads,
                    dataStorageBackend = config.DataStorageBackend
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting device scanning");
            return StatusCode(500, new { message = "Failed to start scanning", error = ex.Message });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopScanning()
    {
        try
        {
            if (!_scanManager.IsRunning)
            {
                return BadRequest(new { message = "Scanning is not running" });
            }

            await _scanManager.StopMonitoringAsync();
            _logger.LogInformation("Device scanning stopped via API");
            
            return Ok(new { message = "Device scanning stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping device scanning");
            return StatusCode(500, new { message = "Failed to stop scanning", error = ex.Message });
        }
    }

    [HttpGet("status")]
    public IActionResult GetScanningStatus()
    {
        try
        {
            var deviceCount = _configService.GetDevices().Count;
            return Ok(new 
            { 
                isRunning = _scanManager.IsRunning,
                deviceCount = deviceCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scanning status");
            return StatusCode(500, new { message = "Failed to get scanning status", error = ex.Message });
        }
    }
}
