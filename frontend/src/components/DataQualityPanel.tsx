import React, { useEffect, useState } from 'react';
import { DataQualitySummary, DataQualityState } from '../types';
import { api } from '../api';

const DataQualityPanel: React.FC = () => {
  const [summary, setSummary] = useState<DataQualitySummary | null>(null);
  const [dataPoints, setDataPoints] = useState<DataQualityState[]>([]);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [summaryData, pointsData] = await Promise.all([
          api.getDataQualitySummary(),
          api.getAllDataPoints(),
        ]);
        setSummary(summaryData);
        setDataPoints(pointsData);
      } catch (error) {
        console.error('Error fetching data quality:', error);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 2000); // Update every 2 seconds

    return () => clearInterval(interval);
  }, []);

  const getQualityClass = (quality: string) => {
    switch (quality) {
      case 'Good': return 'quality-good';
      case 'Stale': return 'quality-stale';
      case 'Uncertain': return 'quality-uncertain';
      default: return '';
    }
  };

  return (
    <div className="panel">
      <h2>Data Quality & Staleness</h2>
      
      {summary ? (
        <div className="metrics-grid">
          <div className="metric-card quality-good">
            <h3>Good</h3>
            <div className="metric-value">{summary.goodCount}</div>
            <div className="metric-label">Current & valid</div>
          </div>
          <div className="metric-card quality-stale">
            <h3>Stale</h3>
            <div className="metric-value">{summary.staleCount}</div>
            <div className="metric-label">Outdated data</div>
          </div>
          <div className="metric-card quality-uncertain">
            <h3>Uncertain</h3>
            <div className="metric-value">{summary.uncertainCount}</div>
            <div className="metric-label">Quality unknown</div>
          </div>
          <div className="metric-card">
            <h3>Total</h3>
            <div className="metric-value">{summary.totalDataPoints}</div>
            <div className="metric-label">Data points</div>
          </div>
        </div>
      ) : (
        <p>Loading data quality...</p>
      )}

      <div className="datapoints-table">
        <h3>Data Points Status ({dataPoints.length} total)</h3>
        <table>
          <thead>
            <tr>
              <th>Data Point ID</th>
              <th>Quality</th>
              <th>Last Value</th>
              <th>Last Update</th>
              <th>Threshold (ms)</th>
            </tr>
          </thead>
          <tbody>
            {dataPoints.slice(0, 20).map((dp) => (
              <tr key={dp.dataPointId} className={getQualityClass(dp.quality)}>
                <td>{dp.dataPointId}</td>
                <td>
                  <span className={`quality-badge ${getQualityClass(dp.quality)}`}>
                    {dp.quality}
                  </span>
                </td>
                <td>{dp.lastKnownValue}</td>
                <td>
                  {dp.lastSuccessTimestamp 
                    ? new Date(dp.lastSuccessTimestamp).toLocaleTimeString()
                    : 'Never'}
                </td>
                <td>{dp.stalenessThresholdMs}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default DataQualityPanel;
