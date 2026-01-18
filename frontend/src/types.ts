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
