# Mock installer PowerShell wrapper
param(
    [string]$Silent = ""
)

Write-Host "[MOCK] Starting installation of MockTimeoutInstaller"

if ($Silent -eq "/S" -or $Silent -eq "/SILENT" -or $Silent -eq "/QUIET") {
    Write-Host "[MOCK] Running in silent mode"
} else {
    Write-Host "[MOCK] Running in interactive mode"
}

if (150 -gt 0) {
    Write-Host "[MOCK] Simulating installation... (150 seconds)"
    Start-Sleep -Seconds 150
}

# Simulate registry write for successful installations
if (0 -eq 0) {
    $logPath = Join-Path $env:TEMP "MockTimeoutInstaller.bat.log"
    "Mock installation completed at $(Get-Date)" | Out-File -FilePath $logPath
    Write-Host "[MOCK] Installation completed successfully!"
    Write-Host "[MOCK] Log written to: $logPath"
} else {
    Write-Host "[MOCK] Installation failed with exit code: 0"
}

exit 0
