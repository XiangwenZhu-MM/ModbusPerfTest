const API_BASE_URL = 'http://localhost:5000/api';

export const api = {
  async getDeviceMetrics(count: number = 100) {
    const response = await fetch(`${API_BASE_URL}/Metrics/device?count=${count}`);
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

  async simulateHeartbeatLoad(durationMs: number = 3000) {
    const response = await fetch(`${API_BASE_URL}/Heartbeat/simulate-load?durationMs=${durationMs}`, {
      method: 'POST',
    });
    return response.json();
  },
};
