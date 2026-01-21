using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class DataPointsController : ControllerBase
{
    private readonly IDataPointRepository _repository;
    private readonly DeviceConfigService _configService;

    public DataPointsController(IDataPointRepository repository, DeviceConfigService configService)
    {
        _repository = repository;
        _configService = configService;
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetDataPointCounts()
    {
        var result = await _repository.GetDataPointCountsAsync();
        
        // Enrich with theoretical counts based on current configuration
        var tpm = _configService.CalculateTheoreticalTPM();
        
        EnrichTimeRange(result.LastMinute, 1.0, tpm);
        EnrichTimeRange(result.Last10Minutes, 10.0, tpm);
        EnrichTimeRange(result.LastHour, 60.0, tpm);
        EnrichTimeRange(result.Last2Hours, 120.0, tpm);
        
        return Ok(result);
    }

    private void EnrichTimeRange(TimeRangeCount range, double minutes, double tpm)
    {
        if (range == null) return;
        
        range.TheoreticalCount = (long)Math.Round(tpm * minutes);
        if (range.TheoreticalCount > 0)
        {
            range.MissingRate = Math.Max(0, (1.0 - (double)range.Count / range.TheoreticalCount) * 100.0);
        }
        else
        {
            range.MissingRate = 0;
        }
    }

    [HttpGet("devices/last-minute")]
    public async Task<IActionResult> GetDeviceCountsLastMinute()
    {
        var end = DateTime.UtcNow;
        var start = end.AddMinutes(-1);
        var results = await _repository.GetDeviceCountsAsync(start, end);
        return Ok(results);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAllData()
    {
        await _repository.ClearAllDataAsync();
        return Ok(new { message = "All data points have been deleted" });
    }
}
