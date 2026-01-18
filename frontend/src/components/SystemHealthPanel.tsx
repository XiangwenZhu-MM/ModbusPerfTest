import React, { useEffect, useState } from 'react';
import { SystemHealthMetric, HeartbeatMetrics as HeartbeatMetricsType } from '../types';
import { api } from '../api';
import HeartbeatMetrics from './HeartbeatMetrics';
import HeartbeatWarnings from './HeartbeatWarnings';

const SystemHealthPanel: React.FC = () => {
  const [health, setHealth] = useState<SystemHealthMetric | null>(null);
  const [currentMetrics, setCurrentMetrics] = useState<HeartbeatMetricsType | null>(null);
  const [queueStats, setQueueStats] = useState<any>(null);

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

  const getSaturationStatus = (index: number) => {
    if (index < 80) return 'good';
    if (index < 100) return 'warning';
    return 'critical';
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

      <HeartbeatMetrics />
      <HeartbeatWarnings />
    </div>
  );
};

export default SystemHealthPanel;
