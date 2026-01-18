---
description: "Task list for Diagnostic Heartbeat Monitor implementation"
---

# Tasks: Diagnostic Heartbeat Monitor

**Input**: Design documents from `/specs/002-diagnostic-heartbeat/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/api.yaml](contracts/api.yaml)

**Tests**: TDD approach requested in specimen. Automated tests for backend (xUnit) and frontend (Jest) are included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- File paths: `backend/src/`, `frontend/src/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create `backend/src/Models/` directory for new data entities
- [ ] T002 Create `backend/src/Api/` and `backend/src/Services/` directories if not present
- [ ] T003 Create `logs/` directory at repository root for heartbeat warning logs
- [ ] T004 [P] Add heartbeat configuration section to `backend/appsettings.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core model and configuration infrastructure required for all detection logic

- [ ] T005 [P] Implement `HeartbeatConfig` model in `backend/src/Models/HeartbeatConfig.cs`
- [ ] T006 [P] Implement `DriftEvent` model in `backend/src/Models/DriftEvent.cs`
- [ ] T007 Add `DriftEvent` and `HeartbeatConfig` interfaces to `frontend/src/types.ts`
- [ ] T008 [P] Configure dependency injection for `HeartbeatConfig` in `backend/Program.cs`

**Checkpoint**: Foundation ready - core models and configuration available for service implementation.

---

## Phase 3: User Story 1 - Automatic System Health Monitoring (Priority: P1) 🎯 MVP

**Goal**: Background service that pulses at intervals and detects internal latency/clock shift.

**Independent Test**: Verify that the background service pulses, detects latency spikes (simulated), and detects clock shifts (manual change).

### Tests for User Story 1

- [ ] T009 [P] [US1] Create unit tests for detection logic in `backend/tests/Unit/HeartbeatMonitorTests.cs` (timing accuracy, drift detection)
- [ ] T010 [P] [US1] Create integration test for background service pulsing in `backend/tests/Integration/HeartbeatServiceTests.cs`

### Implementation for User Story 1

- [ ] T011 [US1] Implement `HeartbeatMonitor` background service base in `backend/src/Services/HeartbeatMonitor.cs` (PeriodicTimer loop)
- [ ] T012 [US1] Implement internal latency detection logic in `backend/src/Services/HeartbeatMonitor.cs` (Stopwatch comparison)
- [ ] T013 [US1] Implement system clock shift detection logic in `backend/src/Services/HeartbeatMonitor.cs` (Wall clock comparison)
- [ ] T014 [US1] Implement schedule realignment logic to ensure recovery after drift events in `backend/src/Services/HeartbeatMonitor.cs`
- [ ] T015 [US1] Register `HeartbeatMonitor` as a hosted service in `backend/Program.cs`

**Checkpoint**: User Story 1 functional - health issues detected in memory in the background.

---

## Phase 4: User Story 2 - Warning Notification and Logging (Priority: P2)

**Goal**: Persist warnings to log files and provide an API for UI retrieval.

**Independent Test**: Verify warnings are written to `logs/heartbeat-warnings.log` and accessible via `GET /api/heartbeat/warnings`.

### Tests for User Story 2

- [ ] T016 [P] [US2] Create unit tests for logging and rotation in `backend/tests/Unit/HeartbeatLoggerTests.cs`
- [ ] T017 [P] [US2] Create contract tests for heartbeat API in `backend/tests/Integration/HeartbeatApiTests.cs`

### Implementation for User Story 2

- [ ] T018 [US2] Implement `HeartbeatLogger` service with size-based rotation in `backend/src/Services/HeartbeatLogger.cs`
- [ ] T019 [US2] Integrate `HeartbeatLogger` into `HeartbeatMonitor` to persist detected events
- [ ] T020 [US2] Implement in-memory bounded cache (ConcurrentQueue) for recent warnings in `backend/src/Services/HeartbeatMonitor.cs`
- [ ] T021 [US2] Implement `HeartbeatController` with `GET /api/heartbeat/warnings` and `GET /api/heartbeat/config` in `backend/src/Api/HeartbeatController.cs`

**Checkpoint**: User Story 2 functional - historical warnings preserved in file and available via API.

---

## Phase 5: User Story 3 - Real-Time Warning Display (Priority: P3)

**Goal**: UI component that displays recent warnings in the dashboard.

**Independent Test**: Verify warnings appear in the dashboard UI within 2 seconds of detection, newest first.

### Tests for User Story 3

- [ ] T022 [P] [US3] Create component tests for `HeartbeatWarnings` in `frontend/src/tests/HeartbeatWarnings.test.tsx` (polling, list rendering, newest first)

### Implementation for User Story 3

- [ ] T023 [P] [US3] Add `fetchHeartbeatWarnings` and `fetchHeartbeatConfig` to `frontend/src/api.ts`
- [ ] T024 [US3] Create `HeartbeatWarnings` React component in `frontend/src/components/HeartbeatWarnings.tsx` with interval polling (2s)
- [ ] T025 [US3] Integrate `HeartbeatWarnings` component into `frontend/src/components/SystemHealthPanel.tsx`
- [ ] T026 [US3] Add styling for warnings (distinguish between latency and clock shift types) in `frontend/src/components/HeartbeatWarnings.css`

**Checkpoint**: All user stories complete - diagnostic heartbeat monitoring fully integrated from background to UI.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [ ] T027 [P] Verify `logs/` directory creation and log rotation behavior in production-like environment
- [ ] T028 Validate SC-004: Performance impact check (CPU/Memory usage < budget)
- [ ] T029 Clean up any temporary debug logging in background service
- [ ] T030 Run all acceptance scenarios from `spec.md` and validate against `quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 completion. Blocks P1 monitoring implementation.
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion. Foundation for alerts and UI.
- **User Story 2 (Phase 4)**: Depends on Phase 3 completion (needs events to log). Foundation for UI.
- **User Story 3 (Phase 5)**: Depends on Phase 4 completion (needs API to fetch warnings).
- **Polish (Phase 6)**: Final sign-off after all stories complete.

### execution Strategy

1. **Phase 1-2**: Foundation setup (Models, Config).
2. **Phase 3**: MVP Implementation (Core background service).
3. **Phase 4**: Persistence & Access (Logging & API).
4. **Phase 5**: Visibility (UI components).
5. **Phase 6**: Final Verification.

---

## Parallel Example: Foundational Phase

```bash
# Implement core models in parallel:
Task: "Implement HeartbeatConfig model in backend/src/Models/HeartbeatConfig.cs"
Task: "Implement DriftEvent model in backend/src/Models/DriftEvent.cs"
Task: "Add DriftEvent and HeartbeatConfig interfaces to frontend/src/types.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1-2)

1. Complete Setup and Foundational phases.
2. Implement `HeartbeatMonitor` background service (US1).
3. Implement `HeartbeatLogger` and API controller (US2).
4. **STOP and VALIDATE**: Verify logs are created and API returns events after simulating latency.

### Incremental Delivery

1. Foundation is solid.
2. Background monitoring ensures system health awareness.
3. Persistent logging enables historical analysis.
4. UI display provides real-time operator visibility.
5. All components independently testable using automated suites.
