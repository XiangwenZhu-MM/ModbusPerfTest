import React, { useState, useEffect } from 'react';
import { ThreadPoolMetrics } from '../types';

const ThreadPoolPanel: React.FC = () => {
  const [metrics, setMetrics] = useState<ThreadPoolMetrics | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      try {
        const response = await fetch('http://localhost:5000/api/ThreadPool/metrics');
        if (!response.ok) throw new Error('Failed to fetch thread pool metrics');
        const data = await response.json();
        setMetrics(data);
        setError(null);
      } catch (error) {
        console.error('Error fetching thread pool metrics:', error);
        setError('Failed to load thread pool metrics');
      }
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 1000);
    return () => clearInterval(interval);
  }, []);

  if (error) {
    return (
      <div className="panel thread-pool-panel">
        <h2>ThreadPool Health Monitor</h2>
        <p className="error">{error}</p>
      </div>
    );
  }

  if (!metrics) {
    return (
      <div className="panel thread-pool-panel">
        <h2>ThreadPool Health Monitor</h2>
        <p>Loading thread pool metrics...</p>
      </div>
    );
  }

  const isPendingHigh = metrics.pendingWorkItems > 10;

  return (
    <div className="panel thread-pool-panel">
      <h2>ThreadPool Health Monitor</h2>
      
      <div className="metrics-grid">
        <div className="metric-card">
          <div className="metric-label">Active Worker Threads</div>
          <div className="metric-value">{metrics.workerThreads}</div>
          <div className="metric-description">Threads handling application logic</div>
        </div>

        <div className="metric-card">
          <div className="metric-label">IO Completion Threads</div>
          <div className="metric-value">{metrics.completionPortThreads}</div>
          <div className="metric-description">Network I/O callbacks (Modbus TCP)</div>
        </div>

        <div className={`metric-card ${isPendingHigh ? 'warning' : ''}`}>
          <div className="metric-label">Pending Work Items</div>
          <div className="metric-value">{metrics.pendingWorkItems}</div>
          <div className="metric-description">
            {isPendingHigh ? '⚠️ System lagging! Check for blocking code' : 'Should be 0'}
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-label">Thread Pool Range</div>
          <div className="metric-value">
            {metrics.minWorkerThreads} - {metrics.maxWorkerThreads}
          </div>
          <div className="metric-description">Min/Max worker thread limits</div>
        </div>
      </div>

      {isPendingHigh && (
        <div className="alert alert-warning">
          <strong>WARNING:</strong> Thread pool starvation detected! 
          Pending work items: {metrics.pendingWorkItems}. 
          Check for blocking calls (.Result, .Wait()) instead of async/await.
        </div>
      )}
    </div>
  );
};

export default ThreadPoolPanel;
