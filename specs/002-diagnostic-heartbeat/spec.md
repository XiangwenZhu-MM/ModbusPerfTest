# Feature Specification: Diagnostic Heartbeat Monitor

**Feature Branch**: `002-diagnostic-heartbeat`  
**Created**: January 18, 2026  
**Status**: Draft  
**Input**: User description: "Diagnostic heartbeat monitoring system to detect performance degradation (Internal Latency from CPU/GC issues) and clock changes (System Drift from NTP/manual adjustments), with configurable intervals, threshold-based alerting, and logging to file and UI display"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic System Health Monitoring (Priority: P1)

A SCADA system operator needs continuous, automatic monitoring to detect when the system is experiencing performance problems or time synchronization issues without requiring manual checks or intervention.

**Why this priority**: Core functionality - without automatic monitoring running in the background, the entire feature delivers no value. This is the foundation that enables all other capabilities.

**Independent Test**: Can be fully tested by starting the system, waiting for the configured interval periods, and verifying that the heartbeat is pulsing at the expected frequency (observable through logs or internal metrics).

**Acceptance Scenarios**:

1. **Given** the system is running normally, **When** the heartbeat monitor starts, **Then** it pulses at the configured interval (default 1000ms) continuously
2. **Given** the heartbeat is running, **When** a CPU spike occurs causing execution delays, **Then** the monitor detects the internal latency within one heartbeat cycle
3. **Given** the heartbeat is running, **When** the system clock is adjusted (NTP sync or manual change), **Then** the monitor detects the clock shift within one heartbeat cycle
4. **Given** a drift event has occurred, **When** the system returns to normal operation, **Then** the heartbeat automatically realigns its schedule and continues monitoring

---

### User Story 2 - Warning Notification and Logging (Priority: P2)

When system health issues are detected, operators need clear, timestamped warnings logged to a file so they can investigate problems, establish patterns over time, and provide evidence for troubleshooting or compliance purposes.

**Why this priority**: Alerts without persistence have limited value - operators need historical records to identify recurring issues, correlate with other events, and perform root cause analysis.

**Independent Test**: Can be tested by simulating drift conditions (CPU load, clock changes) and verifying that warning messages are written to the log file with correct timestamps, event types, and deviation measurements.

**Acceptance Scenarios**:

1. **Given** an internal latency event occurs, **When** the deviation exceeds the threshold, **Then** a warning is logged to file with event type "PERFORMANCE_DEGRADED", timestamp, expected interval, actual duration, and deviation amount
2. **Given** a clock shift event occurs, **When** the monotonic and wall clock diverge significantly, **Then** a warning is logged to file with event type "CLOCK_SHIFT", timestamp, monotonic time, wall clock time, and deviation amount
3. **Given** multiple warnings occur over time, **When** viewing the log file, **Then** all warnings are preserved chronologically with complete diagnostic information
4. **Given** the log file reaches a certain size, **When** new warnings are generated, **Then** the system manages log rotation to prevent unbounded growth

---

### User Story 3 - Real-Time Warning Display (Priority: P3)

Operators viewing the system metrics dashboard need to see recent warnings displayed prominently so they can immediately identify current or recent system health issues without having to access log files.

**Why this priority**: Enhances operator awareness but depends on P1 (detection) and P2 (logging). The system is still functional without UI display since logs provide all necessary information.

**Independent Test**: Can be tested by triggering warnings and verifying they appear in the system metrics UI area, with the latest warning at the top, showing event type, occurrence time, and deviation details.

**Acceptance Scenarios**:

1. **Given** the system metrics dashboard is open, **When** a new warning is generated, **Then** it appears at the top of the warnings list within 2 seconds
2. **Given** multiple warnings exist, **When** viewing the warnings display, **Then** warnings are ordered with newest first, showing event type, timestamp, and deviation for each
3. **Given** warnings are displayed, **When** the operator views the information, **Then** they can distinguish between "Internal Latency" and "System Drift" warnings clearly
4. **Given** a large number of warnings have accumulated, **When** viewing the display, **Then** only recent warnings are shown (configurable limit, default 50 entries)

---

### Edge Cases

- What happens when the system experiences extreme CPU starvation (>10 seconds blocked)?
  - The heartbeat will detect a massive internal latency on its next pulse and log the full delay duration. The system will automatically realign and continue monitoring.

- How does the system handle rapid, successive clock changes?
  - Each clock shift is detected and logged independently. The monotonic timer ensures internal latency detection remains accurate regardless of system clock manipulation.

