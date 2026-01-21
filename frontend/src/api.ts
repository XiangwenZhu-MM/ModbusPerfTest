import { DataPointCountsResult } from './types';

const API_BASE_URL = 'http://localhost:5000/api';

export const api = {
  async getDeviceMetrics(count: number = 100) {
    const response = await fetch(`${API_BASE_URL}/Metrics/device?count=${count}`);
    return response.json();
  },

  async getAllFrames() {
    const response = await fetch(`${API_BASE_URL}/Metrics/frames`);
    return response.json();
  },

  async getSystemHealth() {
    const response = await fetch(`${API_BASE_URL}/Metrics/system`);
    return response.json();
  },

  async getQueueStats() {
    const response = await fetch(`${API_BASE_URL}/Metrics/queue`);
    return response.json();
  },

  async getClockDriftStatistics() {
    const response = await fetch(`${API_BASE_URL}/ClockDrift/statistics`);
    return response.json();
  },

  async getDataQualitySummary() {
    const response = await fetch(`${API_BASE_URL}/DataQuality/summary`);
    return response.json();
  },

  async getAllDataPoints() {
    const response = await fetch(`${API_BASE_URL}/DataQuality/datapoints`);
    return response.json();
  },

  async uploadConfig(configJson: string) {
    const response = await fetch(`${API_BASE_URL}/DeviceConfig/upload`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(configJson),
    });
    return response.json();
  },

  async getHeartbeatWarnings() {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/warnings`);
    return response.json();
  },

  async getHeartbeatConfig() {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/config`);
    return response.json();
  },

  async getHeartbeatMetrics() {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/metrics`);
    return response.json();
  },

  async getHeartbeatMetricsHistory() {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/metrics/history`);
    return response.json();
  },

  async simulateHeartbeatLoad(durationMs: number = 3000) {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/simulate-load?durationMs=${durationMs}`, {
      method: 'POST',
    });
    return response.json();
  },

  async simulateHeartbeatGC() {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/simulate-gc`, {
      method: 'POST',
    });
    return response.json();
  },

  async getDataPointCounts(): Promise<DataPointCountsResult> {
    const response = await fetch(`${API_BASE_URL}/DataPoints/counts`);
    return response.json();
  },

  async getDeviceDataCountsLastMinute(): Promise<{deviceName: string, count: number}[]> {
    const response = await fetch(`${API_BASE_URL}/DataPoints/devices/last-minute`);
    return response.json();
  },

  async clearDataPoints() {
    const response = await fetch(`${API_BASE_URL}/DataPoints/clear`, {
      method: 'DELETE',
    });
    return response.json();
  },

  async getThreadPoolMetrics() {
    const response = await fetch(`${API_BASE_URL}/ThreadPool/metrics`);
    return response.json();
  },

  // Scan Control APIs
  async startScanning() {
    const response = await fetch(`${API_BASE_URL}/ScanControl/start`, {
      method: 'POST',
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to start scanning');
    }
    return response.json();
  },

  async stopScanning() {
    const response = await fetch(`${API_BASE_URL}/ScanControl/stop`, {
      method: 'POST',
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to stop scanning');
    }
    return response.json();
  },

  async getScanningStatus() {
    const response = await fetch(`${API_BASE_URL}/ScanControl/status`);
    return response.json();
  },

  // Configuration APIs
  async getConfiguration() {
    const response = await fetch(`${API_BASE_URL}/Configuration`);
    return response.json();
  },

  async applyConfiguration(config: any) {
    const response = await fetch(`${API_BASE_URL}/Configuration/apply`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(config),
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to apply configuration');
    }
    return response.json();
  },
};
