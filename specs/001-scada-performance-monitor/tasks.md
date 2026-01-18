---
description: "Task list for SCADA Performance Monitor MVP implementation"
---

# Tasks: SCADA Performance Monitor MVP

**Input**: Design documents from `/specs/001-scada-performance-monitor/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

## Phase 1: Setup
- [X] T001 Create backend and frontend project structure (backend/ and frontend/)
- [X] T002 [P] Initialize ASP.NET Core WebAPI project in backend/
- [X] T003 [P] Initialize React project in frontend/
- [X] T004 Add NModbus and required dependencies to backend project
- [X] T005 Add basic README and .gitignore files

## Phase 2: Foundational
- [X] T006 Implement JSON configuration loader for device and frame definitions in backend/src/services/DeviceConfigService.cs
- [X] T007 Implement in-memory task queue for scan tasks in backend/src/services/ScanTaskQueue.cs
- [X] T008 Implement Modbus TCP driver wrapper using NModbus in backend/src/services/ModbusDriver.cs
- [X] T009 Implement thread management for per-device scan workers in backend/src/services/DeviceScanManager.cs
- [X] T010 Implement REST API endpoint for uploading/importing device configuration in backend/src/api/DeviceConfigController.cs
- [X] T011 Implement WebSocket server for real-time metric updates in backend/src/api/MetricWebSocket.cs

## Phase 3: User Story 1 - Real-Time Network Performance Monitoring (P1)
- [X] T012 [P] [US1] Implement scan task generator per frame (frequency, address range) in backend/src/services/ScanTaskGenerator.cs
- [X] T013 [P] [US1] Implement per-device scan worker (thread/task) with single read lock in backend/src/services/DeviceScanWorker.cs
- [X] T014 [US1] Collect and calculate device-level metrics (queue delay, round-trip time, total deviation) in backend/src/services/MetricCollector.cs
- [X] T015 [US1] Expose real-time device-level metrics via WebSocket in backend/src/api/MetricWebSocket.cs
- [ ] T016 [US1] Display real-time device-level metrics on dashboard in frontend/src/components/DeviceMetricsPanel.tsx

## Phase 4: User Story 2 - System Clock Drift Detection (P2)
- [X] T017 [P] [US2] Implement system clock drift calculation in backend/src/services/ClockDriftService.cs
- [X] T018 [US2] Expose clock drift metrics via WebSocket in backend/src/api/MetricWebSocket.cs
- [X] T019 [US2] Display clock drift metrics on dashboard in frontend/src/components/SystemHealthPanel.tsx

## Phase 5: User Story 3 - Visual Performance Dashboard (P3)
- [X] T020 [P] [US3] Implement system-level health metrics calculation (task rates, saturation, dropped tasks) in backend/src/services/SystemHealthService.cs
- [X] T021 [US3] Expose system-level metrics via WebSocket in backend/src/api/MetricWebSocket.cs
- [X] T022 [US3] Display system-level metrics and trends on dashboard in frontend/src/components/SystemHealthPanel.tsx
- [X] T023 [US3] Implement real-time chart updates for all metrics in frontend/src/components/PerformanceCharts.tsx

## Phase 6: User Story 4 - Data Quality and Staleness Detection (P4)
- [X] T024 [P] [US4] Implement staleness detection logic per data point in backend/src/services/DataQualityService.cs
- [X] T025 [US4] Update data quality state and last known value/timestamp in backend/src/services/DataQualityService.cs
- [X] T026 [US4] Expose data quality states via WebSocket in backend/src/api/MetricWebSocket.cs
- [X] T027 [US4] Display stale/uncertain data visually on dashboard in frontend/src/components/DataQualityPanel.tsx

## Phase 7: Test Application - Modbus TCP Device Simulator
- [X] T028 Create C# console app to simulate 200 Modbus TCP devices in test-simulator/DeviceSimulator.cs
- [X] T029 [P] Implement Modbus TCP server logic for each simulated device in test-simulator/DeviceSimulator.cs
- [X] T030 [P] Support configuration of device count, address ranges, and response delays in test-simulator/DeviceSimulator.cs
- [X] T031 [P] Provide CLI to start/stop simulation and monitor active connections in test-simulator/DeviceSimulator.cs

## Final Phase: Polish & Cross-Cutting
- [X] T032 Add error handling and logging throughout backend/src/
- [X] T033 Add connection failure and queue overflow notifications in backend/src/services/
- [X] T034 Add configuration and build scripts for backend and frontend
- [X] T035 Update documentation for deployment and usage in README.md

## Dependencies
- Phase 1 and 2 must be completed before any user story phases
- User stories (Phases 3-6) can be developed in parallel after foundational work
- Test application (Phase 7) can be developed in parallel with backend
- Polish phase is last

## Parallel Execution Examples
- T002, T003, T004, T005 can run in parallel
- T012, T013, T017, T020, T024, T028 can run in parallel after foundational setup
- T029, T030, T031 can run in parallel

## Implementation Strategy
- MVP-first: Deliver core backend, frontend, and simulator for 1 device, then scale to 200 devices
- Incremental: Each user story is independently testable and deployable
- No unit tests required for MVP
