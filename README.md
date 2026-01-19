# SCADA Performance Monitor MVP

A high-performance SCADA system that monitors Modbus TCP devices and calculates real-time performance metrics.

## Features

- **Device-Level Metrics**: Queue delay, round-trip time, total deviation
- **System-Level Health**: Task rates, saturation ratio, dropped tasks
- **Data Quality Detection**: Automatic staleness detection with last known value retention
- **Real-Time Dashboard**: Dynamic web interface with live metric updates
- **Multi-Device Support**: Configure and monitor multiple Modbus TCP devices via JSON
- **Performance-Focused**: Optimized for minimal latency and maximum throughput
- **High-Performance Driver**: Async Modbus driver supporting multiple polling intervals per device

## Architecture

- **Backend**: ASP.NET Core WebAPI (.NET 10.0) with NModbus
- **Frontend**: React with TypeScript
- **Communication**: WebSocket for real-time updates
- **Concurrency**: High-performance async driver with one connection per device
- **Multi-Frame Polling**: Independent polling loops for each frame at different intervals

## Project Structure

```
ModbusPerfTest/
├── backend/              # ASP.NET Core WebAPI
│   └── src/
│       ├── api/         # Controllers and WebSocket handlers
│       ├── services/    # Business logic and Modbus driver
│       └── models/      # Data models
├── frontend/            # React TypeScript SPA
│   └── src/
│       ├── components/  # UI components
│       └── services/    # API and WebSocket clients
└── specs/               # Feature specifications and documentation
```

## Getting Started

### Quick Start (One-Click)

**Windows Users**: Simply double-click **`start.ps1`** to launch both applications.

Or run from PowerShell:
```powershell
.\start.ps1
```

To stop all applications:
```powershell
.\stop.ps1
```

---

### Manual Setup

### Prerequisites

- .NET 10.0 SDK
- Node.js 18+ and npm
- Modern web browser

### Backend Setup

```bash
cd backend
dotnet restore
dotnet run
```

### Frontend Setup

```bash
cd frontend
npm install
npm start
```

### Modbus Device Simulator

**Note**: Use any third-party Modbus TCP simulator (e.g., ModRSsim2, Modbus Slave) configured to match the devices in `device-config.json`.

## Configuration

### Modbus Driver Selection

In `appsettings.json`, configure which driver to use:

```json
{
  "UseHighPerformanceDriver": true   // Use async high-performance driver (recommended)
}
```

See [specs/003-high-perf-modbus-driver/IMPLEMENTATION_SUMMARY.md](specs/003-high-perf-modbus-driver/IMPLEMENTATION_SUMMARY.md) for detailed driver documentation.

### Device Configuration

Device configuration is loaded from `device-config.json`:

```json
{
  "devices": [
    {
      "name": "PLC-1",
      "ipAddress": "192.168.1.100",
      "port": 502,
      "slaveId": 1,
      "frames": [
        {
          "name": "FastFrame",
          "startAddress": 400001,
          "count": 50,
          "scanFrequencyMs": 100
        },
        {
          "name": "SlowFrame",
          "startAddress": 400051,
          "count": 50,
          "scanFrequencyMs": 1000
        }
      ]
    }
  ]
}
```

### Import from XML

Use the provided PowerShell script to convert existing device configurations:

```powershell
.\convert-device-config.ps1
```

This reads `device-config.xml` and generates `device-config.json`.

## Performance Goals

- Device metric updates: <1 second
- Failure notifications: <2 seconds
- System health metrics: 60-second rolling window
- Success rate: 99%+ under normal load

## License

MIT
