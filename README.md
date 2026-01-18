# SCADA Performance Monitor MVP

A high-performance SCADA system that monitors Modbus TCP devices and calculates real-time performance metrics.

## Features

- **Device-Level Metrics**: Queue delay, round-trip time, total deviation
- **System-Level Health**: Task rates, saturation ratio, dropped tasks
- **Data Quality Detection**: Automatic staleness detection with last known value retention
- **Real-Time Dashboard**: Dynamic web interface with live metric updates
- **Multi-Device Support**: Configure and monitor multiple Modbus TCP devices via JSON
- **Performance-Focused**: Optimized for minimal latency and maximum throughput

## Architecture

- **Backend**: ASP.NET Core WebAPI (.NET 7.0) with NModbus
- **Frontend**: React with TypeScript
- **Communication**: WebSocket for real-time updates
- **Concurrency**: One thread per device, single read per device at a time

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
├── test-simulator/      # Modbus TCP device simulator (200 devices)
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

- .NET 7.0 SDK
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

### Device Simulator (Optional - Use Mock Driver Instead)

```bash
cd test-simulator
dotnet run
```

**Note**: The backend includes a mock Modbus driver (enabled by default in `appsettings.json`). No simulator needed for testing!

## Configuration

Device configuration is loaded from JSON with the following structure:

```json
{
  "devices": [
    {
      "ipAddress": "192.168.1.100",
      "port": 502,
      "slaveId": 1,
      "frames": [
        {
          "startAddress": 0,
          "count": 10,
          "scanFrequencyMs": 1000
        }
      ]
    }
  ]
}
```

## Performance Goals

- Device metric updates: <1 second
- Failure notifications: <2 seconds
- System health metrics: 60-second rolling window
- Success rate: 99%+ under normal load

## License

MIT
