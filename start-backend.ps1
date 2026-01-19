#!/usr/bin/env pwsh
# Start SCADA Performance Monitor Backend

Write-Host "=== SCADA Performance Monitor - Backend Startup ===" -ForegroundColor Cyan
Write-Host ""

# Stop existing backend process if running
Write-Host "Checking for existing backend process..." -ForegroundColor Yellow
$existingProcess = Get-Process -Name "ModbusPerfTest.Backend" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Host "Stopping existing backend process (PID: $($existingProcess.Id))..." -ForegroundColor Yellow
    Stop-Process -Name "ModbusPerfTest.Backend" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Write-Host "Existing process stopped." -ForegroundColor Green
}

# Navigate to backend directory and start
Write-Host "Starting backend server..." -ForegroundColor Yellow
Set-Location -Path "$PSScriptRoot\backend"

Write-Host ""
Write-Host "Backend will be available at: http://localhost:5000" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Gray
Write-Host ""

dotnet run
