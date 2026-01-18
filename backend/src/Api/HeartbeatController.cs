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

    /// <summary>
    /// Gets the current real-time heartbeat metrics.
    /// </summary>
    /// <returns>Current heartbeat metrics including latency and clock drift.</returns>
    [HttpGet("metrics")]
    public ActionResult<HeartbeatMetrics> GetMetrics()
    {
        try
        {
            var metrics = _heartbeatMonitor.GetCurrentMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve heartbeat metrics");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Gets the metrics history (last 10 measurements, newest first).
    /// </summary>
    /// <returns>List of recent heartbeat metrics.</returns>
    [HttpGet("metrics/history")]
    public ActionResult<IEnumerable<HeartbeatMetrics>> GetMetricsHistory()
    {
        try
        {
            var history = _heartbeatMonitor.GetMetricsHistory();
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve heartbeat metrics history");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Simulates high CPU load to test heartbeat latency detection.
    /// </summary>
    /// <param name="durationMs">Duration of CPU load in milliseconds (default: 3000ms, max: 10000ms).</param>
    /// <returns>Confirmation message.</returns>
    [HttpPost("simulate-load")]
    public ActionResult SimulateLoad([FromQuery] int durationMs = 3000)
    {
        try
        {
            // Limit duration to prevent excessive load
            var actualDuration = Math.Min(durationMs, 10000);
            
            _logger.LogInformation("Simulating CPU load for {DurationMs}ms across all cores", actualDuration);

            // Simulate CPU-intensive work across all cores to saturate the system
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var processorCount = Environment.ProcessorCount;
            var tasks = new Task[processorCount];
            long totalIterations = 0;

            // Spawn CPU-intensive work on all cores
            for (int core = 0; core < processorCount; core++)
            {
                tasks[core] = Task.Run(() =>
                {
                    long iterations = 0;
                    var localStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    while (localStopwatch.ElapsedMilliseconds < actualDuration)
                    {
                        // CPU-intensive calculation
                        for (int i = 0; i < 1000000; i++)
                        {
                            var _ = Math.Sqrt(i) * Math.Log(i + 1) * Math.Sin(i);
                        }
                        iterations++;
                    }
                    
                    return iterations;
                });
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks);
            
            foreach (var task in tasks.Cast<Task<long>>())
            {
                totalIterations += task.Result;
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "CPU load simulation completed: {DurationMs}ms, {Cores} cores, {Iterations} total iterations",
                stopwatch.ElapsedMilliseconds,
                processorCount,
                totalIterations
            );

            return Ok(new
            {
                message = "CPU load simulation completed",
                requestedDurationMs = durationMs,
                actualDurationMs = stopwatch.ElapsedMilliseconds,
                coresUsed = processorCount,
                totalIterations = totalIterations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate CPU load");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Triggers a full blocking GC to test heartbeat latency detection for GC pauses.
    /// </summary>
    /// <returns>Confirmation message with GC pause duration.</returns>
    [HttpPost("simulate-gc")]
    public ActionResult SimulateGC()
    {
        try
        {
            _logger.LogInformation("Triggering full blocking GC to test latency detection");

            // Allocate junk data to make GC pause longer and more noticeable
            var junkData = new List<object>();
            for (int i = 0; i < 1000000; i++)
            {
                junkData.Add(new byte[1024]); // 1KB per object = ~1GB total
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Get GC info before
            var memoryBefore = GC.GetTotalMemory(false);
            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);

            // Trigger full blocking compacting GC (Stop-the-World event)
            // This is the most aggressive GC that will cause maximum latency
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            stopwatch.Stop();

            // Get GC info after
            var memoryAfter = GC.GetTotalMemory(false);
            var gen0After = GC.CollectionCount(0);
            var gen1After = GC.CollectionCount(1);
            var gen2After = GC.CollectionCount(2);

            // Get detailed GC pause information
            var gcInfo = GC.GetGCMemoryInfo();
            var pauseDurations = gcInfo.PauseDurations
                .Select(p => p.TotalMilliseconds)
                .ToList();

            _logger.LogInformation(
                "GC simulation completed: Total={TotalMs}ms, Gen0={Gen0}, Gen1={Gen1}, Gen2={Gen2}, Pauses={Pauses}",
                stopwatch.ElapsedMilliseconds,
                gen0After - gen0Before,
                gen1After - gen1Before,
                gen2After - gen2Before,
                string.Join(", ", pauseDurations.Select(p => $"{p:F1}ms"))
            );

            return Ok(new
            {
                message = "GC simulation completed",
                totalDurationMs = stopwatch.ElapsedMilliseconds,
                memoryFreedBytes = memoryBefore - memoryAfter,
                gen0Collections = gen0After - gen0Before,
                gen1Collections = gen1After - gen1Before,
                gen2Collections = gen2After - gen2Before,
                pauseDurationsMs = pauseDurations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate GC");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }
}
