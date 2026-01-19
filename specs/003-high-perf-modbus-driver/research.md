# High-Performance Modbus Driver

## Overview

The High-Performance Modbus Driver is an implementation of the `IModbusDriver` interface that leverages NModbus's async capabilities to handle multiple address groups polling at different intervals on the same device connection.

## Key Features

### 1. **Asynchronous Operations**
- Uses `ReadHoldingRegistersAsync()` instead of synchronous blocking calls
- Allows multiple frames to poll independently without blocking each other
- Example: Frame A polling every 100ms won't block Frame B polling every 1000ms

### 2. **Single Connection Per Device**
- One TCP connection per unique IP:Port combination
- NModbus handles transaction ID sequencing internally
- Reduces network overhead and PLC connection slots

### 3. **Automatic Connection Management**
- Lazy connection creation on first request
- Automatic reconnection on failures
- Thread-safe connection pooling using `ConcurrentDictionary`

### 4. **Optimized for Low Latency**
- TCP NoDelay enabled (disables Nagle's algorithm)
- Optimized buffer sizes (8KB send/receive)
- 3-second read/write timeouts

## Architecture

### Strategy A: One Connection, Multiple Tasks

This implementation uses the recommended "Strategy A" approach:

```
Device (127.0.0.1:502)
  └── Single TCP Connection
       ├── Frame 1 (100ms polling) ──> Task 1
       ├── Frame 2 (1000ms polling) ──> Task 2
       └── Frame 3 (2000ms polling) ──> Task 3
```

Each frame runs in its own async task, but all share the same connection. NModbus queues requests internally and manages transaction IDs automatically.

## Configuration

### Enable High-Performance Driver

In `appsettings.json`:

```json
{
  "UseMockModbus": false,
  "UseHighPerformanceDriver": true
}
```

### Driver Selection Priority

1. If `UseMockModbus = true`: Uses MockModbusDriver (for testing)
2. Else if `UseHighPerformanceDriver = true`: Uses HighPerformanceModbusDriver
3. Else: Uses standard ModbusDriver (legacy)

## Performance Characteristics

### Advantages

✅ **Independent Polling**: Fast frames (100ms) don't wait for slow frames (2000ms)  
✅ **Lower Latency**: Async I/O and TCP optimizations reduce response time  
✅ **Resource Efficient**: Single connection per device reduces PLC load  
✅ **Automatic Queuing**: NModbus handles request sequencing internally  
✅ **Thread-Safe**: Multiple threads can call ReadHoldingRegistersAsync() safely  

### Technical Details

- **Transaction IDs**: Automatically managed by NModbus (no manual intervention needed)
- **Request Ordering**: If two requests arrive simultaneously, NModbus serializes them over TCP
- **Connection Pooling**: Keyed by `{ipAddress}:{port}` (slave ID is per-request)
- **Error Handling**: Failed connections are automatically removed and recreated

## Comparison: Standard vs High-Performance Driver

| Feature | Standard Driver | High-Performance Driver |
|---------|----------------|-------------------------|
| API | Synchronous (Task.Run wrapper) | Native async (ReadHoldingRegistersAsync) |
| Connection Model | One per device | One per device |
| Locking | SemaphoreSlim (global lock) | Lock-free (ConcurrentDictionary) |
| TCP Optimization | Default settings | NoDelay=true, optimized buffers |
| Multi-frame Support | Sequential (blocks) | Concurrent (non-blocking) |
| Recommended For | Legacy systems | Modern high-frequency polling |

## Example: Multi-Frame Polling

### Device Configuration

```json
{
  "devices": [
    {
      "name": "PLC-1",
      "ipAddress": "192.168.1.100",
      "port": 502,
      "slaveId": 1,
      "frames": [
        { "name": "FastFrame", "startAddress": 400001, "count": 10, "scanFrequencyMs": 100 },
        { "name": "SlowFrame", "startAddress": 400101, "count": 50, "scanFrequencyMs": 1000 }
      ]
    }
  ]
}
```

### Execution Flow

```
Time 0ms:   FastFrame polls (400001-400010)
Time 100ms: FastFrame polls
Time 200ms: FastFrame polls
Time 300ms: FastFrame polls
Time 400ms: FastFrame polls
Time 500ms: FastFrame polls
Time 600ms: FastFrame polls
Time 700ms: FastFrame polls
Time 800ms: FastFrame polls
Time 900ms: FastFrame polls
Time 1000ms: FastFrame polls + SlowFrame polls (400101-400150)
```

Both frames share the same TCP connection. If they request at the same instant, NModbus queues them.

## When to Use

### ✅ Use High-Performance Driver When:
- Polling multiple frames at different rates on the same device
- Need low latency (< 10ms overhead)
- Modern PLCs with good TCP/IP stacks
- High-frequency polling (< 500ms intervals)

### ⚠️ Use Standard Driver When:
- Legacy PLCs with unstable TCP stacks
- Very slow networks with high packet loss
- Simplicity is more important than performance

## Implementation Notes

### Thread Safety
The driver is fully thread-safe. Multiple tasks can call `ReadHoldingRegistersAsync()` concurrently.

### Connection Lifecycle
1. First request creates the connection
2. Connection is reused for all subsequent requests to the same IP:Port
3. On error, connection is disposed and recreated on next request

### Transaction ID Management
NModbus automatically assigns unique transaction IDs to each request and matches responses. You don't need to manage this.

## Future Enhancements

### Strategy B: Multiple Connections (Optional)

For PLCs that struggle with rapid sequential requests on one socket, we could implement Strategy B:

```csharp
// Each frame gets its own connection
var connectionKey = $"{ipAddress}:{port}:{slaveId}:{startAddress}";
```

This would use more PLC connection slots but guarantee complete isolation between frames.

### Connection Health Monitoring

Add periodic keep-alive checks:
```csharp
// Ping device every 30 seconds to detect dead connections
var healthCheck = await master.ReadHoldingRegistersAsync(1, 0, 1);
```

## References

- [NModbus GitHub](https://github.com/NModbus/NModbus)
- [Modbus TCP Specification](https://modbus.org/docs/Modbus_Messaging_Implementation_Guide_V1_0b.pdf)
- Original design discussion in user requirements

---

**Version**: 1.0  
**Author**: GitHub Copilot  
**Date**: January 19, 2026
