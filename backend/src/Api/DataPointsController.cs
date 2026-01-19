using Microsoft.AspNetCore.Mvc;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Api;

[ApiController]
[Route("api/[controller]")]
public class DataPointsController : ControllerBase
{
    private readonly DataPointRepository _repository;

    public DataPointsController(DataPointRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetDataPointCounts()
    {
        var result = await _repository.GetDataPointCountsAsync();
        return Ok(result);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAllData()
    {
        await _repository.ClearAllDataAsync();
        return Ok(new { message = "All data points have been deleted" });
    }
}
