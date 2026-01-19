# Data Model: Diagnostic Heartbeat Monitor

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)  
**Date**: January 18, 2026

## Entity Definitions

### DriftEvent

Represents a detected system anomaly (either internal latency or system drift).

**Purpose**: Capture all diagnostic information about a heartbeat deviation for logging and display.

**Attributes**:
- `EventType` (string, required): Type of drift detected
  - Values: `"PERFORMANCE_DEGRADED"` | `"CLOCK_SHIFT"`
  - Validation: Must be one of the two defined values
  
- `Timestamp` (DateTime, required): When the event was detected
  - Format: UTC DateTime
  - Precision: Milliseconds
  - Usage: For chronological ordering and display
  
- `MonoElapsedMs` (long, required): Elapsed time measured by monotonic timer (Stopwatch)
  - Unit: Milliseconds
  - Validation: Must be > 0
  - Purpose: Actual execution time independent of system clock
  
- `WallElapsedMs` (double, required): Elapsed time measured by system wall clock (DateTime.UtcNow)
  - Unit: Milliseconds
  - Precision: Fractional milliseconds
  - Purpose: System clock measurement for drift comparison
  
- `ExpectedIntervalMs` (int, required): The configured heartbeat interval
  - Unit: Milliseconds
  - Default: 1000
  - Purpose: Baseline for deviation calculation
  
- `DeviationMs` (double, computed): Magnitude of deviation
  - Calculation: For internal latency: `MonoElapsedMs - ExpectedIntervalMs`
  - Calculation: For clock shift: `Math.Abs(MonoElapsedMs - WallElapsedMs)`
  - Unit: Milliseconds
  - Purpose: Quantify severity of drift
  
- `Message` (string, required): Human-readable description of the event
  - Max length: 500 characters
  - Format: Includes event type, expected vs actual values
  - Example: `"Expected 1000ms, took 2345ms (Potential CPU/GC spike)"`

**Relationships**:
- No foreign keys or relationships (self-contained value object)
- Multiple DriftEvents may reference the same HeartbeatConfig (via ExpectedIntervalMs value)

**State Transitions**:
- Immutable once created (read-only after construction)
- Lifecycle: Created → Logged to file → Added to in-memory cache → Retrieved by API → Displayed in UI → Evicted from cache (when >50 entries)

**Validation Rules**:
- EventType must be "PERFORMANCE_DEGRADED" or "CLOCK_SHIFT"
- All time values must be positive
- Timestamp must not be in the future
- Message must not be empty

**C# Implementation**:
```csharp
public class DriftEvent
{
    public string EventType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public long MonoElapsedMs { get; init; }
    public double WallElapsedMs { get; init; }
    public int ExpectedIntervalMs { get; init; }
    
    public double DeviationMs => EventType == "PERFORMANCE_DEGRADED" 
        ? MonoElapsedMs - ExpectedIntervalMs 
        : Math.Abs(MonoElapsedMs - WallElapsedMs);
    
    public string Message { get; init; } = string.Empty;
}
```

---

### HeartbeatConfig

Represents configuration settings for the heartbeat monitor.

**Purpose**: Encapsulate all tunable parameters for heartbeat behavior and thresholds.

**Attributes**:
- `Enabled` (bool, required): Whether heartbeat monitoring is active
  - Default: `true`
  - Purpose: Allow operators to disable monitoring without code changes
  
- `IntervalMs` (int, required): Milliseconds between heartbeat pulses
  - Default: 1000
  - Validation: Must be >= 100 (minimum 100ms to avoid excessive overhead)
  - Validation: Must be <= 60000 (maximum 1 minute for reasonable detection)
  - Purpose: Controls frequency of health checks
  
- `ThresholdMs` (int, required): Deviation threshold for triggering warnings
  - Default: 2000
  - Validation: Must be >= IntervalMs (threshold cannot be less than interval)
  - Recommended: 200% of IntervalMs (2x interval)
  - Purpose: Defines acceptable tolerance before alerting
  
- `MaxWarningsInMemory` (int, required): Maximum warnings retained in cache for UI
  - Default: 50
  - Validation: Must be >= 10 and <= 1000
  - Purpose: Bound memory usage for in-memory warning cache
  
- `LogFilePath` (string, required): Path to warning log file
  - Default: `"logs/heartbeat-warnings.log"`
  - Validation: Must be writable path
  - Purpose: Location for persistent warning storage
  
- `MaxLogFileSizeMB` (int, required): Maximum log file size before rotation
  - Default: 10
  - Unit: Megabytes
  - Validation: Must be >= 1 and <= 1000
  - Purpose: Prevent unbounded disk usage

**Relationships**:
- Referenced by HeartbeatMonitor service (composition)
- No persistence (loaded from appsettings.json on startup)

**Validation Rules**:
- ThresholdMs >= IntervalMs (enforced at configuration load time)
- IntervalMs in range [100, 60000]
- MaxWarningsInMemory in range [10, 1000]
- MaxLogFileSizeMB in range [1, 1000]
- LogFilePath must be non-empty and valid directory path

