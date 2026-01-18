# Quickstart: Diagnostic Heartbeat Monitor

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)  
**For**: Developers and Operators

## What is the Heartbeat Monitor?

The diagnostic heartbeat monitor is a background service that continuously checks system health by detecting:

1. **Internal Latency** - Performance degradation from CPU spikes, garbage collection pauses, or thread starvation
2. **System Drift** - Clock synchronization issues from NTP adjustments, manual time changes, or hardware failures

It runs automatically in the background, logs warnings to files, and displays recent alerts in the system metrics dashboard.

---

## Quick Start (Default Configuration)

### 1. Enable the Monitor

The heartbeat monitor is **enabled by default**. No configuration changes needed to start.

### 2. View Warnings in UI

1. Start the application: `.\start.ps1`
2. Open the frontend: http://localhost:3000
3. Navigate to **System Metrics** area
4. View the **Heartbeat Warnings** panel (shows most recent warnings at the top)

### 3. Check Log Files

Warnings are also written to: `logs/heartbeat-warnings.log`

```powershell
# View recent warnings
Get-Content logs\heartbeat-warnings.log -Tail 20

# Watch warnings in real-time
Get-Content logs\heartbeat-warnings.log -Wait
```

---

## Configuration

Edit `backend/appsettings.json` to customize behavior:

```json
{
  "HeartbeatMonitor": {
    "Enabled": true,              // Enable/disable monitoring
    "IntervalMs": 1000,           // Check every 1000ms (1 second)
    "ThresholdMs": 2000,          // Warn if execution takes >2000ms
    "MaxWarningsInMemory": 50,    // Keep 50 most recent warnings for UI
    "LogFilePath": "logs/heartbeat-warnings.log",
    "MaxLogFileSizeMB": 10        // Rotate log at 10MB
  }
}
```

### Configuration Parameters

| Parameter | Default | Description | Valid Range |
|-----------|---------|-------------|-------------|
| `Enabled` | `true` | Enable/disable heartbeat monitoring | `true` or `false` |
| `IntervalMs` | `1000` | Milliseconds between health checks | 100 - 60000 |
| `ThresholdMs` | `2000` | Warning threshold (2x interval recommended) | Must be ≥ IntervalMs |
| `MaxWarningsInMemory` | `50` | Max warnings displayed in UI | 10 - 1000 |
| `LogFilePath` | `logs/heartbeat-warnings.log` | Warning log file location | Any writable path |
| `MaxLogFileSizeMB` | `10` | Log file size before rotation | 1 - 1000 |

**Important**: `ThresholdMs` must be greater than or equal to `IntervalMs`. Recommended: set threshold to 200% of interval.

### After Configuration Changes

Restart the backend to apply changes:

```powershell
.\stop.ps1
.\start-backend.ps1
```

---

## Understanding Warnings

### Warning Types

#### 1. PERFORMANCE_DEGRADED (Internal Latency)

**What it means**: The system took longer than expected to complete a heartbeat cycle.

**Possible causes**:
- High CPU usage (other processes consuming resources)
- Garbage collection pause (.NET GC running)
- Thread starvation (thread pool exhausted)
- Disk I/O blocking

**Example log entry**:
```
2026-01-18T14:23:45.123Z [PERFORMANCE_DEGRADED] Expected 1000ms, took 2345ms (Potential CPU/GC spike)
```

**What to do**:
- Check CPU usage in Task Manager
- Review other running processes
- Check if multiple SCADA scans running simultaneously
- Consider increasing system resources if persistent

#### 2. CLOCK_SHIFT (System Drift)

**What it means**: The system clock was adjusted while the application was running.

**Possible causes**:
- NTP (Network Time Protocol) synchronization
- Manual clock adjustment by administrator
- CMOS battery failure (rare)
- Virtual machine clock drift

**Example log entry**:
```
2026-01-18T14:24:10.456Z [CLOCK_SHIFT] System clock changed externally. Mono: 1002ms vs Wall: 1523.4ms
```

**What to do**:
- Check if NTP is configured and syncing
- Verify no manual time changes occurred
- For VMs: ensure VM tools are installed for proper clock sync
- If frequent: consider disabling aggressive NTP sync (adjust sync interval)

---

## Testing the Monitor

### Test 1: Simulate Internal Latency (CPU Spike)

**Backend API Test**: Force a delay to trigger PERFORMANCE_DEGRADED warning

```powershell
# Call a test endpoint that blocks for 3 seconds (requires implementation)
Invoke-RestMethod http://localhost:5000/api/heartbeat/test/cpu-spike -Method POST
```

**Expected result**: 
- Warning logged to file within 1-2 seconds
- Warning appears in UI within 2 seconds
- Log entry shows: `PERFORMANCE_DEGRADED Expected 1000ms, took ~3000ms`

### Test 2: Simulate Clock Shift

**Manual test**: Manually adjust system clock

