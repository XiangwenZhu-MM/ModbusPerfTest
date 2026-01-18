import React, { useState, useEffect } from 'react';
import { DriftEvent } from '../types';
import { api } from '../api';
import './HeartbeatWarnings.css';

const HeartbeatWarnings: React.FC = () => {
  const [warnings, setWarnings] = useState<DriftEvent[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Fetch warnings immediately
    fetchWarnings();

    // Poll every 2 seconds for new warnings
    const interval = setInterval(fetchWarnings, 2000);

    return () => clearInterval(interval);
  }, []);

  const fetchWarnings = async () => {
    try {
      const data = await api.getHeartbeatWarnings();
      setWarnings(data);
      setError(null);
    } catch (err) {
      console.error('Failed to fetch heartbeat warnings:', err);
      setError('Failed to load warnings');
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleString();
  };

  const getWarningTypeLabel = (eventType: string) => {
    return eventType === 'PERFORMANCE_DEGRADED' ? 'Internal Latency' : 'System Drift';
  };

  const getWarningClass = (eventType: string) => {
    return eventType === 'PERFORMANCE_DEGRADED' ? 'warning-latency' : 'warning-drift';
  };

  if (error) {
    return (
      <div className="heartbeat-warnings">
        <h3>Heartbeat Warnings</h3>
        <div className="error-message">{error}</div>
      </div>
    );
  }

  return (
    <div className="heartbeat-warnings">
      <h3>Heartbeat Warnings</h3>
      {warnings.length === 0 ? (
        <div className="no-warnings">No warnings detected - system healthy</div>
      ) : (
        <div className="warnings-list">
          {warnings.map((warning, index) => (
            <div key={index} className={`warning-item ${getWarningClass(warning.eventType)}`}>
              <div className="warning-header">
                <span className="warning-type">{getWarningTypeLabel(warning.eventType)}</span>
                <span className="warning-time">{formatTimestamp(warning.timestamp)}</span>
              </div>
              <div className="warning-message">{warning.message}</div>
              <div className="warning-details">
                <span>Deviation: {warning.deviationMs.toFixed(1)}ms</span>
                <span>Expected: {warning.expectedIntervalMs}ms</span>
                <span>Actual: {warning.monoElapsedMs}ms</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default HeartbeatWarnings;
