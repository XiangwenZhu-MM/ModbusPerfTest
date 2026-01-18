using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class DeviceConfigController : ControllerBase
{
    private readonly DeviceConfigService _configService;
    private readonly DeviceScanManager _scanManager;

    public DeviceConfigController(
        DeviceConfigService configService,
        DeviceScanManager scanManager)
    {
        _configService = configService;
        _scanManager = scanManager;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadConfiguration([FromBody] string jsonContent)
    {
        var success = await _configService.LoadFromJsonAsync(jsonContent);
        
        if (!success)
        {
            return BadRequest("Invalid configuration format");
        }

        // Stop existing monitoring and start with new config
        await _scanManager.StopMonitoringAsync();
        var devices = _configService.GetDevices();
        _scanManager.StartMonitoring(devices);

        return Ok(new { message = "Configuration loaded successfully", deviceCount = devices.Count });
    }

    [HttpGet("current")]
    public IActionResult GetCurrentConfiguration()
    {
        var config = _configService.CurrentConfiguration;
        
        if (config == null)
        {
            return NotFound("No configuration loaded");
        }

        return Ok(config);
    }
}
