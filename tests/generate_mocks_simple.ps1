# Simplified Mock Installer Generator for Testing
param(
    [string]$OutputPath = "./tests/mock_installers"
)

Write-Host "=== Mock Installer Generator (Simplified) ===" -ForegroundColor Cyan

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Mock installer definitions
$mockInstallers = @(
    @{Name = "MockChrome.bat"; ExitCode = 0; Delay = 2},
    @{Name = "MockFirefox.bat"; ExitCode = 0; Delay = 3},
    @{Name = "MockVSCode.bat"; ExitCode = 0; Delay = 5},
    @{Name = "Mock7Zip.bat"; ExitCode = 0; Delay = 1},
    @{Name = "MockFailingInstaller.bat"; ExitCode = 1; Delay = 1},
    @{Name = "MockTimeoutInstaller.bat"; ExitCode = 0; Delay = 150},
    @{Name = "MockSilentInstaller.bat"; ExitCode = 0; Delay = 0},
    @{Name = "MockJetBrains.bat"; ExitCode = 0; Delay = 4}
)

foreach ($mock in $mockInstallers) {
    $outputFile = Join-Path $OutputPath $mock.Name
    
    # Create batch file content
    $content = "@echo off`r`n"
    $content += "echo [MOCK] Starting installation of $($mock.Name)`r`n"
    
    if ($mock.Delay -gt 0) {
        $content += "echo [MOCK] Simulating installation... ($($mock.Delay) seconds)`r`n"
        $content += "timeout /t $($mock.Delay) /nobreak >nul 2>&1`r`n"
    }
    
    if ($mock.ExitCode -eq 0) {
        $content += "echo [MOCK] Installation completed successfully!`r`n"
    } else {
        $content += "echo [MOCK] Installation failed with exit code: $($mock.ExitCode)`r`n"
    }
    
    $content += "exit /b $($mock.ExitCode)`r`n"
    
    # Write file
    $content | Out-File -FilePath $outputFile -Encoding ASCII
    Write-Host "Created: $($mock.Name)" -ForegroundColor Green
}

# Also create executable versions using PowerShell wrapper
foreach ($mock in $mockInstallers) {
    $exeName = $mock.Name -replace '\.bat$', '.exe.ps1'
    $outputFile = Join-Path $OutputPath $exeName
    
    $psContent = @"
# Mock installer PowerShell wrapper
param(
    [string]`$Silent = ""
)

Write-Host "[MOCK] Starting installation of $($mock.Name -replace '\.bat$', '')"

if (`$Silent -eq "/S" -or `$Silent -eq "/SILENT" -or `$Silent -eq "/QUIET") {
    Write-Host "[MOCK] Running in silent mode"
} else {
    Write-Host "[MOCK] Running in interactive mode"
}

if ($($mock.Delay) -gt 0) {
    Write-Host "[MOCK] Simulating installation... ($($mock.Delay) seconds)"
    Start-Sleep -Seconds $($mock.Delay)
}

# Simulate registry write for successful installations
if ($($mock.ExitCode) -eq 0) {
    `$logPath = Join-Path `$env:TEMP "$($mock.Name).log"
    "Mock installation completed at `$(Get-Date)" | Out-File -FilePath `$logPath
    Write-Host "[MOCK] Installation completed successfully!"
    Write-Host "[MOCK] Log written to: `$logPath"
} else {
    Write-Host "[MOCK] Installation failed with exit code: $($mock.ExitCode)"
}

exit $($mock.ExitCode)
"@
    
    $psContent | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host "Created PowerShell mock: $exeName" -ForegroundColor Cyan
}

Write-Host "`nMock installers generated successfully!" -ForegroundColor Green
Write-Host "Location: $OutputPath" -ForegroundColor Gray
Write-Host "`nYou can now run integration tests with these mock installers." -ForegroundColor Yellow
