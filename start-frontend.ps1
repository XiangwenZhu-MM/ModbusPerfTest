# Start Frontend Dashboard Only
# This script starts the React frontend application

Write-Host "=== Starting Frontend Dashboard ===" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot

Set-Location "$projectRoot\frontend"

Write-Host "Starting React development server..." -ForegroundColor Green
npm start
