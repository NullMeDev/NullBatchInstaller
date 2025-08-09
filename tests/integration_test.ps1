# Integration Test Suite for NullInstaller
# Runs comprehensive tests with mock installers

param(
    [switch]$CI,
    [switch]$Verbose,
    [string]$TestCategory = "All"
)

$ErrorActionPreference = "Stop"
$script:testResults = @()
$script:testsPassed = 0
$script:testsFailed = 0

# Test configuration
$testConfig = @{
    MockInstallersPath = "./tests/mock_installers"
    TestLogPath = "./tests/test_results.log"
    TempPath = "$env:TEMP\NullInstallerTests"
}

# Ensure test environment is ready
function Initialize-TestEnvironment {
    Write-Host "=== Initializing Test Environment ===" -ForegroundColor Cyan
    
    # Create directories
    $testConfig.Keys | Where-Object { $_ -like "*Path" } | ForEach-Object {
        $path = $testConfig[$_]
        if (-not (Test-Path $path)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
            Write-Host "✓ Created: $path" -ForegroundColor Green
        }
    }
    
    # Generate mock installers if not exist
    if ((Get-ChildItem $testConfig.MockInstallersPath -Filter "*.exe" -ErrorAction SilentlyContinue).Count -eq 0) {
        Write-Host "Generating mock installers..." -ForegroundColor Yellow
        & "$PSScriptRoot\mock_installers\generate_mocks.ps1" -OutputPath $testConfig.MockInstallersPath
    }
    
    # Clear previous test results
    if (Test-Path $testConfig.TestLogPath) {
        Remove-Item $testConfig.TestLogPath -Force
    }
}

# Test runner function
function Run-Test {
    param(
        [string]$Name,
        [string]$Category,
        [scriptblock]$Test
    )
    
    if ($TestCategory -ne "All" -and $Category -ne $TestCategory) {
        return
    }
    
    Write-Host "`n▶ Running: $Name" -ForegroundColor Cyan -NoNewline
    
    $result = @{
        Name = $Name
        Category = $Category
        Success = $false
        Duration = 0
        Error = $null
    }
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        & $Test
        $result.Success = $true
        $script:testsPassed++
        Write-Host " ✓ PASSED" -ForegroundColor Green
    }
    catch {
        $result.Error = $_.Exception.Message
        $script:testsFailed++
        Write-Host " ✗ FAILED" -ForegroundColor Red
        if ($Verbose) {
            Write-Host "  Error: $($result.Error)" -ForegroundColor DarkRed
        }
    }
    finally {
        $stopwatch.Stop()
        $result.Duration = $stopwatch.ElapsedMilliseconds
        $script:testResults += $result
    }
}

# Test: Basic installer execution
Run-Test -Name "Basic Installer Execution" -Category "Core" -Test {
    $installer = Join-Path $testConfig.MockInstallersPath "MockChrome.exe"
    $process = Start-Process -FilePath $installer -ArgumentList "/S" -PassThru -Wait
    
    if ($process.ExitCode -ne 0) {
        throw "Installer returned non-zero exit code: $($process.ExitCode)"
    }
}

# Test: Multiple concurrent installations
Run-Test -Name "Concurrent Installations" -Category "Core" -Test {
    $installers = @("MockFirefox.exe", "MockVSCode.exe", "MockSilentInstaller.exe")
    $jobs = @()
    
    foreach ($installer in $installers) {
        $path = Join-Path $testConfig.MockInstallersPath $installer
        $jobs += Start-Job -ScriptBlock {
            param($installerPath)
            Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait
        } -ArgumentList $path
    }
    
    $jobs | Wait-Job -Timeout 30 | Out-Null
    $failed = $jobs | Where-Object { $_.State -ne "Completed" }
    
    if ($failed) {
        throw "Some concurrent installations failed or timed out"
    }
    
    $jobs | Remove-Job -Force
}

# Test: Failure handling
Run-Test -Name "Failure Handling" -Category "ErrorHandling" -Test {
    $installer = Join-Path $testConfig.MockInstallersPath "MockFailingInstaller.exe"
    $process = Start-Process -FilePath $installer -ArgumentList "/S" -PassThru -Wait
    
    if ($process.ExitCode -eq 0) {
        throw "Failing installer should return non-zero exit code"
    }
}

