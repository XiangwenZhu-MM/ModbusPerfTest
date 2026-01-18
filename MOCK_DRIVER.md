# Mock Modbus Driver

## Overview

The Mock Modbus Driver allows you to test the SCADA Performance Monitor frontend and backend **without** running the test-simulator or connecting to real Modbus devices. It generates realistic mock data with configurable latency simulation.

## Features

- ✅ **No external dependencies** - No simulator or real devices needed
- ✅ **Realistic data** - Generates varying register values (0-65535)
- ✅ **Configurable latency** - Simulates network/device I/O delays
- ✅ **Stateful simulation** - Values change gradually over time (±0-10 per read)
- ✅ **Per-register tracking** - Each register maintains its own value
- ✅ **Easy configuration** - Simple JSON settings

## Configuration

### Enable Mock Driver

Edit `backend/appsettings.json` or `backend/appsettings.Development.json`:

```json
{
  "UseMockModbus": true,
  "MockModbus": {
    "MinLatencyMs": 20,
    "MaxLatencyMs": 120
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `UseMockModbus` | Enable mock driver (`true`) or use real NModbus (`false`) | `false` |
| `MockModbus:MinLatencyMs` | Minimum simulated I/O latency in milliseconds | `20` |
| `MockModbus:MaxLatencyMs` | Maximum simulated I/O latency in milliseconds | `120` |

## Usage

### Quick Test (Mock Mode - No Simulator Required)

1. **Edit configuration** to enable mock mode:
   ```json
   "UseMockModbus": true
   ```

2. **Start backend only**:
   ```powershell
   cd backend
   dotnet run
   ```
   
   You should see:
   ```
   Using MOCK Modbus driver (latency: 20-120ms)
   ```

3. **Start frontend**:
   ```powershell
   cd frontend
   npm start
   ```

4. **View dashboard** at http://localhost:3000

### Switch to Real Devices

1. **Edit configuration** to disable mock mode:
   ```json
   "UseMockModbus": false
   ```

2. **Start simulator** (or connect to real devices):
   ```powershell
   cd test-simulator
   dotnet run
   ```

3. **Start backend**:
   ```powershell
   cd backend
   dotnet run
   ```
   
   You should see:
   ```
   Using REAL Modbus driver (NModbus TCP)
   ```

## How It Works

### Mock Data Generation

- **Initial values**: Each register starts with a random value (0-1000)
- **Value changes**: On each read, values change by ±0-10 randomly
- **Clamping**: Values stay within valid range (0-65535)
- **Persistence**: Register values are maintained in memory across reads

### Latency Simulation

- Random delay between `MinLatencyMs` and `MaxLatencyMs` for each read
- Simulates realistic network + device processing time
- Allows testing of system performance under different latency conditions

### Example Data Flow

```
Read Request: Device 127.0.0.1:5020, Slave 1, Address 40001, Count 100
  ↓
Simulate latency: Random delay (e.g., 75ms)
  ↓
Generate 100 register values:
  - Address 40001: 523 → 527 (changed by +4)
  - Address 40002: 891 → 885 (changed by -6)
  - Address 40003: 412 → 419 (changed by +7)
  ... (97 more registers)
  ↓
Return ushort[] array
```

## Testing Scenarios

### Low Latency (Fast Network)
```json
"MinLatencyMs": 5,
"MaxLatencyMs": 20
```
Simulates local network or high-performance devices.

### High Latency (Slow Network)
```json
"MinLatencyMs": 100,
"MaxLatencyMs": 500
```
Simulates WAN connections or slow devices.

### Variable Latency (Unstable Network)
```json
"MinLatencyMs": 10,
"MaxLatencyMs": 300
```
Simulates unstable network with unpredictable delays.

## Advantages Over Simulator

| Aspect | Mock Driver | Simulator |
|--------|-------------|-----------|
| **Startup Time** | Instant | Requires separate process |
| **Resource Usage** | Minimal | Additional memory/CPU |
| **Configuration** | JSON setting | Manual device count/ports |
| **Network** | No TCP connections | Real TCP sockets |
| **Flexibility** | Easily adjust latency | Fixed simulation logic |
| **Debugging** | Simpler stack traces | Network layer complexity |

## Use Cases

✅ **Frontend Development** - Test UI without backend complexity  
✅ **Performance Testing** - Measure system behavior under various latencies  
✅ **CI/CD Pipelines** - Automated testing without external dependencies  
✅ **Demonstrations** - Quick setup for demos and presentations  
✅ **Development Offline** - Work without network access to devices  
✅ **Latency Analysis** - Study impact of I/O delays on metrics  

## Limitations

⚠ **Not a full Modbus simulator** - Does not simulate actual Modbus protocol  
⚠ **No network errors** - Always succeeds (no connection failures)  
⚠ **No protocol validation** - Skips Modbus frame/CRC checks  
⚠ **Simple value generation** - Random walk only, no realistic PLC logic  

For comprehensive testing including network failures and protocol validation, use the real test-simulator with NModbus.

## Example: Complete Mock Setup

```powershell
# 1. Configure mock mode
# Edit backend/appsettings.Development.json:
# "UseMockModbus": true

# 2. Start backend (NO simulator needed!)
cd backend
dotnet run
# Output: "Using MOCK Modbus driver (latency: 20-120ms)"

# 3. Start frontend
cd ../frontend
npm start

# 4. Open browser to http://localhost:3000
# Dashboard will show live metrics with mock data!
```

## Architecture

```
DeviceScanWorker
       ↓
   IModbusDriver ← Interface
       ↓
       ├─ ModbusDriver (Real) ← NModbus TCP
       └─ MockModbusDriver (Mock) ← In-memory simulation
```

The `IModbusDriver` interface allows seamless switching between real and mock implementations via dependency injection in `Program.cs`.
