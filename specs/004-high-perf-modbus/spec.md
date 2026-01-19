# Feature Specification: High Performance Modbus Driver

**Feature Branch**: `004-high-perf-modbus`  
**Created**: January 19, 2026  
**Status**: Draft  
**Input**: User description: "Implement a high-performance Modbus driver using NModbus async operations to support independent polling intervals for multiple address groups on a single device connection."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Concurrent Multi-Frequency Polling (Priority: P1)

As a SCADA engineer, I want to poll critical registers (e.g., setpoints) every 100ms and diagnostic registers (e.g., firmware version) every 10000ms on the same PLC using a single TCP connection, so that I can maximize data freshness for critical control loops without exhausting the PLC's limited TCP connection slots or bandwidth.

**Why this priority**: Correct handling of multiple frequencies on a shared connection is the core innovation required for the high-performance driver.

**Independent Test**: Configure two address groups ("frames") on a single physical/simulated device: one at 100ms and one at 5000ms. Verify that the 100ms frame consistently receives data 10 times per second, even while the 5000ms frame is executing its periodic read.

**Acceptance Scenarios**:

1. **Given** a Modbus device with two frames configured at different frequencies (100ms and 5000ms), **When** the monitoring starts, **Then** the driver must initiate independent polling loops for each frame.
2. **Given** concurrent polling tasks, **When** they share a single TCP connection, **Then** the driver must utilize asynchronous I/O to ensure neither task blocks the other's execution timing.

---

### User Story 2 - Shared Connection Persistence (Priority: P2)

As a system administrator, I want the system to maintain exactly one persistent TCP connection per device for all registered register groups, so that I can minimize network overhead and avoid \"connection refused\" errors from PLCs with strict connection limits.

**Why this priority**: PLC resources are often severely constrained; maintaining multiple connections to the same device is a common cause of system instability in large-scale SCADA deployments.

**Independent Test**: Monitor network sockets (e.g., using `netstat`) while polling multiple register groups on the same IP/Port. Verify only one active TCP connection exists for that target.

**Acceptance Scenarios**:

1. **Given** multiple register groups target the same IP address and port, **When** polling begins, **Then** the system must establish a single shared connection context for all groups.
2. **Given** an established connection being used by one frame, **When** a second frame starts polling, **Then** it must reuse the existing connection instead of opening a new one.

---

### User Story 3 - Automated Connection Resiliency (Priority: P3)

As a system operator, I want the driver to automatically detect connection failures and attempt to re-establish the shared connection, so that data collection resumes seamlessly after network interruptions or device reboots.

**Why this priority**: Manual intervention required for connection recovery results in significant data gaps and operational risk.

**Independent Test**: Simulate a network failure (e.g., stop the device simulator). Verify the system logs the error and successfully resumes polling within one cycle of the device becoming available again.

**Acceptance Scenarios**:

1. **Given** an active connection that encounter a TCP-level error, **When** a frame attempts to poll, **Then** the driver must invalidate the shared connection context and log the failure.
2. **Given** an invalidated connection, **When** the next polling interval occurs, **Then** the driver must attempt a fresh handshake before proceeding with the read.

---

### Edge Cases

- **Congestion**: What happens when the sum of poll requests exceeds the device's response capacity? (Driver should queue requests and prioritize the most recent or log jitter).
- **Slave ID Mismatch**: How does the system handle different Slave IDs on the same IP:Port? (The shared connection must support multiplexing different Slave ID requests over the same socket).
- **Timeouts**: If a long-running 10000ms poll times out, does it kill the 100ms poll? (Asynchronous isolation should prevent one timeout from blocking other concurrent tasks).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST use native asynchronous Modbus read operations to prevent thread-pool blocking during network I/O.
- **FR-002**: System MUST implement a connection pool that maps unique ``IP:Port`` combinations to a single shared connection instance.
- **FR-003**: System MUST provide a lock-free or highly concurrent mechanism to allow multiple tasks to issue requests to the same shared connection simultaneously.
- **FR-004**: System MUST automatically manage Modbus Transaction IDs (or equivalent protocol sequences) to ensure responses are correctly mapped back to the originating asynchronous task.
- **FR-005**: System MUST optimize TCP socket settings for low latency (e.g., disabling Nagle's algorithm/TCP NoDelay).
- **FR-006**: System MUST allow configuring individual register groups (\"frames\") with independent scan frequencies.
- **FR-007**: System MUST provide a fallback mechanism to a standard sequential driver if compatibility issues arise with specific hardware.

### Key Entities

- **Connection Context**: Represents a shared TCP socket and protocol state for a specific device. Attributes: IP Address, Port, Protocol master instance, Last activity timestamp.
- **Scan Frame**: Represents a specific request for a range of registers. Attributes: Name, Slave ID, Start Address, Quantity, Frequency, Last result.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scan frequency jitter (deviation from target interval) for a 100ms frame remains under 10% even when sharing a connection with a 5000ms frame.
- **SC-002**: TCP connection count to target devices is exactly 1 per `IP:Port`, regardless of the number of registered scan frames.
- **SC-003**: System recovers from a forced connection drop and resumes data updates within 2x the slowest configured scan interval plus network timeout.
- **SC-004**: Zero thread-blocking calls are detected in the data acquisition hot-path during profiling.
