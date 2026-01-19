# SCADA Performance Monitor - Stop All Applications
# This script stops all running backend and frontend processes

Write-Host "=== SCADA Performance Monitor - Shutdown Script ===" -ForegroundColor Cyan
Write-Host ""

# Stop Backend
Write-Host "Stopping Backend API..." -ForegroundColor Yellow
Get-Process -Name "ModbusPerfTest.Backend" -ErrorAction SilentlyContinue | Stop-Process -Force
if ($?) {
    Write-Host "  Backend stopped" -ForegroundColor Green
} else {
    Write-Host "  No backend process found" -ForegroundColor Gray
}

# Stop Frontend (node processes running react-scripts)
Write-Host "Stopping Frontend Dashboard..." -ForegroundColor Yellow
Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*react-scripts*" } | Stop-Process -Force
if ($?) {
    Write-Host "  Frontend stopped" -ForegroundColor Green
} else {
    Write-Host "  No frontend process found" -ForegroundColor Gray
}

Write-Host ""
Write-Host "All applications stopped" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
