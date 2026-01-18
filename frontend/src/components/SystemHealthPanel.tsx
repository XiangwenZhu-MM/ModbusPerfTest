import React, { useEffect, useState } from 'react';
import { SystemHealthMetric, ClockDriftStatistics } from '../types';
import { api } from '../api';

const SystemHealthPanel: React.FC = () => {
  const [health, setHealth] = useState<SystemHealthMetric | null>(null);
  const [clockDrift, setClockDrift] = useState<ClockDriftStatistics | null>(null);
  const [queueStats, setQueueStats] = useState<any>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [healthData, driftData, queueData] = await Promise.all([
          api.getSystemHealth(),
          api.getClockDriftStatistics(),
          api.getQueueStats(),
        ]);
        setHealth(healthData);
        setClockDrift(driftData);
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
      
      {health ? (
        <div className="metrics-grid">
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
          <div className={`metric-card saturation-${getSaturationStatus(health.saturationIndex)}`}>
            <h3>Saturation</h3>
            <div className="metric-value">{health.saturationIndex.toFixed(1)}%</div>
            <div className="metric-label">
              {health.saturationIndex > 100 ? '⚠️ OVERLOAD' : '✓ Normal'}
            </div>
          </div>
          <div className="metric-card">
            <h3>Dropped Tasks</h3>
            <div className="metric-value">{health.droppedTPM.toFixed(1)} TPM</div>
            <div className="metric-label">Tasks dropped per minute</div>
          </div>
        </div>
      ) : (
        <p>Loading system health...</p>
      )}

      <div className="subsection">
        <h3>Clock Drift Statistics</h3>
        {clockDrift && clockDrift.totalMeasurements > 0 ? (
          <div className="stats-grid">
            <div><strong>Average:</strong> {clockDrift.averageDriftMs.toFixed(2)} ms</div>
            <div><strong>Min:</strong> {clockDrift.minDriftMs.toFixed(2)} ms</div>
            <div><strong>Max:</strong> {clockDrift.maxDriftMs.toFixed(2)} ms</div>
            <div><strong>Std Dev:</strong> {clockDrift.standardDeviationMs.toFixed(2)} ms</div>
          </div>
        ) : (
          <p>No clock drift data available</p>
        )}
      </div>

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
    </div>
  );
};

export default SystemHealthPanel;
