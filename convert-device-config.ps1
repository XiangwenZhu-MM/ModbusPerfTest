#!/usr/bin/env pwsh
# Convert device-config.xml to device-config.json
# Each <port> tag represents a Modbus TCP device
# Generates 2 frames per device with fixed configuration

param(
    [string]$InputXml = "device-config.xml",
    [string]$OutputJson = "device-config.json"
)

Write-Host "Converting $InputXml to $OutputJson..." -ForegroundColor Cyan

# Load XML
[xml]$xml = Get-Content $InputXml

# Extract ports
$ports = $xml.project.ports.port

Write-Host "Found $($ports.Count) port(s) in XML" -ForegroundColor Yellow

# Build JSON structure
$devices = @()

foreach ($port in $ports) {
    # Extract device reference to use as slaveId
    $deviceRef = $port.deviceref.deviceref
    if ($deviceRef) {
        # Use the numeric value from deviceref as slaveId
        $slaveId = [int]$deviceRef.'#text'
    } else {
        # Default to 1 if no deviceref found
        $slaveId = 1
    }

    $device = [PSCustomObject]@{
        name = $port.name
        ipAddress = $port.host
        port = [int]$port.port
        slaveId = $slaveId
        frames = @(
            [PSCustomObject]@{
                name = "Frame1"
                startAddress = 400001
                count = 50
                scanFrequencyMs = 1000
            },
            [PSCustomObject]@{
                name = "Frame2"
                startAddress = 400051
                count = 50
                scanFrequencyMs = 1000
            }
        )
    }

    $devices += $device

    Write-Host "  - Port: $($device.port), Host: $($device.ipAddress), SlaveId: $($device.slaveId), Name: $($port.name)" -ForegroundColor Gray
}

# Create final object
$config = [PSCustomObject]@{
    devices = $devices
}

# Convert to JSON and save
$jsonOutput = $config | ConvertTo-Json -Depth 10
$jsonOutput | Set-Content -Path $OutputJson -Encoding UTF8

Write-Host "`nConversion complete!" -ForegroundColor Green
Write-Host "Generated $($devices.Count) device(s) with 2 frames each" -ForegroundColor Green
Write-Host "Output: $OutputJson" -ForegroundColor Cyan
