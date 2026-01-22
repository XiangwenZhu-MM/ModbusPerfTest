# Implementation Plan: System Resource Metrics

**Branch**: `007-system-resource-metrics` | **Date**: 2026-01-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-system-resource-metrics/spec.md`

## Summary

This feature adds real-time monitoring of the application's CPU and Memory usage to the dashboard. It will provide developers and operators with visibility into the system's resource consumption while running high-load Modbus scanning tasks.

## Technical Context

**Language/Version**: C# (.NET 10), TypeScript/React
**Primary Dependencies**: 
  - Backend: `System.Diagnostics` (Process class)
  - Frontend: React, Recharts (for visualization if needed)
**Performance Goals**: 
  - Minimal sampling overhead (<0.5% CPU).
  - Accurate representation of private working set memory.
**Constraints**:
  - Sample interval should be around 2 seconds to match dashboard refresh rates.

## Project Structure

### Documentation (this feature)

```text
specs/007-system-resource-metrics/
├── plan.md              # This file
├── spec.md              # Specification
├── tasks.md             # Implementation tasks
└── checklists/          # Checklists
```

### Affected Files

```text
backend/
├── src/Models/
│   └── SystemResourceMetrics.cs (New)
├── src/Services/
│   └── ResourceMonitorService.cs (New) or ScadaHealthMonitor.cs (Update)
└── src/Api/
    └── MetricsController.cs (Update)

frontend/
├── src/
│   ├── types.ts (Update)
│   ├── api.ts (Update)
│   └── components/
│       └── SystemHealthPanel.tsx (Update)
```

## Implementation Strategy

### Phase 1: Backend Implementation
1.  **Define Model**: Create `SystemResourceMetrics` to hold CPU % and Memory MB.
2.  **Sampling Service**: Implement a background service (or add to `ScadaHealthMonitor`) that:
    - Calculates CPU % by comparing `TotalProcessorTime` over a 1-2 second interval.
    - Captures `WorkingSet64` or `PrivateMemorySize64` for memory.
3.  **Expose API**: Update `MetricsController` to return these values in a new `system-resources` endpoint.

### Phase 2: Frontend Implementation
1.  **Types**: Update `SystemMetrics` or similar interface in `types.ts`.
2.  **API Client**: Update `api.ts` to call the new endpoint or refresh the existing metrics call if included there.
3.  **UI Components**: Update the "System Health" panel on the dashboard to show CPU % and Memory (MB) with descriptive labels.

## Constitution Check

- All features begin with a written, testable specification (✅)
- Each user story is independently testable and deployable (✅)
- All metrics are observable via dashboard (✅)
- Simplicity: Using built-in .NET Process class for metrics (✅)