**C# Implementation**:
```csharp
public class HeartbeatConfig
{
    public bool Enabled { get; set; } = true;
    public int IntervalMs { get; set; } = 1000;
    public int ThresholdMs { get; set; } = 2000;
    public int MaxWarningsInMemory { get; set; } = 50;
    public string LogFilePath { get; set; } = "logs/heartbeat-warnings.log";
    public int MaxLogFileSizeMB { get; set; } = 10;
    
    public void Validate()
    {
        if (IntervalMs < 100 || IntervalMs > 60000)
            throw new ArgumentException("IntervalMs must be between 100 and 60000");
        
        if (ThresholdMs < IntervalMs)
            throw new ArgumentException("ThresholdMs must be >= IntervalMs");
        
        if (MaxWarningsInMemory < 10 || MaxWarningsInMemory > 1000)
            throw new ArgumentException("MaxWarningsInMemory must be between 10 and 1000");
        
        if (MaxLogFileSizeMB < 1 || MaxLogFileSizeMB > 1000)
            throw new ArgumentException("MaxLogFileSizeMB must be between 1 and 1000");
        
        if (string.IsNullOrWhiteSpace(LogFilePath))
            throw new ArgumentException("LogFilePath must not be empty");
    }
}
```

---

## Entity Relationships

```text
HeartbeatConfig
      │
      │ (1:1 composition)
      ▼
HeartbeatMonitor
      │
      │ (creates)
      ▼
DriftEvent (0..*)
      │
      ├─► Log File (persistent)
      └─► ConcurrentQueue (in-memory cache, max 50)
              │
              └─► API Endpoint → Frontend UI
```

**Description**:
- One HeartbeatConfig instance per application lifetime (loaded from appsettings.json)
- One HeartbeatMonitor instance (singleton hosted service)
- Multiple DriftEvent instances created over time (when deviations detected)
- DriftEvents written to persistent log file (append-only, rotated when full)
- Recent DriftEvents cached in memory (bounded FIFO queue)
- Frontend retrieves cached DriftEvents via REST API

---

## Data Flow

### Detection Flow
1. HeartbeatMonitor pulses using PeriodicTimer (interval from HeartbeatConfig)
2. On each pulse, capture:
   - Monotonic elapsed time (Stopwatch)
   - Wall clock elapsed time (DateTime.UtcNow)
3. Evaluate conditions:
   - If MonoElapsedMs > ThresholdMs → Create DriftEvent with EventType="PERFORMANCE_DEGRADED"
   - If |MonoElapsedMs - WallElapsedMs| > 500 → Create DriftEvent with EventType="CLOCK_SHIFT"
4. For each DriftEvent created:
   - Write to log file via HeartbeatLogger
   - Add to in-memory ConcurrentQueue (evict oldest if >MaxWarningsInMemory)

### Display Flow
1. Frontend polls `GET /api/heartbeat/warnings` every 2 seconds
2. HeartbeatController reads DriftEvents from HeartbeatMonitor's ConcurrentQueue
3. Returns DriftEvents as JSON array (newest first)
4. Frontend renders warnings in HeartbeatWarnings component

---

## Storage Strategy

### Persistent Storage (Log File)
- **Format**: Plain text, one line per warning
- **Structure**: `[ISO8601 Timestamp] [EventType] Message (Mono: Xms, Wall: Yms)`
- **Example**: `2026-01-18T14:23:45.123Z [CLOCK_SHIFT] System clock changed externally. Mono: 1002ms vs Wall: 1523.4ms`
- **Rotation**: When file size exceeds MaxLogFileSizeMB, rename to `.old` and start new file
- **Retention**: Keep current + .old file (2 generations total)

### Transient Storage (In-Memory Cache)
- **Structure**: ConcurrentQueue<DriftEvent>
- **Capacity**: MaxWarningsInMemory (default 50)
- **Eviction**: FIFO - oldest warnings dequeued when capacity exceeded
- **Purpose**: Fast API reads for UI display
- **Lifetime**: Application lifetime (cleared on restart)

---

## TypeScript Interface (Frontend)

```typescript
interface DriftEvent {
  eventType: 'PERFORMANCE_DEGRADED' | 'CLOCK_SHIFT';
  timestamp: string; // ISO 8601 format
  monoElapsedMs: number;
  wallElapsedMs: number;
  expectedIntervalMs: number;
  deviationMs: number;
  message: string;
}

interface HeartbeatConfig {
  enabled: boolean;
  intervalMs: number;
  thresholdMs: number;
  maxWarningsInMemory: number;
  logFilePath: string;
  maxLogFileSizeMB: number;
}
```

---

## Validation Summary

| Entity | Required Validations | Optional Validations |
|--------|---------------------|---------------------|
| DriftEvent | EventType enum, All times > 0, Timestamp not future, Message not empty | - |
| HeartbeatConfig | IntervalMs [100-60000], ThresholdMs >= IntervalMs, MaxWarnings [10-1000], MaxLogSizeMB [1-1000], LogFilePath not empty | ThresholdMs recommended 200% of IntervalMs |

---

## Change Impact Analysis

**New Files**:
- `backend/src/Models/DriftEvent.cs`
- `backend/src/Models/HeartbeatConfig.cs`

**Modified Files**:
- `frontend/src/types.ts` - Add DriftEvent and HeartbeatConfig interfaces

**No Breaking Changes**: New entities only, no modifications to existing models.
