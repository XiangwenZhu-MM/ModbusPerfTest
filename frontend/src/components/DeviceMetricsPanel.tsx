import React, { useEffect, useState } from 'react';
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
  const [frameMetrics, setFrameMetrics] = useState<FrameMetrics[]>([]);

  useEffect(() => {
    const fetchMetrics = async () => {
      try {
        // Use the new frames endpoint that includes all configured frames
        const frames = await api.getAllFrames();
        
        // Transform to our display format
        const aggregated: FrameMetrics[] = frames.map((frame: any) => {
          const hasMetrics = frame.hasMetrics && frame.latestMetric;
          
          return {
            frameId: frame.frameId,
            ipAddress: frame.ipAddress,
            port: frame.port,
            slaveId: frame.slaveId,
            frameIndex: frame.frameIndex,
            startAddress: frame.startAddress,
            count: frame.count,
            scanFrequencyMs: frame.scanFrequencyMs,
            droppedCount: frame.droppedCount,
            droppedTPM: frame.droppedTPM,
            latest: hasMetrics ? {
              queueDurationMs: frame.latestMetric.queueDurationMs,
              deviceResponseTimeMs: frame.latestMetric.deviceResponseTimeMs,
              actualSamplingIntervalMs: frame.latestMetric.actualSamplingIntervalMs,
              timestamp: frame.latestMetric.timestamp
            } : {
              queueDurationMs: 0,
              deviceResponseTimeMs: 0,
              actualSamplingIntervalMs: 0,
              timestamp: new Date().toISOString()
            },
            mean: frame.meanMetrics ? {
              queueDurationMs: frame.meanMetrics.queueDurationMs,
              deviceResponseTimeMs: frame.meanMetrics.deviceResponseTimeMs,
              actualSamplingIntervalMs: frame.meanMetrics.actualSamplingIntervalMs
            } : {
              queueDurationMs: 0,
              deviceResponseTimeMs: 0,
              actualSamplingIntervalMs: 0
            },
            sampleCount: frame.metricsCount || 0
          };
        });

        // Sort by IP (natural), port, slaveId, frameIndex
        aggregated.sort((a, b) => {
          const ipCompare = a.ipAddress.localeCompare(b.ipAddress, undefined, { numeric: true, sensitivity: 'base' });
          if (ipCompare !== 0) return ipCompare;
          if (a.port !== b.port) return a.port - b.port;
          if (a.slaveId !== b.slaveId) return a.slaveId - b.slaveId;
          return a.frameIndex - b.frameIndex;
        });

        setFrameMetrics(aggregated);
      } catch (error) {
        console.error('Error fetching device metrics:', error);
      }
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 1000);

    return () => clearInterval(interval);
  }, []);

  return (
    <div className="panel">
      <h2>Device-Level Metrics</h2>

      <div className="metrics-table" style={{ maxHeight: '1500px', overflowY: 'auto' }}>
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
              <th rowSpan={2} style={{ backgroundColor: '#f8d7da', verticalAlign: 'middle', color: '#000' }}>Channel Util %</th>
              <th colSpan={2} style={{ backgroundColor: '#f5c6cb', textAlign: 'center', fontWeight: 'bold', color: '#000' }}>Dropped</th>
            </tr>
            <tr>
              <th style={{ backgroundColor: '#d1ecf1', color: '#000' }}>Queue Duration</th>
              <th style={{ backgroundColor: '#d1ecf1', color: '#000' }}>Device Resp Time</th>
              <th style={{ backgroundColor: '#d1ecf1', color: '#000' }}>Total</th>
              <th style={{ backgroundColor: '#d4edda', color: '#000' }}>Queue Duration</th>
              <th style={{ backgroundColor: '#d4edda', color: '#000' }}>Device Resp Time</th>
              <th style={{ backgroundColor: '#d4edda', color: '#000' }}>Total</th>
              <th style={{ backgroundColor: '#f5c6cb', color: '#000' }}>Total</th>
              <th style={{ backgroundColor: '#f5c6cb', color: '#000' }}>Per Minute</th>
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
                  const frameUtilization = (fm.mean.deviceResponseTimeMs / fm.scanFrequencyMs) * 100;
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
                const maxMeanQueue = Math.max(...frames.map(fm => fm.mean.queueDurationMs));
                const avgMeanResponse = frames.reduce((sum, fm) => sum + fm.mean.deviceResponseTimeMs, 0) / frames.length;
                const totalUtilization = frames.reduce((sum, fm) => sum + (fm.mean.deviceResponseTimeMs / fm.scanFrequencyMs) * 100, 0);
                const totalDropped = frames.reduce((sum, fm) => sum + fm.droppedCount, 0);
                const totalDroppedTPM = frames.reduce((sum, fm) => sum + fm.droppedTPM, 0);

                rows.push(
                  <tr key={`${deviceKey}-summary`} style={{ backgroundColor: '#f8f9fa', borderTop: '2px solid #000', borderBottom: '2px solid #000', color: '#000' }}>
                    <td colSpan={3}><strong>Device Total</strong></td>
                    <td><strong>{frames.length} Frames</strong></td>
                    <td colSpan={2}></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td><strong>{maxMeanQueue.toFixed(0)}</strong></td>
                    <td><strong>{avgMeanResponse.toFixed(0)}</strong></td>
                    <td></td>
                    <td style={{ color: totalUtilization >= 100 ? 'red' : totalUtilization >= 80 ? 'orange' : '#000' }}>
                      <strong>{totalUtilization.toFixed(1)}%</strong>
                    </td>
                    <td><strong>{totalDropped}</strong></td>
                    <td style={{ color: totalDroppedTPM > 0 ? 'red' : '#000' }}><strong>{totalDroppedTPM.toFixed(1)}</strong></td>
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
