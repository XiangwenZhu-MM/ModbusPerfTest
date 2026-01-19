# Quick Start Guide

## Running the SCADA Performance Monitor MVP

### Prerequisites

Ensure `device-config.json` exists in the project root directory. Both the backend and simulator will automatically load configuration from this file on startup.

### 1. Start the Modbus TCP Device Simulator

```powershell
cd test-simulator
dotnet run
```

The simulator will:
- Automatically read `device-config.json` from the project root
- Create simulated devices matching the configuration (ports, slave IDs)
- Start listening for Modbus TCP connections

**Note**: The simulator creates devices based on the exact configuration, so ensure `device-config.json` is properly configured before starting.

### 2. Start the Backend

```powershell
cd backend
dotnet run
```

The backend will:
- Automatically load `device-config.json` from the project root
- Start monitoring all configured devices immediately
- Begin collecting metrics

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

**Note**: No manual configuration upload needed - monitoring starts automatically!

### 3. View Metrics

#### Device-Level Metrics
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/Metrics/device?count=10"
```

#### System Health Metrics
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/Metrics/system"
```

#### Queue Statistics
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/Metrics/queue"
```

### 6. Start the Frontend (Dashboard)

```powershell
cd frontend
npm start
```

The dashboard will be available at `http://localhost:3000`

## Testing with Multiple Devices

For load testing with 200 devices:

1. Start simulator with 200 devices
2. Generate a config file with 200 device entries (script TODO)
3. Upload the configuration
4. Monitor system health metrics to observe saturation

## Expected Metrics

- **QueueDurationMs**: Time task spent in queue (should be < 100ms under normal load)
- **DeviceResponseTimeMs**: Network round-trip time (typically 1-50ms on localhost)
- **ActualSamplingIntervalMs**: Total time from task creation to completion
- **IngressTPM**: Tasks created per minute
- **EgressTPM**: Tasks completed per minute  
- **SaturationIndex**: Ingress/Egress ratio (>100% indicates overload)
- **DroppedTPM**: Tasks dropped due to queue overflow

## Troubleshooting

- **Connection refused**: Ensure simulator is running on the correct port
- **High queue duration**: Increase scan frequency or reduce device count
- **Dropped tasks**: System is saturated, reduce workload or optimize
- **No metrics**: Verify configuration was uploaded successfully
