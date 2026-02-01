import React, { useEffect, useState } from 'react';
import { DeviceLevelMetric } from '../types';
import { api } from '../api';

interface FrameMetrics {
  frameId: string;
  ipAddress: string;
  port: number;
  slaveId: number;
  frameIndex: number;
  startAddress: number;
  count: number;
  scanFrequencyMs: number;
  droppedCount: number;
  droppedTPM: number;
  latest: {
    queueDurationMs: number;
    deviceResponseTimeMs: number;
    actualSamplingIntervalMs: number;
    timestamp: string;
  };
  mean: {
    queueDurationMs: number;
    deviceResponseTimeMs: number;
    actualSamplingIntervalMs: number;
  };
  sampleCount: number;
}

const DeviceMetricsPanel: React.FC = () => {
  const [metrics, setMetrics] = useState<DeviceLevelMetric[]>([]);
  const [frameMetrics, setFrameMetrics] = useState<FrameMetrics[]>([]);
  const [latestMetric, setLatestMetric] = useState<DeviceLevelMetric | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      try {
        const data = await api.getDeviceMetrics(50);
        setMetrics(data);
        if (data.length > 0) {
          setLatestMetric(data[data.length - 1]);
        }

        // Group metrics by frame and calculate means
        const frameMap = new Map<string, DeviceLevelMetric[]>();
        data.forEach((m: DeviceLevelMetric) => {
          if (!frameMap.has(m.frameId)) {
            frameMap.set(m.frameId, []);
          }
          frameMap.get(m.frameId)!.push(m);
        });

        const aggregated: FrameMetrics[] = Array.from(frameMap.entries()).map(([frameId, frameData]) => {
          const latest = frameData[frameData.length - 1];
          const meanQueue = frameData.reduce((sum, m) => sum + m.queueDurationMs, 0) / frameData.length;
          const meanResponse = frameData.reduce((sum, m) => sum + m.deviceResponseTimeMs, 0) / frameData.length;
          const meanTotal = frameData.reduce((sum, m) => sum + m.actualSamplingIntervalMs, 0) / frameData.length;

          // Parse frameId to extract IP, port, slaveId, frameIndex (format: "IP:Port:SlaveId:StartAddress:FrameIndex")
          const parts = frameId.split(':');
          const ipAddress = parts[0];
          const port = parseInt(parts[1]);
          const slaveId = parseInt(parts[2]);
          const frameIndex = parseInt(parts[4] || '0');

          return {
            frameId,
            ipAddress,
            port,
            slaveId,
            frameIndex,
            startAddress: latest.startAddress,
            count: latest.count,
            scanFrequencyMs: latest.scanFrequencyMs,
            droppedCount: latest.droppedCount,
            droppedTPM: latest.droppedTPM,
            latest: {
              queueDurationMs: latest.queueDurationMs,
              deviceResponseTimeMs: latest.deviceResponseTimeMs,
              actualSamplingIntervalMs: latest.actualSamplingIntervalMs,
              timestamp: latest.timestamp
            },
            mean: {
              queueDurationMs: meanQueue,
              deviceResponseTimeMs: meanResponse,
              actualSamplingIntervalMs: meanTotal
            },
            sampleCount: frameData.length
          };
        });

        // Sort by IP address, port, slave ID, and frame index
        aggregated.sort((a, b) => {
          if (a.ipAddress !== b.ipAddress) {
            return a.ipAddress.localeCompare(b.ipAddress);
          }
          if (a.port !== b.port) {
            return a.port - b.port;
          }
          if (a.slaveId !== b.slaveId) {
            return a.slaveId - b.slaveId;
          }
          return a.frameIndex - b.frameIndex;
        });

        setFrameMetrics(aggregated);
      } catch (error) {
        console.error('Error fetching device metrics:', error);
      }
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 1000); // Update every second

    return () => clearInterval(interval);
  }, []);

  return (
    <div className="panel">
      <h2>Device Metrics</h2>
      {latestMetric ? (
        <div className="metrics-grid">
          <div className="metric-card">
            <h3>Queue Duration</h3>
            <div className="metric-value">{latestMetric.queueDurationMs.toFixed(2)} ms</div>
            <div className="metric-label">Time in queue</div>
          </div>
          <div className="metric-card">
            <h3>Response Time</h3>
            <div className="metric-value">{latestMetric.deviceResponseTimeMs.toFixed(2)} ms</div>
            <div className="metric-label">Network round-trip</div>
          </div>
          <div className="metric-card">
            <h3>Total Deviation</h3>
            <div className="metric-value">{latestMetric.actualSamplingIntervalMs.toFixed(2)} ms</div>
            <div className="metric-label">End-to-end time</div>
          </div>
        </div>
      ) : (
        <p>No metrics available</p>
      )}

      <div className="metrics-table">
        <h3>Frame Metrics (Latest & Mean)</h3>
        <table>
          <thead>
            <tr>
              <th rowSpan={2} style={{ backgroundColor: '#e8f4f8', verticalAlign: 'middle', color: '#000' }}>IP Address</th>
              <th rowSpan={2} style={{ backgroundColor: '#e8f4f8', verticalAlign: 'middle', color: '#000' }}>Port</th>
              <th rowSpan={2} style={{ backgroundColor: '#e8f4f8', verticalAlign: 'middle', color: '#000' }}>Slave</th>
              <th rowSpan={2} style={{ backgroundColor: '#e8f4f8', verticalAlign: 'middle', color: '#000' }}>Frame</th>
              <th rowSpan={2} style={{ backgroundColor: '#fff3cd', verticalAlign: 'middle', color: '#000' }}>Count</th>
              <th rowSpan={2} style={{ backgroundColor: '#fff3cd', verticalAlign: 'middle', color: '#000' }}>Freq</th>
              <th colSpan={3} style={{ backgroundColor: '#d1ecf1', textAlign: 'center', fontWeight: 'bold', color: '#000' }}>Latest (ms)</th>
              <th colSpan={3} style={{ backgroundColor: '#d4edda', textAlign: 'center', fontWeight: 'bold', color: '#000' }}>Mean (ms)</th>
              <th rowSpan={2} style={{ backgroundColor: '#f8d7da', verticalAlign: 'middle', color: '#000' }}>Util %</th>
              <th colSpan={2} style={{ backgroundColor: '#f5c6cb', textAlign: 'center', fontWeight: 'bold', color: '#000' }}>Dropped</th>
            </tr>
            <tr>
              <th style={{ backgroundColor: '#d1ecf1', color: '#000' }}>Queue</th>
              <th style={{ backgroundColor: '#d1ecf1', color: '#000' }}>Response</th>
              <th style={{ backgroundColor: '#d1ecf1', color: '#000' }}>Total</th>
              <th style={{ backgroundColor: '#d4edda', color: '#000' }}>Queue</th>
              <th style={{ backgroundColor: '#d4edda', color: '#000' }}>Response</th>
              <th style={{ backgroundColor: '#d4edda', color: '#000' }}>Total</th>
              <th style={{ backgroundColor: '#f5c6cb', color: '#000' }}>Total</th>
              <th style={{ backgroundColor: '#f5c6cb', color: '#000' }}>TPM</th>
            </tr>
          </thead>
          <tbody>
            {(() => {
              const deviceGroups = new Map<string, FrameMetrics[]>();
              frameMetrics.forEach(fm => {
                const deviceKey = `${fm.ipAddress}:${fm.port}:${fm.slaveId}`;
                if (!deviceGroups.has(deviceKey)) {
                  deviceGroups.set(deviceKey, []);
                }
                deviceGroups.get(deviceKey)!.push(fm);
              });

              const rows: React.ReactElement[] = [];
              deviceGroups.forEach((frames, deviceKey) => {
                // Add individual frame rows
                frames.forEach(fm => {
                  const frameUtilization = (fm.mean.actualSamplingIntervalMs / fm.scanFrequencyMs) * 100;
                  rows.push(
                    <tr key={fm.frameId}>
                      <td>{fm.ipAddress}</td>
                      <td>{fm.port}</td>
                      <td>{fm.slaveId}</td>
                      <td>{fm.frameIndex}</td>
                      <td>{fm.count}</td>
                      <td>{fm.scanFrequencyMs}ms</td>
                      <td>{fm.latest.queueDurationMs.toFixed(0)}</td>
                      <td>{fm.latest.deviceResponseTimeMs.toFixed(0)}</td>
                      <td style={{ color: fm.latest.actualSamplingIntervalMs > fm.scanFrequencyMs ? 'red' : 'inherit' }}>
                        {fm.latest.actualSamplingIntervalMs.toFixed(0)}
                      </td>
                      <td><strong>{fm.mean.queueDurationMs.toFixed(0)}</strong></td>
                      <td><strong>{fm.mean.deviceResponseTimeMs.toFixed(0)}</strong></td>
                      <td style={{ color: fm.mean.actualSamplingIntervalMs > fm.scanFrequencyMs ? 'red' : 'inherit' }}>
                        <strong>{fm.mean.actualSamplingIntervalMs.toFixed(0)}</strong>
                      </td>
                      <td style={{ color: frameUtilization >= 100 ? 'red' : frameUtilization >= 80 ? 'orange' : 'inherit' }}>
                        <strong>{frameUtilization.toFixed(1)}%</strong>
                      </td>
                      <td>{fm.droppedCount}</td>
                      <td style={{ color: fm.droppedTPM > 0 ? 'red' : 'inherit' }}>{fm.droppedTPM.toFixed(1)}</td>
                    </tr>
                  );
                });

                // Add device summary row for all devices
                const sumLatestQueue = frames.reduce((sum, fm) => sum + fm.latest.queueDurationMs, 0);
                const sumLatestResponse = frames.reduce((sum, fm) => sum + fm.latest.deviceResponseTimeMs, 0);
                const sumLatestTotal = frames.reduce((sum, fm) => sum + fm.latest.actualSamplingIntervalMs, 0);
                const sumMeanQueue = frames.reduce((sum, fm) => sum + fm.mean.queueDurationMs, 0);
                const sumMeanResponse = frames.reduce((sum, fm) => sum + fm.mean.deviceResponseTimeMs, 0);
                const sumMeanTotal = frames.reduce((sum, fm) => sum + fm.mean.actualSamplingIntervalMs, 0);
                const totalDropped = frames.reduce((sum, fm) => sum + fm.droppedCount, 0);
                const totalDroppedTPM = frames.reduce((sum, fm) => sum + fm.droppedTPM, 0);

                // Device-level metrics
                const maxQueueDuration = Math.max(...frames.map(fm => fm.mean.queueDurationMs));
                const avgDeviceResponseTime = frames.reduce((sum, fm) => sum + fm.mean.deviceResponseTimeMs, 0) / frames.length;
                const channelUtilization = frames.reduce((sum, fm) => sum + ((fm.mean.deviceResponseTimeMs / fm.scanFrequencyMs) * 100), 0);

                // Device-level metrics row
                rows.push(
                  <tr key={`${deviceKey}-metrics`} style={{ backgroundColor: '#f5f5f5', fontWeight: 'bold', color: '#333' }}>
                    <td>{frames[0].ipAddress}</td>
                    <td>{frames[0].port}</td>
                    <td>{frames[0].slaveId}</td>
                    <td colSpan={2} style={{ fontSize: '0.9em' }}>
                      <strong>DEVICE ({frames.length} frame{frames.length > 1 ? 's' : ''})</strong>
                    </td>
                    <td colSpan={2} title="Worst-case delay for any tag">
                      Max Queue: <strong>{maxQueueDuration.toFixed(0)}ms</strong>
                    </td>
                    <td colSpan={2} title="Average physical speed of network/PLC">
                      Avg Resp: <strong>{avgDeviceResponseTime.toFixed(0)}ms</strong>
                    </td>
                    <td colSpan={2} title="Sum of frame utilizations - channel saturation" style={{ color: channelUtilization >= 100 ? 'red' : channelUtilization >= 80 ? 'orange' : 'inherit' }}>
                      Ch. Util: <strong>{channelUtilization.toFixed(1)}%</strong>
                    </td>
                    <td colSpan={2} title="Total dropped tasks across all frames" style={{ color: totalDropped > 0 ? 'red' : 'inherit' }}>
                      Total Drops: <strong>{totalDropped}</strong>
                    </td>
                  </tr>
                );
              });

              return rows;
            })()}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default DeviceMetricsPanel;
