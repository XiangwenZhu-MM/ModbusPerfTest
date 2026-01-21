# NModbusAsync Library Fix - Summary

## Problem Identified

The project was configured to use `ModbusLibrary: "NModbusAsync"` but was referencing the **wrong library**:

- **Incorrect**: `NModbus4.NetCore` v4.0.0 (from the .NET Core port of NModbus)
- **Correct**: `NModbusAsync` v3.0.2 (from https://github.com/wolf8196/NModbusAsync)

This caused issues with `AllowConcurrentFrameReads=true` because:
1. The wrong library doesn't provide true async/await support
2. Concurrent calls to the same Modbus master instance caused race conditions
3. High queue durations occurred due to blocking operations

## Changes Made

### 1. Updated NuGet Package Reference
**File**: `backend/ModbusPerfTest.Backend.csproj`

```xml
<!-- BEFORE -->
<PackageReference Include="NModbus4.NetCore" Version="4.0.0" />

<!-- AFTER -->
<PackageReference Include="NModbusAsync" Version="3.0.2" />
```

### 2. Updated NModbusAsyncDriver Implementation
**File**: `backend/src/Services/NModbusAsyncDriver.cs`

**Changes**:
- Updated using statement from `Modbus.Device` to `NModbus`
- Replaced `ModbusIpMaster.CreateIp()` with `ModbusFactory().CreateMaster()`
- Added connection-level locking with `SemaphoreSlim` to prevent concurrent read issues
- Updated documentation comments

**Key improvements**:
```csharp
// Added lock to ConnectionContext
public SemaphoreSlim Lock { get; } = new(1, 1);

// Wrapped all Modbus read operations with lock
await context.Lock.WaitAsync(cancellationToken);
try
{
    var data = await context.Master.ReadHoldingRegistersAsync(slaveId, startAddress, count);
    return data;
}
finally
{
    context.Lock.Release();
}
```

## Impact on AllowConcurrentFrameReads

### With the Fix:
✅ **Thread-safe**: Multiple frames can be processed by different workers without race conditions  
✅ **Correct library**: Using NModbusAsync (wolf8196) with true async support  
✅ **Serialized per connection**: Each device connection is protected by a lock  
✅ **Lower queue duration**: No more blocking issues from concurrent access  

### Behavior:
- When `AllowConcurrentFrameReads=true`:
  - Multiple worker threads are created (one per frame)
  - Each worker can process tasks independently
  - Access to the same device connection is serialized by the lock
  - Different devices can still be accessed concurrently

- When `AllowConcurrentFrameReads=false`:
  - Single worker thread per device (original behavior)
  - Sequential processing through device-level lock in DeviceScanWorker

## Testing Recommendations

1. **Restart the backend** with current configuration:
   ```json
   {
     "UseHighPerformanceDriver": true,
     "AllowConcurrentFrameReads": true,
     "ModbusLibrary": "NModbusAsync"
   }
   ```

2. **Monitor metrics**:
   - Queue duration should decrease significantly
   - No race conditions or corrupted data
   - Thread pool metrics should be stable

3. **Verify in UI**:
   - Check System Health Metrics → Queue Statistics
   - Check ThreadPool Health Monitor
   - Ensure dropped tasks remain at 0 or minimal

## Build Status

✅ **Build succeeded** with the new NModbusAsync library  
✅ **All namespaces updated** correctly  
✅ **Thread safety implemented** with connection-level locking  

## Next Steps

1. Test with your existing device configuration
2. Compare queue duration metrics before/after
3. If issues persist, consider testing with `AllowConcurrentFrameReads=false` to isolate the problem
