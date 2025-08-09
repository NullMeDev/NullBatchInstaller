# Mock installer PowerShell wrapper
param(
    [string]$Silent = ""
)

Write-Host "[MOCK] Starting installation of MockChrome"

if ($Silent -eq "/S" -or $Silent -eq "/SILENT" -or $Silent -eq "/QUIET") {
    Write-Host "[MOCK] Running in silent mode"
} else {
    Write-Host "[MOCK] Running in interactive mode"
}

if (2 -gt 0) {
    Write-Host "[MOCK] Simulating installation... (2 seconds)"
    Start-Sleep -Seconds 2
}

# Simulate registry write for successful installations
if (0 -eq 0) {
    $logPath = Join-Path $env:TEMP "MockChrome.bat.log"
    "Mock installation completed at $(Get-Date)" | Out-File -FilePath $logPath
    Write-Host "[MOCK] Installation completed successfully!"
    Write-Host "[MOCK] Log written to: $logPath"
} else {
    Write-Host "[MOCK] Installation failed with exit code: 0"
}

exit 0
