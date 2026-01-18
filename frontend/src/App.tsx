import React from 'react';
import './App.css';
import DeviceMetricsPanel from './components/DeviceMetricsPanel';
import SystemHealthPanel from './components/SystemHealthPanel';
import DataQualityPanel from './components/DataQualityPanel';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <h1>SCADA Performance Monitor MVP</h1>
        <p>Real-time monitoring of Modbus TCP devices</p>
      </header>
      
      <main className="dashboard">
        <DeviceMetricsPanel />
        <SystemHealthPanel />
        <DataQualityPanel />
      </main>
      
      <footer className="App-footer">
        <p>© 2026 ModbusPerfTest - High-Performance SCADA Monitoring</p>
      </footer>
    </div>
  );
}

export default App;
