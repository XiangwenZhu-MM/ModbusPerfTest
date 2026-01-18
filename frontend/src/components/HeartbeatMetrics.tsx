import React, { useState, useEffect } from 'react';
import { HeartbeatMetrics as HeartbeatMetricsType } from '../types';
import { api } from '../api';
import './HeartbeatMetrics.css';

const HeartbeatMetrics: React.FC = () => {
  const [metrics, setMetrics] = useState<HeartbeatMetricsType | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [simulating, setSimulating] = useState<boolean>(false);

  useEffect(() => {
    // Fetch metrics immediately
    fetchMetrics();

    // Poll every 1 second for real-time updates
    const interval = setInterval(fetchMetrics, 1000);

    return () => clearInterval(interval);
  }, []);

  const fetchMetrics = async () => {
    try {
      const data = await api.getHeartbeatMetrics();
      setMetrics(data);
      setError(null);
    } catch (err) {
      console.error('Failed to fetch heartbeat metrics:', err);
      setError('Failed to load metrics');
    }
  };

  const handleSimulateLoad = async () => {
    try {
      setSimulating(true);
      await api.simulateHeartbeatLoad(3000); // 3 seconds of CPU load
      // Metrics will update automatically via polling
    } catch (err) {
      console.error('Failed to simulate load:', err);
      setError('Failed to simulate load');
    } finally {
      setSimulating(false);
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString();
  };

  const getHealthStatus = () => {
    if (!metrics) return 'unknown';
    if (metrics.isHealthy) return 'healthy';
    if (metrics.latencyMs > 0 && metrics.clockDriftMs > 500) return 'critical';
    if (metrics.latencyMs > 0) return 'warning-latency';
    if (metrics.clockDriftMs > 500) return 'warning-drift';
    return 'healthy';
  };

  const getStatusLabel = () => {
    const status = getHealthStatus();
    switch (status) {
      case 'healthy': return '✓ Healthy';
      case 'warning-latency': return '⚠ Latency Detected';
      case 'warning-drift': return '⚠ Clock Drift Detected';
      case 'critical': return '⚠ Multiple Issues';
      default: return '? Unknown';
    }
  };

  if (error) {
    return (
      <div className="heartbeat-metrics">
        <h3>Real-Time System Metrics</h3>
        <div className="error-message">{error}</div>
      </div>
    );
  }

  if (!metrics) {
    return (
      <div className="heartbeat-metrics">
        <h3>Real-Time System Metrics</h3>
        <div className="loading">Loading...</div>
      </div>
    );
  }

  return (
    <div className="heartbeat-metrics">
      <h3>Real-Time System Metrics</h3>
      <div className={`status-indicator ${getHealthStatus()}`}>
        <span className="status-label">{getStatusLabel()}</span>
        <span className="status-time">Last Check: {formatTimestamp(metrics.lastCheckedAt)}</span>
      </div>

      <div className="metrics-grid">
        <div className="metric-item">
          <div className="metric-label">System Latency</div>
          <div className={`metric-value ${metrics.latencyMs > 0 ? 'alert' : ''}`}>
            {metrics.latencyMs} ms
          </div>
          <div className="metric-detail">
            Expected: {metrics.expectedIntervalMs}ms, Actual: {metrics.lastMonoElapsedMs}ms
          </div>
        </div>

        <div className="metric-item">
          <div className="metric-label">Clock Drift</div>
          <div className={`metric-value ${metrics.clockDriftMs > 500 ? 'alert' : ''}`}>
            {metrics.clockDriftMs.toFixed(1)} ms
          </div>
          <div className="metric-detail">
            Mono: {metrics.lastMonoElapsedMs}ms, Wall: {metrics.lastWallElapsedMs.toFixed(1)}ms
          </div>
        </div>
      </div>

      <div className="metrics-info">
        <div className="info-item">
          <span className="info-label">Heartbeat Interval:</span>
          <span className="info-value">{metrics.expectedIntervalMs}ms</span>
        </div>
        <div className="info-item">
          <button 
            className="simulate-button" 
            onClick={handleSimulateLoad}
            disabled={simulating}
          >
            {simulating ? 'Simulating Load...' : '🔥 Test Latency Detection (3s CPU Load)'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default HeartbeatMetrics;
