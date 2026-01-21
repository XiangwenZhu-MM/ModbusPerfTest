export interface DeviceLevelMetric {
  deviceId: string;
  frameId: string;
  startAddress: number;
  count: number;
  scanFrequencyMs: number;
  queueDurationMs: number;
  deviceResponseTimeMs: number;
  actualSamplingIntervalMs: number;
  droppedCount: number;
  droppedTPM: number;
  timestamp: string;
}

export interface SystemHealthMetric {
  ingressTPM: number;
  egressTPM: number;
  saturationIndex: number;
  droppedTPM: number;
  timestamp: string;
}

export interface ClockDriftStatistics {
  totalMeasurements: number;
  averageDriftMs: number;
  minDriftMs: number;
  maxDriftMs: number;
  standardDeviationMs: number;
  timestamp: string;
}

export interface DataQualitySummary {
  totalDataPoints: number;
  goodCount: number;
  staleCount: number;
  uncertainCount: number;
  timestamp: string;
}

export interface DataQualityState {
  dataPointId: string;
  quality: 'Good' | 'Stale' | 'Uncertain';
  lastKnownValue: any;
  lastSuccessTimestamp?: string;
  stalenessThresholdMs: number;
}

export interface DriftEvent {
  eventType: 'PERFORMANCE_DEGRADED' | 'CLOCK_SHIFT';
  timestamp: string;
  monoElapsedMs: number;
  wallElapsedMs: number;
  expectedIntervalMs: number;
  deviationMs: number;
  message: string;
}

export interface HeartbeatConfig {
  enabled: boolean;
  intervalMs: number;
  thresholdMs: number;
  maxWarningsInMemory: number;
  logFilePath: string;
  maxLogFileSizeMB: number;
}

export interface ThreadPoolMetrics {
  workerThreads: number;
  completionPortThreads: number;
  pendingWorkItems: number;
  minWorkerThreads: number;
  maxWorkerThreads: number;
}

export interface HeartbeatMetrics {
  lastMonoElapsedMs: number;
  lastWallElapsedMs: number;
  lastCheckedAt: string;
  expectedIntervalMs: number;
  latencyMs: number;
  clockDriftMs: number;
  isHealthy: boolean;
}

export interface TimeRangeCount {
  count: number;
  startTime: string;
  endTime: string;
}

export interface DataPointCountsResult {
  lastMinute: TimeRangeCount;
  last10Minutes: TimeRangeCount;
  lastHour: TimeRangeCount;
  last2Hours: TimeRangeCount;
}
