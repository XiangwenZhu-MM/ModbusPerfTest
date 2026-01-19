# Implementation Plan: SCADA Performance Monitor MVP

**Branch**: `001-scada-performance-monitor` | **Date**: 2026-01-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-scada-performance-monitor/spec.md`

**Note**: This plan follows the project constitution and SpecKit workflow.

## Summary

Build a high-performance SCADA MVP that monitors Modbus TCP devices and calculates real-time performance metrics, including device-level (queue delay, round-trip time, total deviation) and system-level (task rates, saturation, dropped tasks) metrics, with dynamic dashboard display and robust data quality/staleness detection. Performance is the primary feature: the system must minimize latency, maximize throughput, and ensure timely, accurate metric reporting under load.
        
  To validate performance and scalability, a test application will be developed to simulate 200 Modbus TCP devices. This simulator will allow end-to-end load testing of the SCADA MVP under realistic conditions.

The system imports device configuration from a JSON file. Each device entry includes:
  - IP address
  - Port
  - Slave ID
  - One or more frames, each defining:
    - Modbus address range
    - Scan frequency (per frame)

For each frame, the system generates scan tasks at the configured frequency and enqueues them. The Modbus TCP driver reads scan tasks from the queue and performs Modbus reads as soon as possible. The backend uses a multi-threaded approach: each device is handled by a dedicated thread, but only one read task is executed per device at a time (no concurrent reads to the same device).

## Technical Context

**Language/Version**: C# 10 (.NET 6+, backend), TypeScript/React (frontend)
**Primary Dependencies**: 
  - Backend: ASP.NET Core WebAPI, NModbus, System.Threading, System.Collections.Concurrent, System.Text.Json
  - Frontend: React, recharts (or similar), WebSocket (for live updates)
**Storage**: In-memory (MVP phase, no persistent storage required)
**Testing**: No unit tests required for MVP phase
**Target Platform**: Windows/Linux server (backend), modern browsers (frontend)
**Project Type**: Web (REST API + WebSocket + SPA dashboard)
**Performance Goals**: Performance is the top priority. The system must:
  - Minimize end-to-end latency for all device and system metrics
  - Maximize throughput of scan tasks and Modbus reads
  - Guarantee <1s device metric update, <2s failure notification, 60s rolling health metrics
  - Maintain 99%+ success rate under normal and high load
  - Avoid bottlenecks in task queueing, Modbus driver, and dashboard updates
**Constraints**: No persistent storage, trusted network, multi-device support, one thread per device (Task/Thread per device), only one read per device at a time (lock/async semaphore)
**Scale/Scope**: MVP: multi-device, 1 dashboard, 1 operator; extensible to multi-device, multi-user

**Test Application**: A Modbus TCP device simulator will be implemented (in C# or Python) to simulate 200 concurrent Modbus TCP devices, each responding to requests as a real device would. This will be used for load and performance testing of the main system.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- All features begin with a written, testable specification (✅)
- Test-first: Automated tests required before implementation (✅)
- Each user story is independently testable and deployable (✅)
- All metrics and data quality states are observable via dashboard (✅)
- Simplicity: MVP avoids unnecessary complexity (✅)

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
