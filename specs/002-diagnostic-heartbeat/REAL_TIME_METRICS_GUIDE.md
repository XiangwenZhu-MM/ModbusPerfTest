# Real-Time Metrics Display - Quick Reference

## What You'll See

The System Health Metrics panel now displays **two sections**:

### 1. Real-Time System Metrics (NEW) ✨
Updates every **1 second** with current measurements:

```
┌─────────────────────────────────────────────────────┐
│ Real-Time System Metrics                            │
├─────────────────────────────────────────────────────┤
│ [✓ Healthy] Last Check: 3:45:12 PM                 │
├──────────────────────┬──────────────────────────────┤
│  System Latency      │  Clock Drift                 │
│      0 ms            │    15.2 ms                   │
│  Expected: 1000ms    │  Mono: 1001ms                │
│  Actual: 1001ms      │  Wall: 1016ms                │
└──────────────────────┴──────────────────────────────┘
```

**Color Indicators**:
- 🟢 **Green** (Healthy): Latency = 0ms AND Clock Drift < 500ms
- 🟠 **Orange** (Warning - Latency): Latency > 0ms only
- 🔴 **Red** (Warning - Drift): Clock Drift > 500ms only  
- 🟣 **Pink** (Critical): Both latency AND drift detected

### 2. Heartbeat Warnings (Existing)
Updates every **2 seconds** with threshold violations only:

```
┌─────────────────────────────────────────────────────┐
│ Heartbeat Warnings                                  │
├─────────────────────────────────────────────────────┤
│ [⚠ Internal Latency] 1/18/2026, 3:45:10 PM         │
│ Expected 1000ms, took 2150ms (Potential CPU/GC)    │
│ Deviation: 1150.0ms | Expected: 1000ms | Actual: 2150ms
└─────────────────────────────────────────────────────┘
```

## Key Differences

| Feature | Real-Time Metrics | Warnings |
|---------|------------------|----------|
| **Purpose** | Shows current values | Shows violations only |
| **Update Rate** | Every 1 second | Every 2 seconds |
| **Display** | Always visible | Only when thresholds exceeded |
| **Threshold** | None (shows all values) | Latency > 2000ms OR Drift > 500ms |
| **Use Case** | Live monitoring | Historical alerts |

## What Each Metric Means

### System Latency
- **What it measures**: How much longer than expected the heartbeat took
- **Calculation**: `max(0, ActualTime - ExpectedTime)`
- **Healthy value**: `0 ms` (on time or faster)
- **Example**: If heartbeat expected every 1000ms but took 1050ms → **Latency = 50ms**

### Clock Drift
- **What it measures**: Divergence between monotonic timer and system clock
- **Calculation**: `|MonotonicTime - WallClockTime|`
- **Healthy value**: `< 500 ms`
- **Causes**: NTP sync, manual clock change, VM time drift
- **Example**: Monotonic says 1001ms elapsed, Wall clock says 1520ms elapsed → **Drift = 519ms**

## When to Be Concerned

### System Latency > 0ms
- **Mild (1-500ms)**: Normal occasional delays (GC, context switching)
- **Moderate (500-2000ms)**: Possible CPU contention, check other processes
- **Severe (>2000ms)**: Triggers warning, investigate immediately
  - Check: CPU usage, memory pressure, disk I/O

### Clock Drift > 500ms
- **Always investigate** when this occurs
- **Triggers warning** immediately (no threshold, 500ms is the limit)
- **Common causes**:
  - NTP synchronization in progress
  - Virtual machine host time sync
  - Manual system clock adjustment
  - Hardware clock issues (rare)

## API Endpoints

```bash
# Get current real-time metrics (always available)
GET http://localhost:5000/api/heartbeat/metrics

# Response:
{
  "lastMonoElapsedMs": 1001,
  "lastWallElapsedMs": 1016.5,
  "lastCheckedAt": "2026-01-18T15:45:12Z",
  "expectedIntervalMs": 1000,
  "latencyMs": 1,
  "clockDriftMs": 15.5,
  "isHealthy": true
}

# Get warnings (only shows threshold violations)
GET http://localhost:5000/api/heartbeat/warnings

# Response (empty when healthy):
[]
```

## Testing

### Simulate System Latency
1. Run CPU stress test or start heavy workload
2. Watch "System Latency" metric increase from 0ms
3. If exceeds 2000ms, warning appears in "Heartbeat Warnings" section

### Simulate Clock Drift
1. Manually adjust system clock forward/backward by 1+ seconds
2. Watch "Clock Drift" metric jump to >500ms
3. Warning immediately appears in "Heartbeat Warnings" section
4. After NTP resync, drift returns to normal (<500ms)

## Configuration

No additional configuration needed. Real-time metrics use the same heartbeat settings:

```json
{
  "HeartbeatMonitor": {
    "Enabled": true,
    "IntervalMs": 1000,  // How often to pulse (Real-Time Metrics shows each pulse)
    "ThresholdMs": 2000  // When to create warning (Real-Time Metrics shows all values)
  }
}
```

## Troubleshooting

**Problem**: Metrics not updating  
**Solution**: Check that HeartbeatMonitor.Enabled = true in appsettings.json

**Problem**: Metrics show "Loading..." forever  
**Solution**: Verify backend is running and CORS allows frontend origin

**Problem**: Healthy but warnings still show  
**Solution**: Warnings show recent historical violations (last 50). They don't disappear when system recovers - this is by design for diagnostics.

**Problem**: Clock drift always shows ~15-30ms  
**Solution**: This is normal system jitter between Stopwatch and DateTime. Only >500ms is concerning.
