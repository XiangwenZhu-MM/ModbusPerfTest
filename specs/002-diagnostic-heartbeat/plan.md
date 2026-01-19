# Implementation Plan: Diagnostic Heartbeat Monitor

**Branch**: `002-diagnostic-heartbeat` | **Date**: January 18, 2026 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/002-diagnostic-heartbeat/spec.md`

## Summary

Implement a continuous background heartbeat monitor that detects two types of system health issues in SCADA environments:
1. **Internal Latency** - Performance degradation from CPU spikes, thread starvation, or GC pauses
2. **System Drift** - Clock synchronization issues from NTP changes, manual adjustments, or hardware failures

The monitor uses high-resolution monotonic timers to pulse at configurable intervals (default 1000ms), automatically detects deviations exceeding thresholds (default 2000ms), logs warnings to persistent files, and displays recent alerts in the system metrics UI. The system must be non-intrusive (<0.5% CPU, <10MB memory) and automatically recover after drift events.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: ASP.NET Core (10.0.0), React (19.2.3), TypeScript (4.9.5)  
**Storage**: File system logging (text files with rotation), in-memory warning cache for UI display  
**Testing**: xUnit for backend unit/integration tests, Jest/React Testing Library for frontend  
**Target Platform**: Windows/Linux server (ASP.NET Core backend), modern browsers (Chrome, Firefox, Safari)  
**Project Type**: Web application (existing backend + frontend structure)  
**Performance Goals**: <0.5% CPU usage, <10MB memory, timing accuracy within 5% of configured interval, warning detection within 1 heartbeat cycle  
**Constraints**: Non-intrusive monitoring (negligible overhead), automatic recovery from drift events, log rotation to prevent unbounded disk usage  
**Scale/Scope**: Single background service, 2 detection types, 2 API endpoints, 1 UI component, file-based logging with rotation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Specification-Driven Development
- Specification complete and approved: [spec.md](spec.md)
- User stories defined with acceptance criteria (3 prioritized stories)
- Measurable outcomes defined (8 success criteria)

### ✅ II. Test-First (Non-Negotiable)
- Test strategy identified: Unit tests for heartbeat timing/detection logic, integration tests for logging/API, UI tests for warning display
- Red-Green-Refactor cycle will be enforced for all components
- Automated tests required before merge

### ✅ III. Independent Deployability
- P1 (Automatic Monitoring): Can deploy and test heartbeat detection independently
- P2 (Warning Logging): Can add logging without UI display
- P3 (Real-Time UI): Can add UI display last without affecting core functionality
- Each priority level delivers standalone value

### ✅ IV. Observability & Transparency
- Heartbeat warnings logged to persistent files with timestamps
- Recent warnings displayed in system metrics UI
- All drift events (internal latency and system drift) are observable

### ✅ V. Simplicity & Minimalism
- Uses built-in .NET PeriodicTimer and Stopwatch (no external monitoring frameworks)
- File-based logging (no database complexity for this feature)
- Simple in-memory cache for recent warnings (bounded at 50 entries)
- Straightforward detection algorithm matches reference implementation

**Constitution Compliance**: ✅ PASS - All core principles satisfied, no violations to justify.

---

## Post-Phase 1 Re-Evaluation

*Re-checked after completing research.md, data-model.md, contracts/, and quickstart.md*

### ✅ I. Specification-Driven Development
- Design documents complete: research.md, data-model.md, contracts/api.yaml, quickstart.md
- All design decisions traceable to spec requirements
- No scope creep detected

### ✅ II. Test-First (Non-Negotiable)
- Unit test targets identified: HeartbeatMonitorTests.cs (timing accuracy, detection logic)
- Integration test targets identified: HeartbeatApiTests.cs (API endpoints)
- Frontend test targets identified: HeartbeatWarnings.test.tsx (component rendering)
- All tests can be written before implementation

### ✅ III. Independent Deployability
- Phase 1 design confirms independent deployment of each priority level
- No hidden dependencies introduced
- API contract allows frontend/backend development in parallel

### ✅ IV. Observability & Transparency
- API contracts defined for warning retrieval (GET /api/heartbeat/warnings)
- Log file format specified (ISO 8601 timestamps, structured messages)
- Configuration endpoint added (GET /api/heartbeat/config) for transparency

### ✅ V. Simplicity & Minimalism
- Research confirmed: no external dependencies needed beyond existing stack
- Data model contains only 2 entities (DriftEvent, HeartbeatConfig)
- API surface area: 2 endpoints only
- Frontend: 1 component (HeartbeatWarnings.tsx)

**Post-Design Constitution Compliance**: ✅ PASS - Design maintains adherence to all core principles. No violations introduced during planning phase.

## Project Structure

### Documentation (this feature)

```text
specs/002-diagnostic-heartbeat/
├── plan.md              # This file
├── research.md          # Phase 0: Technology decisions and patterns
├── data-model.md        # Phase 1: DriftEvent, HeartbeatConfig entities
├── quickstart.md        # Phase 1: How to configure and test the heartbeat monitor
├── contracts/           # Phase 1: API contracts for warning endpoints
│   └── api.yaml         # OpenAPI spec for GET /api/heartbeat/warnings
└── checklists/
    └── requirements.md  # Specification validation (already complete)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── Models/
│   │   ├── DriftEvent.cs           # NEW: Represents detected anomaly
│   │   └── HeartbeatConfig.cs      # NEW: Configuration (interval, threshold)
│   ├── Services/
│   │   ├── HeartbeatMonitor.cs     # NEW: Core monitoring service (PeriodicTimer + Stopwatch)
│   │   └── HeartbeatLogger.cs      # NEW: File logging with rotation
│   └── Api/
│       └── HeartbeatController.cs  # NEW: GET /api/heartbeat/warnings endpoint
├── appsettings.json                # MODIFY: Add heartbeat configuration section
└── Program.cs                      # MODIFY: Register HeartbeatMonitor as hosted service

frontend/
├── src/
│   ├── components/
│   │   └── HeartbeatWarnings.tsx   # NEW: Warning display component
│   ├── types.ts                    # MODIFY: Add DriftEvent interface
│   └── api.ts                      # MODIFY: Add fetchHeartbeatWarnings()
└── tests/
    └── HeartbeatWarnings.test.tsx  # NEW: Component tests

logs/                               # NEW: Directory for heartbeat warning logs
└── heartbeat-warnings.log          # Auto-created by HeartbeatLogger

tests/ (backend)                    # NEW: Test directory structure
├── Unit/
│   └── HeartbeatMonitorTests.cs    # Timing accuracy, detection logic
└── Integration/
    └── HeartbeatApiTests.cs        # API endpoint validation
```

**Structure Decision**: Web application structure (Option 2 from template). Heartbeat monitor runs as a background hosted service in the existing ASP.NET Core backend. File-based logging to `/logs` directory. Frontend displays warnings fetched from REST API. Follows existing pattern: services in `backend/src/Services/`, controllers in `backend/src/Api/`, React components in `frontend/src/components/`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations - constitution check passed. This table is intentionally empty.
