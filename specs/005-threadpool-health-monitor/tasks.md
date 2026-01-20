# Implementation Tasks: ThreadPool Health Monitor

- [X] Create ScadaHealthMonitor class in backend/src/Services
- [X] Create ThreadPoolMetrics model in backend/src/Models

## Core Implementation
- [X] Implement metrics collection logic in ScadaHealthMonitor (worker threads, IO threads, pending work items, min/max limits)
- [X] Implement periodic background task to update and log/report metrics every second
- [X] Add alerting logic for PendingWorkItems threshold
- [X] Ensure metrics collection is non-blocking and low-overhead

## Integration
- [X] Integrate ScadaHealthMonitor into backend startup (Program.cs or service entrypoint)
- [X] Add configuration for alert thresholds and update interval (appsettings.json)

## Testing
- [ ] (SKIPPED) Add unit tests for metrics collection logic (user opted out)
- [ ] (SKIPPED) Add integration test for live monitoring and alerting (user opted out)

## Polish
- [X] Add documentation for operators (QUICKSTART.md or README)
- [X] Review and refactor for code quality and performance
