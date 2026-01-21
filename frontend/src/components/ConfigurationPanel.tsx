import React, { useState, useEffect } from 'react';
import { api } from '../api';
import { RuntimeConfig } from '../config.types';
import './ConfigurationPanel.css';

interface ConfigurationPanelProps {
  isScanning: boolean;
}

const ConfigurationPanel: React.FC<ConfigurationPanelProps> = ({ isScanning }) => {
  const [config, setConfig] = useState<RuntimeConfig>({
    useAsyncRead: false,
    useAsyncNModbus: false,
    allowConcurrentFrameReads: false,
    dataStorageBackend: 'SQLite',
    minWorkerThreads: 22
  });
  
  const [originalConfig, setOriginalConfig] = useState<RuntimeConfig | null>(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string>('');
  const [messageType, setMessageType] = useState<'success' | 'error' | 'warning' | ''>('');

  const fetchConfiguration = async () => {
    try {
      const data = await api.getConfiguration();
      setConfig(data);
      setOriginalConfig(data);
    } catch (error) {
      console.error('Error fetching configuration:', error);
    }
  };

  useEffect(() => {
    fetchConfiguration();
  }, []);

  const hasChanges = () => {
    if (!originalConfig) return false;
    return JSON.stringify(config) !== JSON.stringify(originalConfig);
  };

  const handleApply = async () => {
    setLoading(true);
    setMessage('');
    try {
      const result = await api.applyConfiguration(config);
      setMessage(result.message || 'Configuration applied successfully');
      setMessageType(result.requiresRestart ? 'warning' : 'success');
      setOriginalConfig(config);
    } catch (error: any) {
      setMessage(error.message || 'Failed to apply configuration');
      setMessageType('error');
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    if (originalConfig) {
      setConfig(originalConfig);
      setMessage('');
    }
  };

  return (
    <div className="configuration-panel">
      <h2>Runtime Configuration</h2>
      
      <div className="config-grid">
        <div className="config-item">
          <div className="config-label">Use Async Read</div>
          <div className="checkbox-container">
            <input
              type="checkbox"
              id="useAsyncRead"
              checked={config.useAsyncRead}
              onChange={(e) => setConfig({ ...config, useAsyncRead: e.target.checked })}
              disabled={isScanning}
            />
            <label htmlFor="useAsyncRead">Use async Modbus read operations (ReadHoldingRegistersAsync)</label>
          </div>
        </div>

        <div className="config-item">
          <div className="config-label">Use Async NModbus</div>
          <div className="checkbox-container">
            <input
              type="checkbox"
              id="useAsyncNModbus"
              checked={config.useAsyncNModbus}
              onChange={(e) => setConfig({ ...config, useAsyncNModbus: e.target.checked })}
              disabled={isScanning}
            />
            <label htmlFor="useAsyncNModbus">Use NModbusAsync library (wolf8196 fork) instead of standard NModbus</label>
          </div>
        </div>

        <div className="config-item">
          <div className="config-label">Allow Concurrent Frame Reads</div>
          <div className="checkbox-container">
            <input
              type="checkbox"
              id="allowConcurrentFrameReads"
              checked={config.allowConcurrentFrameReads}
              onChange={(e) => setConfig({ ...config, allowConcurrentFrameReads: e.target.checked })}
              disabled={isScanning}
            />
            <label htmlFor="allowConcurrentFrameReads">Allow multiple frames to be read concurrently from the same device</label>
          </div>
        </div>

        <div className="config-item">
          <div className="config-label">Data Storage Backend</div>
          <select
            value={config.dataStorageBackend}
            onChange={(e) => setConfig({ ...config, dataStorageBackend: e.target.value })}
            disabled={isScanning}
            className="config-select"
          >
            <option value="SQLite">SQLite</option>
            <option value="InfluxDB">InfluxDB</option>
          </select>
          <p className="config-description">Database backend for storing data points</p>
        </div>

        <div className="config-item">
          <div className="config-label">Min Worker Threads</div>
          <input
            type="number"
            value={config.minWorkerThreads}
            onChange={(e) => setConfig({ ...config, minWorkerThreads: parseInt(e.target.value) || 1 })}
            disabled={isScanning}
            className="config-input"
            min="1"
            max="1000"
          />
          <p className="config-description">Minimum number of worker threads in the thread pool (1-1000)</p>
        </div>
      </div>

      <div className="config-actions">
        <button
          className="btn btn-apply"
          onClick={handleApply}
          disabled={loading || isScanning || !hasChanges()}
          title={isScanning ? "Stop scanning before applying configuration" : ""}
        >
          {loading ? 'Applying...' : 'Apply Configuration'}
        </button>
        <button
          className="btn btn-reset"
          onClick={handleReset}
          disabled={loading || !hasChanges()}
        >
          Reset
        </button>
      </div>

      {isScanning && hasChanges() && (
        <div className="message warning">
          ⚠ Stop scanning to apply configuration changes
        </div>
      )}

      {message && (
        <div className={`message ${messageType}`}>
          {messageType === 'warning' && '⚠ '}
          {message}
        </div>
      )}
    </div>
  );
};

export default ConfigurationPanel;
