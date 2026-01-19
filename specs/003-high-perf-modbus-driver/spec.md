# Feature Specification: High Performance Modbus Driver

**Feature Branch**: `003-high-perf-modbus-driver`  
**Created**: January 19, 2026  
**Status**: Completed  
**Input**: User description: "Implement a high-performance Modbus driver using NModbus async operations to support independent polling intervals for multiple address groups on a single device connection."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Concurrent Multi-Frequency Polling (Priority: P1)

As a SCADA engineer, I want to poll critical registers every 100ms and diagnostic registers every 1000ms on the same PLC using a single TCP connection, so that I can maximize data freshness without overloading multiple TCP connection slots on the PLC.

**Why this priority**: Correct handling of multiple frequencies is the core requirement for high-performance data acquisition.

**Independent Test**: Configure two frames on one device with 100ms and 1000ms intervals. Verify that the 100ms frame achieves ~10 polls per second regardless of the 1000ms frame's presence or network response time.

**Acceptance Scenarios**:

1. **Given** a Modbus device with two frames (100ms and 5000ms), **When** both frames are active, **Then** the 100ms frame should achieve its targeted frequency within 5% tolerance.
2. **Given** a single TCP connection to a device, **When** multiple async read requests are issued simultaneously, **Then** the driver must serialize them correctly without dropping any requests.

---

### User Story 2 - Automated Connection Recovery (Priority: P2)

As a system operator, I want the Modbus driver to automatically reconnect if a TCP connection is interrupted, so that data collection resumes without manual intervention.

**Why this priority**: Operational continuity is critical for monitoring systems.

**Independent Test**: Disconnect the network/simulator during operation. Verify that the system logs errors and resumes polling automatically once the device is back online.

**Acceptance Scenarios**:

1. **Given** an established connection, **When** the connection is lost (e.g., simulator stopped), **Then** the driver must remove the failed connection from its pool.
2. **Given** a lost connection, **When** the next polling cycle occurs, **Then** the driver must attempt to establish a new connection.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The driver MUST use asynchronous I/O (`ReadHoldingRegistersAsync`) to prevent thread blocking during network operations.
- **FR-002**: The driver MUST maintain exactly one TCP connection per unique IP:Port combination.
- **FR-003**: The driver MUST automatically manage Modbus Transaction IDs to match requests with responses across concurrent tasks.
- **FR-004**: The driver MUST support independent polling loops for each configured frame.
- **FR-005**: The driver MUST optimize TCP performance by disabling Nagle's algorithm (TCP NoDelay).
- **FR-006**: The driver MUST provide a mechanism to switch between Standard and High-Performance implementations in appsettings.json.

### Key Entities

- **Modbus Connection**: Represents a persistent TCP/IP connection to a specific Modbus device, managed as a singleton per device key.
- **Frame Configuration**: Defines a range of registers and the frequency at which they should be polled.

## Success Criteria *(mandatory)*

- **SC-001**: Critical frames (e.g., 100ms) maintain their targeted frequency with <5% deviation even when sharing a connection with slower frames.
- **SC-002**: System supports concurrent polling tasks across multiple devices without global locks.
- **SC-003**: Connection overhead (TCP handshake) occurs only once per device unless an error occurs.

## Assumptions

- The target PLC supports multiple successive async requests on the same TCP socket.
- The network infrastructure has sufficient bandwidth for the combined frequency of all frames.

