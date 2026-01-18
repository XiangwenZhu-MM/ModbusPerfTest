# SCADA Performance Monitor - Start All Applications
# This script starts both backend and frontend applications

Write-Host "=== SCADA Performance Monitor - Startup Script ===" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot

# Start Backend
Write-Host "Starting Backend API..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$projectRoot\backend'; Write-Host 'Backend API Server' -ForegroundColor Cyan; dotnet run"

# Wait a bit for backend to start
Start-Sleep -Seconds 2

# Start Frontend
Write-Host "Starting Frontend Dashboard..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$projectRoot\frontend'; Write-Host 'Frontend Dashboard' -ForegroundColor Cyan; npm start"

Write-Host ""
Write-Host "Applications starting..." -ForegroundColor Yellow
Write-Host "  - Backend API will be available at http://localhost:5274" -ForegroundColor Gray
Write-Host "  - Frontend Dashboard will open at http://localhost:3000" -ForegroundColor Gray
Write-Host ""
Write-Host "Close the PowerShell windows to stop the applications" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to exit this window..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