- What happens when the log file cannot be written (disk full, permissions issue)?
  - The system should continue monitoring and detecting issues, but log a separate error about the logging failure. In-memory recent warnings should still be maintained for UI display.

- How does the system behave during system startup or shutdown?
  - During startup, the first pulse establishes baselines and subsequent pulses begin detection. During shutdown, the monitor stops cleanly without false positives from shutdown delays.

- What happens when the configured threshold is set lower than the configured interval?
  - The system should validate configuration at startup and use sensible defaults (threshold must be >= interval, recommended 200% of interval) to prevent false positives.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST pulse a heartbeat at a configurable interval with a default of 1000 milliseconds

- **FR-002**: System MUST use a high-resolution monotonic timer (independent of system clock) to measure elapsed time between heartbeats

- **FR-003**: System MUST detect "Internal Latency" when the monotonic timer shows the heartbeat took longer than the configured threshold to execute

- **FR-004**: System MUST detect "System Drift" when the monotonic timer and system wall clock measurements diverge by more than 500 milliseconds

- **FR-005**: System MUST support a configurable threshold for warning generation with a default of 2000 milliseconds (200% of default interval)

- **FR-006**: System MUST automatically recover and realign the heartbeat schedule after detecting a drift event

- **FR-007**: System MUST log warnings to a persistent file when deviations exceed the configured threshold

- **FR-008**: Warning logs MUST include the event type ("PERFORMANCE_DEGRADED" for internal latency, "CLOCK_SHIFT" for system drift)

- **FR-009**: Warning logs MUST include the occurrence timestamp, expected interval, actual measured duration, and calculated deviation

- **FR-010**: System MUST display recent warnings in the system metrics area of the user interface

- **FR-011**: Warning display MUST show warnings in reverse chronological order (latest warning at the top)

- **FR-012**: Each displayed warning MUST show the warning type, occurrence time, and deviation information

- **FR-013**: System MUST limit the number of warnings displayed in the UI to prevent performance degradation (reasonable default: 50 most recent warnings)

- **FR-014**: System MUST consume negligible CPU and memory resources during normal operation (non-intrusive monitoring)

- **FR-015**: System MUST handle log file rotation to prevent unbounded disk usage

### Key Entities

- **Heartbeat Monitor**: The background service that pulses at regular intervals and performs drift detection. Maintains monotonic timer reference, last wall clock reference, configuration settings (interval, threshold), and operational state.

- **Drift Event**: A detected anomaly representing either internal latency or system drift. Contains event type, detection timestamp, monotonic elapsed time, wall clock elapsed time, calculated deviation, and descriptive message.

- **Warning Log Entry**: A persistent record of a drift event written to the log file. Contains timestamp, event type, deviation measurements, and diagnostic context.

- **Warning Display Record**: An in-memory representation of recent drift events for UI presentation. Contains event type, formatted occurrence time, deviation summary, and display-specific formatting.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The heartbeat monitor operates continuously with timing accuracy within 5% of the configured interval under normal system conditions

- **SC-002**: Internal latency events are detected and logged within one heartbeat cycle (maximum 1 second delay at default settings) after the latency occurs

- **SC-003**: System drift events are detected and logged within one heartbeat cycle after the clock change occurs

- **SC-004**: The monitor consumes less than 0.5% CPU and less than 10 MB memory during normal operation (non-intrusive requirement)

- **SC-005**: Operators can view warning history spanning at least 24 hours of system operation through log files

- **SC-006**: Recent warnings appear in the UI within 2 seconds of detection

- **SC-007**: The system successfully recovers and resumes accurate monitoring after experiencing drift events of varying magnitudes (100ms to 10+ seconds)

- **SC-008**: Warning logs are preserved across system restarts and remain accessible for troubleshooting

## Assumptions

- The .NET runtime provides PeriodicTimer and Stopwatch APIs for high-resolution monotonic timing (available in .NET 6+)
- The existing system metrics UI area can accommodate a warnings display section
- Standard file system logging libraries are available and sufficient for log management
- The 500ms threshold for detecting clock drift is appropriate for typical SCADA NTP synchronization scenarios
- Operators have read access to log files for historical analysis
- The system runs on hardware with stable monotonic clock support
- Log file rotation policies (size limits, retention) follow standard organizational guidelines

## Open Questions

None - all requirements are specified with reasonable defaults based on typical SCADA system monitoring needs.