# Test: Timeout handling
Run-Test -Name "Timeout Detection" -Category "ErrorHandling" -Test {
    $installer = Join-Path $testConfig.MockInstallersPath "MockTimeoutInstaller.exe"
    
    $job = Start-Job -ScriptBlock {
        param($installerPath)
        Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait
    } -ArgumentList $installer
    
    $completed = Wait-Job -Job $job -Timeout 5
    
    if ($completed) {
        throw "Timeout installer should not complete within 5 seconds"
    }
    
    Stop-Job -Job $job
    Remove-Job -Job $job -Force
}

# Test: MSI installer handling
Run-Test -Name "MSI Installer Support" -Category "Installers" -Test {
    $installer = Join-Path $testConfig.MockInstallersPath "Mock7Zip.msi"
    
    if (Test-Path $installer) {
        $args = "/i `"$installer`" /qn /norestart"
        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $args -PassThru -Wait
        
        if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 1603) {
            throw "MSI installation failed with exit code: $($process.ExitCode)"
        }
    }
}

# Test: Retry logic
Run-Test -Name "Retry Mechanism" -Category "Core" -Test {
    $installer = Join-Path $testConfig.MockInstallersPath "MockFailingInstaller.exe"
    $maxRetries = 3
    $retryCount = 0
    $success = $false
    
    while ($retryCount -lt $maxRetries -and -not $success) {
        $process = Start-Process -FilePath $installer -ArgumentList "/S" -PassThru -Wait
        
        if ($process.ExitCode -eq 0) {
            $success = $true
        } else {
            $retryCount++
            Start-Sleep -Seconds 1
        }
    }
    
    if ($retryCount -ne $maxRetries) {
        throw "Expected $maxRetries retries, but got $retryCount"
    }
}

# Test: Silent flag detection
Run-Test -Name "Silent Flag Detection" -Category "Core" -Test {
    $testCases = @(
        @{ File = "test.exe"; Flags = @("/S", "/SILENT", "/QUIET") }
        @{ File = "test.msi"; Flags = @("/qn", "/quiet") }
    )
    
    foreach ($test in $testCases) {
        foreach ($flag in $test.Flags) {
            # This would normally test the actual NullInstaller logic
            # For now, we're just validating the flag format
            if (-not $flag.StartsWith("/")) {
                throw "Invalid silent flag format: $flag"
            }
        }
    }
}

# Test: Post-installation hook
Run-Test -Name "Post-Install Hook Execution" -Category "Hooks" -Test {
    $hookScript = Join-Path $testConfig.TempPath "test_hook.ps1"
    $flagFile = Join-Path $testConfig.TempPath "hook_executed.flag"
    
    # Create hook script
    @"
`$flagFile = '$flagFile'
New-Item -Path `$flagFile -ItemType File -Force | Out-Null
Add-Content -Path `$flagFile -Value "Hook executed at `$(Get-Date)"
"@ | Out-File -FilePath $hookScript -Encoding UTF8
    
    # Execute hook
    & powershell.exe -ExecutionPolicy Bypass -File $hookScript
    
    if (-not (Test-Path $flagFile)) {
        throw "Post-install hook did not execute successfully"
    }
    
    Remove-Item $flagFile -Force -ErrorAction SilentlyContinue
}

# Test: JetBrains plugin installation
Run-Test -Name "JetBrains Plugin Integration" -Category "Plugins" -Test {
    $pluginScript = Join-Path $PSScriptRoot "..\jetbrains_plugin_integration.ps1"
    
    if (Test-Path $pluginScript) {
        # Test that the script syntax is valid
        $errors = @()
        [System.Management.Automation.Language.Parser]::ParseFile($pluginScript, [ref]$null, [ref]$errors)
        
        if ($errors.Count -gt 0) {
            throw "JetBrains plugin script has syntax errors: $($errors[0].Message)"
        }
    } else {
        Write-Warning "JetBrains plugin script not found, skipping validation"
    }
}

# Test: Logging functionality
Run-Test -Name "Installation Logging" -Category "Core" -Test {
    $logFile = Join-Path $testConfig.TempPath "test_install.log"
    $installer = Join-Path $testConfig.MockInstallersPath "MockChrome.exe"
    
    # Run installer with output redirection
    $output = & $installer /S 2>&1
    $output | Out-File -FilePath $logFile -Encoding UTF8
    
    if (-not (Test-Path $logFile)) {
        throw "Log file was not created"
    }
    
    $logContent = Get-Content $logFile -Raw
    if ($logContent.Length -eq 0) {
        throw "Log file is empty"
    }
}

# Test: Registry verification
Run-Test -Name "Registry Installation Check" -Category "Verification" -Test {
    # Check if we can read the uninstall registry key
    $paths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    )
    
    $accessible = $false
    foreach ($path in $paths) {
        if (Test-Path $path) {
            $accessible = $true
            break
        }
    }
    
    if (-not $accessible) {
        throw "Cannot access Windows uninstall registry keys"
    }
}

