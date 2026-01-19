import React, { useEffect, useState } from 'react';
import { SystemHealthMetric, HeartbeatMetrics as HeartbeatMetricsType, DataPointCountsResult } from '../types';
import { api } from '../api';
import HeartbeatMetrics from './HeartbeatMetrics';
import HeartbeatWarnings from './HeartbeatWarnings';

const SystemHealthPanel: React.FC = () => {
  const [health, setHealth] = useState<SystemHealthMetric | null>(null);
  const [currentMetrics, setCurrentMetrics] = useState<HeartbeatMetricsType | null>(null);
  const [queueStats, setQueueStats] = useState<any>(null);
  const [dataPointCounts, setDataPointCounts] = useState<DataPointCountsResult | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [healthData, metricsData, queueData] = await Promise.all([
          api.getSystemHealth(),
          api.getHeartbeatMetrics(),
          api.getQueueStats(),
        ]);
        setHealth(healthData);
        setCurrentMetrics(metricsData);
        setQueueStats(queueData);
      } catch (error) {
        console.error('Error fetching system health:', error);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 5000); // Update every 5 seconds

    return () => clearInterval(interval);
  }, []);

  const handleQueryDataPoints = async () => {
    try {
      const counts = await api.getDataPointCounts();
      setDataPointCounts(counts);
    } catch (error) {
      console.error('Error fetching data point counts:', error);
    }
  };

  const handleClearDataPoints = async () => {
    if (!window.confirm('Are you sure you want to delete all data points? This cannot be undone.')) {
      return;
    }
    try {
      await api.clearDataPoints();
      setDataPointCounts(null);
      alert('All data points have been cleared successfully');
    } catch (error) {
      console.error('Error clearing data points:', error);
      alert('Failed to clear data points');
    }
  };

  return (
    <div className="panel">
      <h2>System Health Metrics</h2>
      
      {health && currentMetrics ? (
        <div className="metrics-grid">
          <div className={`metric-card ${currentMetrics.latencyMs > 0 ? 'metric-alert' : ''}`}>
            <h3>System Latency</h3>
            <div className="metric-value">{currentMetrics.latencyMs} ms</div>
            <div className="metric-label">
              {currentMetrics.latencyMs > 0 ? '⚠️ Delayed' : '✓ On Time'}
            </div>
          </div>
          <div className="metric-card">
            <h3>Ingress Rate</h3>
            <div className="metric-value">{health.ingressTPM.toFixed(1)} TPM</div>
            <div className="metric-label">Tasks created per minute</div>
          </div>
          <div className="metric-card">
            <h3>Egress Rate</h3>
            <div className="metric-value">{health.egressTPM.toFixed(1)} TPM</div>
            <div className="metric-label">Tasks completed per minute</div>
          </div>
          <div className="metric-card">
            <h3>Total Dropped Tasks</h3>
            <div className="metric-value">{queueStats?.totalDropped || 0}</div>
            <div className="metric-label">Lifetime dropped count</div>
          </div>
        </div>
      ) : (
        <p>Loading system health...</p>
      )}

      <div className="subsection">
        <h3>Queue Statistics</h3>
        {queueStats ? (
          <div className="stats-grid">
            <div><strong>Current Size:</strong> {queueStats.currentSize}</div>
            <div><strong>Total Enqueued:</strong> {queueStats.totalEnqueued}</div>
            <div><strong>Total Dequeued:</strong> {queueStats.totalDequeued}</div>
            <div><strong>Total Dropped:</strong> {queueStats.totalDropped}</div>
          </div>
        ) : (
          <p>Loading queue stats...</p>
        )}
      </div>

      <div className="subsection">
        <h3>Data Points Statistics</h3>
        <div style={{ marginBottom: '10px' }}>
          <button onClick={handleQueryDataPoints} style={{ padding: '8px 16px', cursor: 'pointer', marginRight: '8px' }}>
            Query Data Point Counts
          </button>
          <button onClick={handleClearDataPoints} style={{ padding: '8px 16px', cursor: 'pointer', backgroundColor: '#dc3545', color: 'white', border: 'none', borderRadius: '4px' }}>
            Clear All Data
          </button>
        </div>
        {dataPointCounts ? (
          <div className="stats-grid">
            <div>
              <strong>Last Minute:</strong> {dataPointCounts.lastMinute.count.toLocaleString()}
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.lastMinute.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.lastMinute.endTime).toLocaleTimeString()}
              </div>
            </div>
            <div>
              <strong>Last 10 Minutes:</strong> {dataPointCounts.last10Minutes.count.toLocaleString()}
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.last10Minutes.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.last10Minutes.endTime).toLocaleTimeString()}
              </div>
            </div>
            <div>
              <strong>Last Hour:</strong> {dataPointCounts.lastHour.count.toLocaleString()}
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.lastHour.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.lastHour.endTime).toLocaleTimeString()}
              </div>
            </div>
            <div>
              <strong>Last 2 Hours:</strong> {dataPointCounts.last2Hours.count.toLocaleString()}
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.last2Hours.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.last2Hours.endTime).toLocaleTimeString()}
              </div>
            </div>
          </div>
        ) : (
          <p>Click the button to query data point counts</p>
        )}
      </div>

      <HeartbeatMetrics />
      <HeartbeatWarnings />
    </div>
  );
};

export default SystemHealthPanel;
