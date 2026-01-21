import React, { useState, useEffect } from 'react';
import './App.css';
import ScanControlPanel from './components/ScanControlPanel';
import ConfigurationPanel from './components/ConfigurationPanel';
import DeviceMetricsPanel from './components/DeviceMetricsPanel';
import SystemHealthPanel from './components/SystemHealthPanel';
import { api } from './api';
// import DataQualityPanel from './components/DataQualityPanel';

function App() {
  const [isScanning, setIsScanning] = useState(false);

  useEffect(() => {
    const fetchScanStatus = async () => {
      try {
        const status = await api.getScanningStatus();
        setIsScanning(status.isRunning);
      } catch (error) {
        console.error('Error fetching scan status:', error);
      }
    };

    fetchScanStatus();
    const interval = setInterval(fetchScanStatus, 2000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="App">
      <header className="App-header">
        <h1>SCADA Performance Monitor MVP</h1>
        <p>Real-time monitoring of Modbus TCP devices</p>
      </header>
      
      <main className="dashboard">
        <ScanControlPanel />
        <ConfigurationPanel isScanning={isScanning} />
        <DeviceMetricsPanel />
        <SystemHealthPanel />
        {/* <DataQualityPanel /> */}
      </main>
      
      <footer className="App-footer">
        <p>© 2026 ModbusPerfTest - High-Performance SCADA Monitoring</p>
      </footer>
    </div>
  );
}

export default App;