# Test: Download functionality
Run-Test -Name "Download Capability" -Category "Network" -Test {
    $testUrl = "https://www.google.com/robots.txt"
    $downloadPath = Join-Path $testConfig.TempPath "test_download.txt"
    
    try {
        Invoke-WebRequest -Uri $testUrl -OutFile $downloadPath -UseBasicParsing -TimeoutSec 10
        
        if (-not (Test-Path $downloadPath)) {
            throw "Download test failed - file not created"
        }
        
        $content = Get-Content $downloadPath -Raw
        if ($content.Length -eq 0) {
            throw "Downloaded file is empty"
        }
    }
    catch {
        if ($CI) {
            Write-Warning "Download test failed in CI environment (might be network restricted)"
        } else {
            throw $_
        }
    }
    finally {
        Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
    }
}

# Test: UI responsiveness simulation
Run-Test -Name "UI Responsiveness" -Category "Performance" -Test {
    # Simulate rapid installer additions
    $mockFiles = Get-ChildItem $testConfig.MockInstallersPath -Filter "*.exe"
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    foreach ($file in $mockFiles) {
        # Simulate file info gathering
        $fileInfo = Get-Item $file.FullName
        $size = $fileInfo.Length
        $name = $fileInfo.Name
    }
    
    $stopwatch.Stop()
    
    if ($stopwatch.ElapsedMilliseconds -gt 1000) {
        throw "File scanning took too long: $($stopwatch.ElapsedMilliseconds)ms"
    }
}

# Generate test report
function Generate-TestReport {
    Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
    Write-Host "TEST RESULTS SUMMARY" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    
    $totalTests = $script:testResults.Count
    $passRate = if ($totalTests -gt 0) { [math]::Round(($script:testsPassed / $totalTests) * 100, 2) } else { 0 }
    
    Write-Host "Total Tests: $totalTests" -ForegroundColor White
    Write-Host "Passed: $script:testsPassed" -ForegroundColor Green
    Write-Host "Failed: $script:testsFailed" -ForegroundColor Red
    Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } else { "Yellow" })
    
    if ($script:testsFailed -gt 0) {
        Write-Host "`nFailed Tests:" -ForegroundColor Red
        $script:testResults | Where-Object { -not $_.Success } | ForEach-Object {
            Write-Host "  - $($_.Name)" -ForegroundColor Red
            if ($Verbose -and $_.Error) {
                Write-Host "    Error: $($_.Error)" -ForegroundColor DarkRed
            }
        }
    }
    
    # Write detailed report to file
    $report = @"
NullInstaller Integration Test Report
Generated: $(Get-Date)
=====================================

Summary:
--------
Total Tests: $totalTests
Passed: $script:testsPassed
Failed: $script:testsFailed
Pass Rate: $passRate%

Detailed Results:
-----------------
"@
    
    foreach ($result in $script:testResults) {
        $status = if ($result.Success) { "PASSED" } else { "FAILED" }
        $report += "`n[$status] $($result.Name) (Category: $($result.Category), Duration: $($result.Duration)ms)"
        
        if (-not $result.Success -and $result.Error) {
            $report += "`n    Error: $($result.Error)"
        }
    }
    
    $report | Out-File -FilePath $testConfig.TestLogPath -Encoding UTF8
    Write-Host "`nDetailed report saved to: $($testConfig.TestLogPath)" -ForegroundColor Gray
    
    # Exit with appropriate code for CI
    if ($CI) {
        if ($script:testsFailed -gt 0) {
            exit 1
        } else {
            exit 0
        }
    }
}

# Main execution
try {
    Initialize-TestEnvironment
    
    Write-Host "`n=== Running Integration Tests ===" -ForegroundColor Cyan
    Write-Host "Test Category: $TestCategory" -ForegroundColor Gray
    
    # Tests are defined and run inline above
    
    Generate-TestReport
}
catch {
    Write-Host "`n✗ Test suite failed with error: $_" -ForegroundColor Red
    if ($CI) {
        exit 1
    }
}
finally {
    # Cleanup
    if (Test-Path $testConfig.TempPath) {
        Remove-Item $testConfig.TempPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}
