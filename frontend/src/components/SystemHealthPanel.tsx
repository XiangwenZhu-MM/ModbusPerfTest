import React, { useEffect, useState } from 'react';
import { SystemHealthMetric, HeartbeatMetrics as HeartbeatMetricsType, DataPointCountsResult, ThreadPoolMetrics, DeviceCountResult } from '../types';
import { api } from '../api';
import HeartbeatMetrics from './HeartbeatMetrics';
import HeartbeatWarnings from './HeartbeatWarnings';

const SystemHealthPanel: React.FC = () => {
  const [health, setHealth] = useState<SystemHealthMetric | null>(null);
  const [currentMetrics, setCurrentMetrics] = useState<HeartbeatMetricsType | null>(null);
  const [queueStats, setQueueStats] = useState<any>(null);
  const [dataPointCounts, setDataPointCounts] = useState<DataPointCountsResult | null>(null);
  const [threadPoolMetrics, setThreadPoolMetrics] = useState<ThreadPoolMetrics | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deviceCounts, setDeviceCounts] = useState<DeviceCountResult[]>([]);
  const [loadingDevices, setLoadingDevices] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [healthData, metricsData, queueData, threadData] = await Promise.all([
          api.getSystemHealth(),
          api.getHeartbeatMetrics(),
          api.getQueueStats(),
          api.getThreadPoolMetrics(),
        ]);
        setHealth(healthData);
        setCurrentMetrics(metricsData);
        setQueueStats(queueData);
        setThreadPoolMetrics(threadData);
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

  const handleOpenDeviceModal = async () => {
    setIsModalOpen(true);
    setLoadingDevices(true);
    try {
      const counts = await api.getDeviceDataCountsLastMinute();
      setDeviceCounts(counts);
    } catch (error) {
      console.error('Error fetching device counts:', error);
    } finally {
      setLoadingDevices(false);
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
          <div className={`metric-card ${(health.exceptionCount ?? 0) > 0 ? 'metric-alert' : ''}`}>
            <h3>Modbus Exceptions</h3>
            <div className="metric-value">{health.exceptionCount ?? 0}</div>
            <div className="metric-label">
              {(health.exceptionCount ?? 0) > 0 ? '⚠️ Errors detected' : '✓ No errors'}
            </div>
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
        <h3>ThreadPool Health Monitor</h3>
        {threadPoolMetrics ? (
          <div className="metrics-grid">
            <div className="metric-card">
              <h4>Worker Threads</h4>
              <div className="metric-value">{threadPoolMetrics.workerThreads}</div>
              <div className="metric-label">Active Workers</div>
            </div>
            <div className="metric-card">
              <h4>Completion Port Threads</h4>
              <div className="metric-value">{threadPoolMetrics.completionPortThreads}</div>
              <div className="metric-label">I/O Completion Threads</div>
            </div>
            <div className="metric-card">
              <h4>Pending Work Items</h4>
              <div className="metric-value">{threadPoolMetrics.pendingWorkItems}</div>
              <div className="metric-label">Items queued for execution</div>
            </div>
            <div className="metric-card">
              <h4>ThreadPool Range</h4>
              <div className="metric-value">{threadPoolMetrics.minWorkerThreads} - {threadPoolMetrics.maxWorkerThreads}</div>
              <div className="metric-label">Min - Max Workers</div>
            </div>
          </div>
        ) : (
          <p>Loading thread pool metrics...</p>
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
            <div className="clickable-range" onClick={handleOpenDeviceModal} title="Click to see per-device breakdown">
              <strong>Last Minute:</strong> {dataPointCounts.lastMinute.count.toLocaleString()} / {dataPointCounts.lastMinute.theoreticalCount.toLocaleString()}
              <div style={{ fontSize: '0.9em', color: dataPointCounts.lastMinute.missingRate > 1.0 ? '#f44336' : '#4caf50', margin: '5px 0' }}>
                Missing Rate: {dataPointCounts.lastMinute.missingRate.toFixed(3)}%
              </div>
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.lastMinute.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.lastMinute.endTime).toLocaleTimeString()}
              </div>
              <div style={{ fontSize: '0.7em', color: '#667eea', marginTop: '4px' }}>
                (Click for details)
              </div>
            </div>
            <div>
              <strong>Last 10 Minutes:</strong> {dataPointCounts.last10Minutes.count.toLocaleString()} / {dataPointCounts.last10Minutes.theoreticalCount.toLocaleString()}
              <div style={{ fontSize: '0.9em', color: dataPointCounts.last10Minutes.missingRate > 1.0 ? '#f44336' : '#4caf50', margin: '5px 0' }}>
                Missing Rate: {dataPointCounts.last10Minutes.missingRate.toFixed(3)}%
              </div>
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.last10Minutes.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.last10Minutes.endTime).toLocaleTimeString()}
              </div>
            </div>
            <div>
              <strong>Last Hour:</strong> {dataPointCounts.lastHour.count.toLocaleString()} / {dataPointCounts.lastHour.theoreticalCount.toLocaleString()}
              <div style={{ fontSize: '0.9em', color: dataPointCounts.lastHour.missingRate > 1.0 ? '#f44336' : '#4caf50', margin: '5px 0' }}>
                Missing Rate: {dataPointCounts.lastHour.missingRate.toFixed(3)}%
              </div>
              <div style={{ fontSize: '0.85em', color: '#666' }}>
                {new Date(dataPointCounts.lastHour.startTime).toLocaleTimeString()} - {new Date(dataPointCounts.lastHour.endTime).toLocaleTimeString()}
              </div>
            </div>
            <div>
              <strong>Last 2 Hours:</strong> {dataPointCounts.last2Hours.count.toLocaleString()} / {dataPointCounts.last2Hours.theoreticalCount.toLocaleString()}
              <div style={{ fontSize: '0.9em', color: dataPointCounts.last2Hours.missingRate > 1.0 ? '#f44336' : '#4caf50', margin: '5px 0' }}>
                Missing Rate: {dataPointCounts.last2Hours.missingRate.toFixed(3)}%
              </div>
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

      {isModalOpen && (
        <div className="modal-overlay" onClick={() => setIsModalOpen(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Device Data Breakdown (Last 1m)</h2>
              <button className="close-button" onClick={() => setIsModalOpen(false)}>&times;</button>
            </div>
            
            {loadingDevices ? (
              <p>Loading device counts...</p>
            ) : deviceCounts.length > 0 ? (
              <ul className="device-list">
                {deviceCounts.map(device => (
                  <li key={device.deviceName} className="device-item">
                    <span className="device-name">{device.deviceName}</span>
                    <span className="device-count">{device.count.toLocaleString()} pts</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p>No data points found for individual devices in the last minute.</p>
            )}
            
            <div style={{ marginTop: '20px', textAlign: 'right' }}>
              <button 
                onClick={() => setIsModalOpen(false)}
                style={{ padding: '8px 16px', cursor: 'pointer' }}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SystemHealthPanel;
