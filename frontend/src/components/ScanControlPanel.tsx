import React, { useState, useEffect } from 'react';
import { api } from '../api';
import './ScanControlPanel.css';

interface ScanStatus {
  isRunning: boolean;
  deviceCount: number;
}

interface ActiveConfig {
  useAsyncRead: boolean;
  useAsyncNModbus: boolean;
  allowConcurrentFrameReads: boolean;
  minWorkerThreads: number;
  dataStorageBackend: string;
}

const ScanControlPanel: React.FC = () => {
  const [status, setStatus] = useState<ScanStatus>({ isRunning: false, deviceCount: 0 });
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string>('');
  const [messageType, setMessageType] = useState<'success' | 'error' | ''>('');  const [activeConfig, setActiveConfig] = useState<ActiveConfig | null>(null);

  const fetchStatus = async () => {
    try {
      const data = await api.getScanningStatus();
      setStatus(data);
    } catch (error) {
      console.error('Error fetching scan status:', error);
    }
  };

  useEffect(() => {
    fetchStatus();
    const interval = setInterval(fetchStatus, 2000); // Poll every 2 seconds
    return () => clearInterval(interval);
  }, []);

  const handleStart = async () => {
    setLoading(true);
    setMessage('');
    try {
      const result = await api.startScanning();
      setMessage(result.message || 'Scanning started successfully');
      setMessageType('success');
      if (result.configuration) {
        setActiveConfig(result.configuration);
      }
      await fetchStatus();
    } catch (error: any) {
      setMessage(error.message || 'Failed to start scanning');
      setMessageType('error');
    } finally {
      setLoading(false);
    }
  };

  const handleStop = async () => {
    setLoading(true);
    setMessage('');
    try {
      const result = await api.stopScanning();
      setMessage(result.message || 'Scanning stopped successfully');
      setMessageType('success');
      await fetchStatus();
    } catch (error: any) {
      setMessage(error.message || 'Failed to stop scanning');
      setMessageType('error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="scan-control-panel">
      <h2>Scan Control</h2>
      
      <div className="status-section">
        <div className="status-item">
          <span className="label">Status:</span>
          <span className={`status-badge ${status.isRunning ? 'running' : 'stopped'}`}>
            {status.isRunning ? '● Running' : '○ Stopped'}
          </span>
        </div>
        <div className="status-item">
          <span className="label">Devices Configured:</span>
          <span className="value">{status.deviceCount}</span>
        </div>
      </div>

      <div className="control-buttons">
        <button
          className="btn btn-start"
          onClick={handleStart}
          disabled={loading || status.isRunning}
        >
          {loading && !status.isRunning ? 'Starting...' : 'Start Scanning'}
        </button>
        <button
          className="btn btn-stop"
          onClick={handleStop}
          disabled={loading || !status.isRunning}
        >
          {loading && status.isRunning ? 'Stopping...' : 'Stop Scanning'}
        </button>
      </div>

      {message && (
        <div className={`message ${messageType}`}>
          {message}
        </div>
      )}

      {activeConfig && (
        <div className="active-config">
          <h3>Active Configuration</h3>
          <div className="config-grid">
            <div className="config-item">
              <span className="config-label">UseAsyncRead:</span>
              <span className="config-value">{activeConfig.useAsyncRead ? 'True' : 'False'}</span>
            </div>
            <div className="config-item">
              <span className="config-label">UseAsyncNModbus:</span>
              <span className="config-value">{activeConfig.useAsyncNModbus ? 'True' : 'False'}</span>
            </div>
            <div className="config-item">
              <span className="config-label">AllowConcurrentFrameReads:</span>
              <span className="config-value">{activeConfig.allowConcurrentFrameReads ? 'True' : 'False'}</span>
            </div>
            <div className="config-item">
              <span className="config-label">Min Worker Threads:</span>
              <span className="config-value">{activeConfig.minWorkerThreads}</span>
            </div>
            <div className="config-item">
              <span className="config-label">Data Storage Backend:</span>
              <span className="config-value">{activeConfig.dataStorageBackend}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ScanControlPanel;