```powershell
# Windows: Adjust clock forward by 10 seconds
# Use Settings > Time & Language > Date & time > Change
# Or PowerShell (requires admin):
Set-Date -Adjust (New-TimeSpan -Seconds 10)
```

**Expected result**:
- Warning logged immediately on next heartbeat cycle
- Log entry shows: `CLOCK_SHIFT Mono: ~1000ms vs Wall: ~10000ms`

**Note**: Restore correct time after test or wait for NTP to sync.

### Test 3: Verify Recovery

After triggering a warning:

1. Wait 5-10 seconds
2. Check that heartbeat continues normally (no new warnings)
3. Verify monitor automatically realigned

**Expected result**: No additional warnings if system returns to normal.

---

## Monitoring Best Practices

### Normal Operation

- **Expected warnings**: 0-1 per hour in healthy systems
- **CPU usage**: <0.5% (negligible)
- **Memory usage**: ~10MB
- **Log file growth**: Minimal (only when warnings occur)

### Alert Thresholds

| Scenario | Action |
|----------|--------|
| 1-2 warnings per hour | Monitor - may be occasional NTP sync or brief CPU spike |
| >5 warnings per hour | Investigate - check CPU usage, review system logs |
| Continuous PERFORMANCE_DEGRADED | Critical - system overloaded, review active scans, increase resources |
| Continuous CLOCK_SHIFT | Critical - clock instability, check NTP configuration or hardware |

### Troubleshooting

**Q: No warnings appearing in UI but log file has warnings**

A: Check frontend connectivity. Verify `GET /api/heartbeat/warnings` returns data:
```powershell
Invoke-RestMethod http://localhost:5000/api/heartbeat/warnings
```

**Q: Too many false positive warnings**

A: Increase `ThresholdMs` in configuration. Try 3x or 4x the interval for less sensitive monitoring.

**Q: Monitor disabled but warnings still appearing**

A: Restart backend after changing `Enabled: false` in appsettings.json.

**Q: Log file not rotating**

A: Check disk space and file permissions. Verify `MaxLogFileSizeMB` setting is appropriate.

---

## API Reference

### Get Recent Warnings

```http
GET /api/heartbeat/warnings
```

**Response** (200 OK):
```json
[
  {
    "eventType": "PERFORMANCE_DEGRADED",
    "timestamp": "2026-01-18T14:23:45.123Z",
    "monoElapsedMs": 2345,
    "wallElapsedMs": 2347.2,
    "expectedIntervalMs": 1000,
    "deviationMs": 1345,
    "message": "Expected 1000ms, took 2345ms (Potential CPU/GC spike)"
  }
]
```

**Response fields**:
- `eventType`: "PERFORMANCE_DEGRADED" or "CLOCK_SHIFT"
- `timestamp`: ISO 8601 UTC timestamp
- `monoElapsedMs`: Monotonic timer measurement (ms)
- `wallElapsedMs`: Wall clock measurement (ms)
- `expectedIntervalMs`: Configured interval
- `deviationMs`: Magnitude of deviation
- `message`: Human-readable description

### Get Configuration

```http
GET /api/heartbeat/config
```

**Response** (200 OK):
```json
{
  "enabled": true,
  "intervalMs": 1000,
  "thresholdMs": 2000,
  "maxWarningsInMemory": 50,
  "logFilePath": "logs/heartbeat-warnings.log",
  "maxLogFileSizeMB": 10
}
```

---

## Log File Format

Each warning is written as a single line:

```
[ISO8601 Timestamp] [EventType] Message (Mono: Xms, Wall: Yms)
```

**Example**:
```
2026-01-18T14:23:45.123Z [PERFORMANCE_DEGRADED] Expected 1000ms, took 2345ms (Potential CPU/GC spike)
2026-01-18T14:24:10.456Z [CLOCK_SHIFT] System clock changed externally. Mono: 1002ms vs Wall: 1523.4ms
```

**Log rotation**:
- When log file reaches `MaxLogFileSizeMB`, it's renamed to `.old`
- New log file starts fresh
- Only 2 generations kept (current + .old)

---

## Performance Impact

The heartbeat monitor is designed to be non-intrusive:

- **CPU**: <0.5% on typical server (negligible)
- **Memory**: ~10MB (50 warnings * 200 bytes each)
- **Disk I/O**: Append-only writes, only when warnings occur (infrequent)
- **Network**: Frontend polls every 2s, typical response <1KB

**No impact on SCADA performance** - monitor runs independently and does not interfere with device scanning or data collection.

---

## Next Steps

- Review [spec.md](spec.md) for complete functional requirements
- See [data-model.md](data-model.md) for entity definitions
- Check [contracts/api.yaml](contracts/api.yaml) for full API specification
- Consult [plan.md](plan.md) for implementation architecture

---

## Support

For issues or questions:
1. Check log file: `logs/heartbeat-warnings.log`
2. Verify configuration: `GET /api/heartbeat/config`
3. Review backend console output for startup messages
4. Check specification: [spec.md](spec.md)
