# Research: Diagnostic Heartbeat Monitor

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)  
**Date**: January 18, 2026  
**Status**: Complete

## Research Questions

### Q1: How to implement high-resolution monotonic timing in .NET 10?

**Decision**: Use `System.Diagnostics.Stopwatch` for monotonic timing and `System.Threading.PeriodicTimer` for interval pulsing.

**Rationale**:
- `Stopwatch` provides high-resolution monotonic timing (uses QueryPerformanceCounter on Windows, clock_gettime on Linux)
- Independent of system clock changes - continues incrementing even if wall clock is adjusted
- `PeriodicTimer` (introduced in .NET 6) is the modern replacement for System.Threading.Timer
- Designed for async/await patterns with `WaitForNextTickAsync()`
- More accurate than Timer for regular intervals
- Both are built into .NET runtime (no external dependencies)

**Alternatives considered**:
- `DateTime.UtcNow` - Rejected: affected by system clock changes, not monotonic
- `System.Threading.Timer` - Rejected: callback-based (not async), less accurate than PeriodicTimer
- External timing libraries - Rejected: unnecessary complexity, built-in APIs sufficient

**Implementation pattern**:
```csharp
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
Stopwatch sw = Stopwatch.StartNew();
DateTime lastWallClock = DateTime.UtcNow;

while (await timer.WaitForNextTickAsync(cancellationToken))
{
    long monoElapsed = sw.ElapsedMilliseconds;
    DateTime currentWall = DateTime.UtcNow;
    double wallElapsed = (currentWall - lastWallClock).TotalMilliseconds;
    
    // Detection logic here
    
    sw.Restart();
    lastWallClock = currentWall;
}
```

### Q2: What's the best practice for background service hosting in ASP.NET Core?

**Decision**: Implement `IHostedService` or extend `BackgroundService` base class.

**Rationale**:
- `BackgroundService` is the standard pattern for long-running background tasks in ASP.NET Core
- Provides `ExecuteAsync()` method with automatic lifetime management
- Integrated with application lifecycle (starts on app start, stops on shutdown)
- Supports graceful cancellation via CancellationToken
- Registered in `Program.cs` with `builder.Services.AddHostedService<T>()`

**Alternatives considered**:
- Manual thread creation - Rejected: poor lifecycle management, no graceful shutdown
- Task.Run in Configure() - Rejected: not integrated with DI/lifecycle, hard to test
- Third-party job schedulers (Hangfire, Quartz) - Rejected: overkill for simple periodic task

**Implementation pattern**:
```csharp
public class HeartbeatMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Monitoring loop using PeriodicTimer
    }
}

// In Program.cs:
builder.Services.AddHostedService<HeartbeatMonitor>();
```

### Q3: How to implement file-based logging with rotation in .NET?

**Decision**: Use simple file I/O with size-based rotation (check file size, rename when threshold exceeded).

**Rationale**:
- Simple implementation without external dependencies
- Size-based rotation prevents unbounded disk usage
- Pattern: When log file exceeds max size, rename to `.old` and start new file
- For SCADA diagnostic logs, simple rotation is sufficient (not high-volume transactional logs)
- File I/O operations are fast enough for infrequent warnings (non-intrusive)

**Alternatives considered**:
- Serilog with file sink - Rejected: adds dependency, overkill for single log file
- NLog - Rejected: same as Serilog, unnecessary for simple use case
- ILogger with file provider - Rejected: less control over rotation policy
- Database logging - Rejected: unnecessary complexity, file-based meets requirements

**Implementation pattern**:
```csharp
private readonly string _logPath = "logs/heartbeat-warnings.log";
private readonly long _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB

private void WriteLog(string message)
{
    var fileInfo = new FileInfo(_logPath);
    if (fileInfo.Exists && fileInfo.Length > _maxFileSizeBytes)
    {
        File.Move(_logPath, _logPath + ".old", overwrite: true);
    }
    
    File.AppendAllText(_logPath, $"{DateTime.UtcNow:O} {message}\n");
}
```

### Q4: How to detect the two types of drift (Internal Latency vs System Drift)?

**Decision**: Compare monotonic timer (Stopwatch) against expected interval for internal latency, compare monotonic vs wall clock for system drift.

**Rationale**:
- **Internal Latency**: If Stopwatch shows elapsed time > threshold, the process was delayed (CPU spike, GC pause, thread starvation)
- **System Drift**: If Stopwatch and wall clock (DateTime.UtcNow) diverge significantly, the system clock was changed
- Use 500ms divergence threshold for clock drift detection (reasonable for NTP sync scenarios)
- Two independent checks provide complete coverage of both failure modes

**Detection algorithm**:
```csharp
long monoMs = stopwatch.ElapsedMilliseconds;
double wallMs = (DateTime.UtcNow - lastWallTime).TotalMilliseconds;

// Detection 1: Internal Latency
if (monoMs > thresholdMs)
{
    LogWarning("PERFORMANCE_DEGRADED", 
        $"Expected {intervalMs}ms, took {monoMs}ms");
}

// Detection 2: System Clock Drift
if (Math.Abs(monoMs - wallMs) > 500)
{
    LogWarning("CLOCK_SHIFT", 
        $"Mono: {monoMs}ms vs Wall: {wallMs:F1}ms");
}
```

**Alternatives considered**:
- Single combined check - Rejected: cannot distinguish between internal and external causes
- Only wall clock monitoring - Rejected: cannot detect internal latency
- Complex NTP queries - Rejected: unnecessary, wall vs mono comparison is sufficient

### Q5: How to efficiently share recent warnings between backend service and API endpoint?

