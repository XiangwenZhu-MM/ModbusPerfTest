using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class DataQualityController : ControllerBase
{
    private readonly DataQualityService _dataQualityService;

    public DataQualityController(DataQualityService dataQualityService)
    {
        _dataQualityService = dataQualityService;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var summary = _dataQualityService.GetSummary();
        return Ok(summary);
    }

    [HttpGet("datapoints")]
    public IActionResult GetAllDataPoints()
    {
        var dataPoints = _dataQualityService.GetAllDataPoints();
        return Ok(dataPoints);
    }

    [HttpGet("datapoints/{dataPointId}")]
    public IActionResult GetDataPoint(string dataPointId)
    {
        var state = _dataQualityService.GetDataPointState(dataPointId);
        
        if (state == null)
        {
            return NotFound($"Data point {dataPointId} not found");
        }

        return Ok(state);
    }
}
