# Tasks: NModbusAsync Switch

**Input**: specs/006-nmodbusasync-switch/plan.md, spec.md  
**Feature Branch**: `006-nmodbusasync-switch`  
**Created**: 2026-01-21

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Add NModbusAsync NuGet package to backend/ModbusPerfTest.Backend.csproj
- [X] T002 [P] Document ModbusLibrary configuration option in README.md or backend docs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add ModbusLibrary configuration setting to backend/appsettings.json with default value "NModbus"
- [X] T004 [P] Add ModbusLibrary configuration setting to backend/appsettings.Development.json
- [X] T005 Create ModbusLibrarySelector enum/config model in backend/src/Models/ModbusLibrarySelector.cs
- [X] T006 Update dependency injection in backend/Program.cs to select IModbusDriver based on ModbusLibrary config

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Switch Modbus Library (Priority: P1) 🎯 MVP

**Goal**: Allow switching between NModbus and NModbusAsync via appsettings

**Independent Test**: Change ModbusLibrary config value, restart backend, verify correct library is used for Modbus operations

### Implementation for User Story 1

- [X] T007 [P] [US1] Implement NModbusAsyncDriver class in backend/src/Services/NModbusAsyncDriver.cs implementing IModbusDriver
- [X] T008 [US1] Integrate NModbusAsyncDriver with dependency injection in backend/Program.cs
- [X] T009 [US1] Verify DeviceScanWorker and DeviceScanManager use IModbusDriver abstraction (no changes needed if already using interface)
- [X] T010 [US1] Manual test: Set ModbusLibrary to "NModbus" in appsettings.json, restart, verify NModbus driver is used
- [X] T011 [US1] Manual test: Set ModbusLibrary to "NModbusAsync" in appsettings.json, restart, verify NModbusAsync driver is used

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Configuration Validation (Priority: P2)

**Goal**: Validate ModbusLibrary config at startup and report clear errors

**Independent Test**: Provide invalid or missing config value, verify system reports error and does not proceed

### Implementation for User Story 2

- [X] T012 [P] [US2] Add config validation logic in backend/Program.cs to check ModbusLibrary value at startup
- [X] T013 [US2] Implement error logging and graceful failure if ModbusLibrary value is invalid or unsupported
- [X] T014 [US2] Test: Set ModbusLibrary to invalid value (e.g., "Invalid"), verify system logs error and does not start
- [X] T015 [US2] Test: Remove ModbusLibrary setting, verify system uses default (NModbus) or reports error as designed

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: Final Polish & Cross-Cutting

**Purpose**: Documentation, cleanup, and final validation

- [X] T016 Update README.md with ModbusLibrary configuration documentation and valid values
- [X] T017 Add inline code comments explaining ModbusLibrary selection logic in Program.cs
- [X] T018 Review code for consistency, naming conventions, and best practices
- [X] T019 Final integration test: Verify switching between NModbus and NModbusAsync works end-to-end

---

## Dependencies

- **Phase 1 and 2 must be completed before any user story work**
- **US1 and US2 can be implemented and tested independently after foundational tasks**
- T008 depends on T007
- T012-T015 can start after T006 is complete

## Parallel Execution Opportunities

- T002, T004, T005 can be done in parallel
- T007 can be done in parallel with T012 preparation
- T010, T011, T014, T015 are manual tests that can be batched

## Implementation Strategy

- **MVP**: Complete all tasks for User Story 1 (T007–T011)
- **Incremental delivery**: Add config validation (User Story 2) after MVP
- **Polish**: Final documentation and cleanup

---

**Total tasks:** 19  
**User Story 1 tasks:** 5  
**User Story 2 tasks:** 4  
**Parallel opportunities:** Setup, foundational, and some implementation tasks