**Decision**: Use thread-safe in-memory circular buffer (bounded concurrent queue).

**Rationale**:
- `ConcurrentQueue<T>` provides lock-free thread-safe operations
- Background service enqueues warnings, API endpoint reads them
- Bound size to 50 entries (spec requirement) by dequeuing oldest when full
- No database overhead for high-frequency reads
- Warnings persist in log file; in-memory cache is for UI display only

**Implementation pattern**:
```csharp
public class HeartbeatMonitor
{
    private readonly ConcurrentQueue<DriftEvent> _recentWarnings = new();
    private readonly int _maxWarnings = 50;
    
    private void RecordWarning(DriftEvent evt)
    {
        _recentWarnings.Enqueue(evt);
        
        while (_recentWarnings.Count > _maxWarnings)
        {
            _recentWarnings.TryDequeue(out _);
        }
    }
    
    public IEnumerable<DriftEvent> GetRecentWarnings()
    {
        return _recentWarnings.Reverse(); // Newest first
    }
}
```

**Alternatives considered**:
- Database storage - Rejected: unnecessary I/O overhead for transient UI data
- Redis/external cache - Rejected: adds deployment complexity
- Static list with locks - Rejected: ConcurrentQueue is more efficient

### Q6: What configuration options should be exposed?

**Decision**: Expose interval, threshold, max warnings, and enable/disable flag in `appsettings.json`.

**Rationale**:
- Interval and threshold are core parameters mentioned in spec (defaults: 1000ms, 2000ms)
- Max warnings limit prevents unbounded memory growth (default: 50)
- Enable/disable flag allows operators to control monitoring without code changes
- Configuration via appsettings.json follows existing project patterns

**Configuration structure**:
```json
{
  "HeartbeatMonitor": {
    "Enabled": true,
    "IntervalMs": 1000,
    "ThresholdMs": 2000,
    "MaxWarningsInMemory": 50,
    "LogFilePath": "logs/heartbeat-warnings.log",
    "MaxLogFileSizeMB": 10
  }
}
```

**Alternatives considered**:
- Environment variables - Rejected: less structured, harder to validate
- Database configuration - Rejected: unnecessary dependency
- Hardcoded values - Rejected: spec requires configurability

### Q7: How to integrate warning display into existing System Metrics UI?

**Decision**: Create standalone React component `HeartbeatWarnings.tsx`, fetch warnings via REST API, display in SystemHealthPanel.

**Rationale**:
- Existing `SystemHealthPanel.tsx` component already displays system metrics
- Add new API endpoint `GET /api/heartbeat/warnings` that returns recent warnings
- Poll endpoint every 2-3 seconds to meet "within 2 seconds" requirement (spec SC-006)
- Component displays warnings in reverse chronological order (newest first)
- Styled to match existing metric panels

**Component structure**:
```typescript
interface DriftEvent {
  eventType: 'PERFORMANCE_DEGRADED' | 'CLOCK_SHIFT';
  timestamp: string;
  monoElapsedMs: number;
  wallElapsedMs: number;
  message: string;
}

const HeartbeatWarnings: React.FC = () => {
  const [warnings, setWarnings] = useState<DriftEvent[]>([]);
  
  useEffect(() => {
    const interval = setInterval(async () => {
      const data = await fetchHeartbeatWarnings();
      setWarnings(data);
    }, 2000);
    
    return () => clearInterval(interval);
  }, []);
  
  // Render warnings in table/list format
};
```

**Alternatives considered**:
- WebSocket push notifications - Rejected: adds complexity, polling is sufficient
- Server-Sent Events - Rejected: same as WebSocket, polling simpler
- Shared state management (Redux) - Rejected: overkill for single component

## Technology Stack Summary

| Component | Technology | Justification |
|-----------|------------|---------------|
| Monotonic Timer | System.Diagnostics.Stopwatch | High-resolution, clock-independent, built-in |
| Periodic Execution | System.Threading.PeriodicTimer | Modern async pattern, accurate intervals |
| Background Service | BackgroundService (IHostedService) | Standard ASP.NET Core pattern |
| File Logging | File.AppendAllText + size-based rotation | Simple, no dependencies, meets requirements |
| In-Memory Cache | ConcurrentQueue<DriftEvent> | Thread-safe, lock-free, bounded size |
| API Endpoint | ASP.NET Core Controller | Follows existing project pattern |
| Frontend Display | React Component + REST polling | Matches existing architecture |
| Configuration | appsettings.json | Standard .NET configuration |
| Testing | xUnit (backend), Jest (frontend) | Existing test frameworks |

## Performance Considerations

- **CPU Usage**: PeriodicTimer + Stopwatch operations are lightweight (~0.01% CPU per 1000ms cycle)
- **Memory Usage**: 50 DriftEvents * ~200 bytes each = ~10KB, well under 10MB budget
- **I/O Impact**: Log writes only on warnings (infrequent), append-only file I/O is fast
- **Network Impact**: Frontend polls every 2s, typical response <1KB, negligible bandwidth
- **Threading**: Single background thread for heartbeat, no thread pool saturation risk

## Open Questions Resolution

All technical unknowns from Technical Context have been resolved:
- ✅ Monotonic timing implementation: Stopwatch + PeriodicTimer
- ✅ Background service hosting: BackgroundService pattern
- ✅ File logging with rotation: Simple file I/O with size checks
- ✅ Drift detection algorithm: Dual check (mono vs threshold, mono vs wall)
- ✅ Warning sharing: ConcurrentQueue in-memory cache
- ✅ Configuration approach: appsettings.json
- ✅ UI integration: React component with REST API polling

**Status**: Research complete, ready for Phase 1 (Design & Contracts).
