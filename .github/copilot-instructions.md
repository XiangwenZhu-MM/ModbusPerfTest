
# ModbusPerfTest AI Agent Instructions

## Project Overview

- **Purpose**: High-performance SCADA system for real-time monitoring of Modbus TCP devices, with device/system metrics, data quality detection, and a live dashboard.
- **Architecture**: 
	- **Backend**: ASP.NET Core WebAPI (.NET 7.0), NModbus, in-memory storage, REST + WebSocket APIs
	- **Frontend**: React (TypeScript), real-time dashboard, API/WebSocket clients
	- **Simulator**: C# console app simulating up to 200 Modbus TCP devices
	- **Mock Driver**: In-memory Modbus driver for backend-only testing (no simulator needed)

## Directory Structure

- `backend/src/Api/` – Controllers (REST/WebSocket endpoints)
- `backend/src/Services/` – Business logic, Modbus drivers, metrics, heartbeat
- `backend/src/Models/` – Data models (DeviceConfig, Metrics, DriftEvent, etc.)
- `frontend/src/components/` – React UI panels (DeviceMetricsPanel, SystemHealthPanel, DataQualityPanel, etc.)
- `frontend/src/types.ts` – TypeScript interfaces for all API data
- `test-simulator/` – Modbus TCP device simulator
- `specs/` – Feature specs, plans, and task lists

## Core Patterns & Conventions

- **Specification-Driven**: All features start with `/specs/[feature]/spec.md` and a plan. No code without a spec.
- **Test-First**: Automated tests (backend: xUnit, frontend: Jest/RTL) are written before implementation for new features (see constitution).
- **Independent Deployability**: Each user story/feature is testable and deployable on its own.
- **Observability**: All metrics, health, and data quality states are exposed via API and/or logs, and visualized in the dashboard.
- **Minimalism**: Avoid unnecessary complexity; prefer in-memory, stateless, and direct solutions unless justified by spec.

## Developer Workflows

- **Quick Start (Windows)**: Run `start.ps1` to launch backend and frontend. Use `stop.ps1` to stop all.
- **Manual Start**:
	- Backend: `cd backend && dotnet restore && dotnet run`
	- Frontend: `cd frontend && npm install && npm start`
	- Simulator: `cd test-simulator && dotnet run` (optional; not needed if using mock driver)
- **Configuration**: Edit `device-config.json` (root) for device/frame setup. Backend and simulator auto-load this file.
- **Mock Driver**: Enable by setting `"UseMockModbus": true` in `backend/appsettings.json`. No simulator needed; backend generates mock data.
- **API Endpoints**: See `frontend/src/api.ts` for all REST calls. Main endpoints: `/api/Metrics/device`, `/api/Metrics/system`, `/api/Metrics/queue`, `/api/ClockDrift/statistics`, `/api/DataQuality/summary`, `/api/Heartbeat/warnings`.
- **WebSocket**: Used for real-time metric updates (see backend `MetricWebSocket.cs`).
- **Logs**: Heartbeat warnings and system events are logged to `/logs/`.

## Key Integration Points

- **Modbus Driver Abstraction**: `IModbusDriver` interface allows switching between real (NModbus) and mock drivers via DI in `Program.cs`.
- **Metrics Collection**: `MetricCollector` (backend) aggregates device/system metrics; exposed via API and WebSocket.
- **Heartbeat Monitoring**: `HeartbeatMonitor` runs as a background service, logs drift/latency events, exposes warnings via API.
- **Frontend Data Flow**: All panels fetch from API endpoints in `api.ts` and use types from `types.ts`.

## Project-Specific Notes

- **No persistent storage**: All state is in-memory for MVP.
- **Performance goals**: <1s device metric update, <2s failure notification, 99%+ success rate under load.
- **No sensitive data**: Security is MVP-scoped; trusted network assumed.
- **Specs are source of truth**: For any new feature, consult `/specs/[feature]/` for requirements and design.

## Example: Add a New Metric

1. Update `/specs/[feature]/spec.md` and `/plan.md` with requirements.
2. Add model in `backend/src/Models/` and update `MetricCollector`.
3. Expose via new or existing API controller in `backend/src/Api/`.
4. Add TypeScript type in `frontend/src/types.ts` and API call in `api.ts`.
5. Display in a new or existing React component in `frontend/src/components/`.
6. Add tests if required by spec.

---
For more, see: `README.md`, `QUICKSTART.md`, `MOCK_DRIVER.md`, and `/specs/` for feature-specific docs.
