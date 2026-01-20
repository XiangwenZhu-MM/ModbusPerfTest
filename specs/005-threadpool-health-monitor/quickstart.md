# ThreadPool Health Monitor - Quickstart Guide

## Overview

The ThreadPool Health Monitor provides real-time visibility into .NET thread pool and task queue metrics for the SCADA backend. This helps detect performance bottlenecks, thread pool starvation, and task backlog issues before they impact system reliability.

## Features

- **Real-time Metrics**: Monitor active threads, IO threads, pending work items, and pool limits
- **Live Logging**: Metrics logged every second during operation
- **Automatic Alerting**: Warnings when pending work items exceed threshold
- **Low Overhead**: <1% CPU impact, non-blocking design

## Configuration

Edit `backend/appsettings.json` or `backend/appsettings.Development.json`:

```json
{
  "ThreadPoolMonitor": {
    "AlertThreshold": 10,
    "IntervalMs": 1000
  }
}
```

**Settings**:
- `AlertThreshold`: Number of pending work items before logging a warning (default: 10)
- `IntervalMs`: Metrics update interval in milliseconds (default: 1000)

## Metrics Explained

### WorkerThreads
Current number of active threads handling application logic. High values indicate high concurrency load.

### CompletionPortThreads
Threads handling network I/O callbacks (critical for Modbus TCP). High values indicate many simultaneous network requests.

### PendingWorkItems
Tasks waiting for a thread to execute them. **This is your early warning metric!**
- **Normal**: 0 or very low (<5)
- **Warning**: >10 indicates potential thread starvation
- **Critical**: Growing linearly (0→200→400) indicates system failure

### Pool Range
Min and Max worker thread limits configured for the thread pool.

## Monitoring Output

Example log output:

```
--- SCADA ENGINE HEALTH ---
Active Threads: 24
Queue Length:   0 (Should be 0!)
IO Threads:     12
Pool Range:     8 to 32767
```

**Warning Example**:
```
WARNING: System is lagging! Check for blocking code. PendingWorkItems=25
```

## Troubleshooting

### High PendingWorkItems

**Cause**: Thread pool starvation, usually from blocking calls like `.Result` or `Task.Wait()` instead of `await`.

**Fix**: 
1. Search codebase for `.Result`, `.Wait()`, `GetAwaiter().GetResult()`
2. Replace with proper `async/await` patterns
3. Ensure all Modbus operations use `ReadHoldingRegistersAsync` with `await`

### Growing Queue Length

**Cause**: Devices offline, network timeouts, or slow responses without proper timeout handling.

**Fix**:
1. Check device connectivity
2. Verify timeout settings for Modbus operations
3. Review error handling for failed operations

## Integration

The monitor starts automatically with the backend service. No manual startup required.

To stop monitoring (if needed):
```csharp
var healthMonitor = app.Services.GetRequiredService<ScadaHealthMonitor>();
healthMonitor.Stop();
```

## Best Practices

1. **Keep PendingWorkItems at 0**: If it's consistently >0, investigate thread starvation
2. **Monitor during load tests**: Verify metrics remain healthy under 200+ device load
3. **Correlate with system events**: Check metrics logs when investigating production issues
4. **Alert on trends**: If PendingWorkItems grows linearly, take immediate action

## Technical Details

- **Implementation**: `backend/src/Services/ScadaHealthMonitor.cs`
- **Startup**: Registered and started in `backend/Program.cs`
- **Thread Safety**: Uses non-blocking async patterns
- **Performance**: Metrics collection uses built-in .NET ThreadPool APIs (minimal overhead)

## Support

For issues or questions, review the implementation in:
- Spec: `specs/005-threadpool-health-monitor/spec.md`
- Plan: `specs/005-threadpool-health-monitor/plan.md`
- Tasks: `specs/005-threadpool-health-monitor/tasks.md`
