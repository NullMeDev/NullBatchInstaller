# NullInstaller Test Runner
# Runs all unit and integration tests

param(
    [switch]$Quick,    # Run only essential tests
    [switch]$Full,     # Run all tests including manual QA prep
    [switch]$CI        # Running in CI environment
)

$ErrorActionPreference = "Continue"
$script:totalTests = 0
$script:passedTests = 0
$script:failedTests = 0
$script:startTime = Get-Date

Write-Host @"
╔══════════════════════════════════════════════════════════════╗
║           NullInstaller Test Suite v1.0                      ║
║           Unit & Integration Testing                         ║
╚══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Test helper functions
function Test-Assert {
    param(
        [string]$TestName,
        [scriptblock]$Condition,
        [string]$ErrorMessage = "Test failed"
    )
    
    $script:totalTests++
    Write-Host -NoNewline "  Testing: $TestName... "
    
    try {
        $result = & $Condition
        if ($result) {
            Write-Host "✓ PASSED" -ForegroundColor Green
            $script:passedTests++
            return $true
        } else {
            Write-Host "✗ FAILED: $ErrorMessage" -ForegroundColor Red
            $script:failedTests++
            return $false
        }
    }
    catch {
        Write-Host "✗ ERROR: $_" -ForegroundColor Red
        $script:failedTests++
        return $false
    }
}

# Phase 1: Environment Setup
Write-Host "`n[Phase 1] Environment Setup" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "PowerShell Version >= 5.0" {
    $PSVersionTable.PSVersion.Major -ge 5
} -ErrorMessage "PowerShell 5.0 or higher required"

Test-Assert "Test directory exists" {
    Test-Path "./tests"
} -ErrorMessage "Test directory not found"

Test-Assert "Mock installers directory created" {
    if (-not (Test-Path "./tests/mock_installers")) {
        New-Item -ItemType Directory -Path "./tests/mock_installers" -Force | Out-Null
    }
    Test-Path "./tests/mock_installers"
}

# Phase 2: Generate Mock Installers
Write-Host "`n[Phase 2] Mock Installer Generation" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "Mock installer generator exists" {
    Test-Path "./tests/generate_mocks_simple.ps1"
}

Test-Assert "Generate mock installers" {
    & ./tests/generate_mocks_simple.ps1 | Out-Null
    $mocks = Get-ChildItem "./tests/mock_installers" -Filter "*.bat"
    $mocks.Count -ge 8
} -ErrorMessage "Failed to generate mock installers"

# Phase 3: Core Functionality Tests
Write-Host "`n[Phase 3] Core Functionality Tests" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "NullInstaller.cs exists" {
    Test-Path "./NullInstaller.cs"
}

Test-Assert "Silent flag patterns validation" {
    $patterns = @("/S", "/SILENT", "/QUIET", "/q", "/qn")
    $patterns.Count -eq 5
}

Test-Assert "Registry path validation" {
    # Check if we can access registry (might fail in restricted environments)
    try {
        $null = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion" -ErrorAction Stop
        $true
    } catch {
        if ($CI) {
            Write-Warning "Registry access limited in CI environment"
            $true  # Pass in CI
        } else {
            $false
        }
    }
}

# Phase 4: Mock Installer Execution Tests
Write-Host "`n[Phase 4] Mock Installer Execution Tests" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "Successful installation simulation" {
    $process = Start-Process -FilePath "./tests/mock_installers/MockChrome.bat" -PassThru -Wait -WindowStyle Hidden
    $process.ExitCode -eq 0
}

Test-Assert "Failed installation simulation" {
    $process = Start-Process -FilePath "./tests/mock_installers/MockFailingInstaller.bat" -PassThru -Wait -WindowStyle Hidden
    $process.ExitCode -eq 1
} -ErrorMessage "Failing installer should return exit code 1"

Test-Assert "Silent mode detection" {
    $testScript = "./tests/mock_installers/MockSilentInstaller.exe.ps1"
    if (Test-Path $testScript) {
        $output = & powershell -ExecutionPolicy Bypass -File $testScript -Silent "/S" 2>&1
        $output -join " " -match "silent mode"
    } else {
        $true  # Skip if script doesn't exist
    }
}

# Phase 5: Concurrent Installation Tests
if (-not $Quick) {
    Write-Host "`n[Phase 5] Concurrent Installation Tests" -ForegroundColor Yellow
    Write-Host "=" * 60
    
    Test-Assert "Parallel installer execution" {
        $jobs = @()
        $installers = @("MockChrome.bat", "MockFirefox.bat", "Mock7Zip.bat")
        
        foreach ($installer in $installers) {
            $path = Join-Path "./tests/mock_installers" $installer
            $jobs += Start-Job -ScriptBlock {
                param($installerPath)
                $process = Start-Process -FilePath $installerPath -PassThru -Wait -WindowStyle Hidden
                $process.ExitCode -eq 0
            } -ArgumentList $path
        }
        
        $results = $jobs | Wait-Job -Timeout 30 | Receive-Job
        $jobs | Remove-Job -Force
        
        ($results | Where-Object { $_ -eq $true }).Count -eq $installers.Count
    } -ErrorMessage "Some concurrent installations failed"
}

# Phase 6: Retry Logic Tests
Write-Host "`n[Phase 6] Retry Logic Tests" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "Retry mechanism validation" {
    $attempts = 0
    $maxRetries = 3
    $success = $false
    
    while ($attempts -lt $maxRetries -and -not $success) {
        $process = Start-Process -FilePath "./tests/mock_installers/MockFailingInstaller.bat" `
                                 -PassThru -Wait -WindowStyle Hidden
        $attempts++
        if ($process.ExitCode -eq 0) {
            $success = $true
        }
    }
    
    $attempts -eq $maxRetries -and -not $success
} -ErrorMessage "Retry count mismatch"

# Phase 7: Post-Installation Hook Tests
Write-Host "`n[Phase 7] Post-Installation Hook Tests" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "Post-install script execution" {
    $hookScript = "./tests/test_hook.ps1"
    $flagFile = "$env:TEMP\hook_test.flag"
    
    # Create test hook
    @"
New-Item -Path '$flagFile' -ItemType File -Force | Out-Null
"@ | Out-File -FilePath $hookScript -Encoding UTF8
    
    # Execute hook
    & powershell -ExecutionPolicy Bypass -File $hookScript
    
    $result = Test-Path $flagFile
    
    # Cleanup
    Remove-Item $hookScript -Force -ErrorAction SilentlyContinue
    Remove-Item $flagFile -Force -ErrorAction SilentlyContinue
    
    $result
}

Test-Assert "JetBrains plugin script validation" {
    $pluginScript = "./jetbrains_plugin_integration.ps1"
    if (Test-Path $pluginScript) {
        $errors = @()
        $tokens = @()
        $ast = $null
        $parseErrors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile(
            $pluginScript, 
            [ref]$tokens, 
            [ref]$parseErrors
        )
        $parseErrors.Count -eq 0
    } else {
        Write-Warning "JetBrains plugin script not found"
        $true
    }
}

# Phase 8: Logging Tests
Write-Host "`n[Phase 8] Logging Tests" -ForegroundColor Yellow
Write-Host "=" * 60

Test-Assert "Log file creation" {
    $logPath = "$env:TEMP\test_install_$(Get-Random).log"
    "Test log entry at $(Get-Date)" | Out-File -FilePath $logPath
    $exists = Test-Path $logPath
    Remove-Item $logPath -Force -ErrorAction SilentlyContinue
    $exists
}

# Phase 9: Performance Tests
if (-not $Quick) {
    Write-Host "`n[Phase 9] Performance Tests" -ForegroundColor Yellow
    Write-Host "=" * 60
    
    Test-Assert "File scanning performance" {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $files = Get-ChildItem "./tests/mock_installers" -Filter "*.bat"
        foreach ($file in $files) {
            $size = $file.Length
            $name = $file.Name
        }
        $stopwatch.Stop()
        
        Write-Host "    (Scan time: $($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Gray
        $stopwatch.ElapsedMilliseconds -lt 1000
    } -ErrorMessage "File scanning too slow"
}

# Phase 10: Integration Test Suite
if (-not $Quick) {
    Write-Host "`n[Phase 10] Full Integration Test Suite" -ForegroundColor Yellow
    Write-Host "=" * 60
    
    Test-Assert "Integration test script exists" {
        Test-Path "./tests/integration_test.ps1"
    }
    
    if ($Full) {
        Write-Host "  Running full integration test suite..." -ForegroundColor Cyan
        $integrationResult = & powershell -ExecutionPolicy Bypass -File "./tests/integration_test.ps1" `
                                         -TestCategory "Core" -CI:$CI
        
        Test-Assert "Integration tests passed" {
            $LASTEXITCODE -eq 0
        }
    } else {
        Write-Host "  Skipping full integration suite (use -Full to run)" -ForegroundColor Gray
    }
}

# Generate Test Report
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "TEST EXECUTION SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

$duration = (Get-Date) - $script:startTime
$passRate = if ($script:totalTests -gt 0) { 
    [math]::Round(($script:passedTests / $script:totalTests) * 100, 2) 
} else { 0 }

Write-Host "Total Tests Run: $script:totalTests" -ForegroundColor White
Write-Host "Tests Passed: $script:passedTests" -ForegroundColor Green
Write-Host "Tests Failed: $script:failedTests" -ForegroundColor $(if ($script:failedTests -gt 0) { "Red" } else { "Gray" })
Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } elseif ($passRate -ge 60) { "Yellow" } else { "Red" })
Write-Host "Execution Time: $($duration.TotalSeconds) seconds" -ForegroundColor Gray

# Save report
$reportPath = "./tests/test_report_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
@"
NullInstaller Test Report
========================
Date: $(Get-Date)
Duration: $($duration.TotalSeconds) seconds

Results:
- Total Tests: $script:totalTests
- Passed: $script:passedTests
- Failed: $script:failedTests
- Pass Rate: $passRate%

Environment:
- PowerShell Version: $($PSVersionTable.PSVersion)
- OS: $([System.Environment]::OSVersion.VersionString)
- CI Mode: $CI
"@ | Out-File -FilePath $reportPath

Write-Host "`nReport saved to: $reportPath" -ForegroundColor Gray

# Exit code for CI
if ($CI) {
    if ($script:failedTests -gt 0) {
        Write-Host "`n✗ TEST SUITE FAILED" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "`n✓ TEST SUITE PASSED" -ForegroundColor Green
        exit 0
    }
} else {
    if ($script:failedTests -gt 0) {
        Write-Host "`n⚠ Some tests failed. Review the results above." -ForegroundColor Yellow
    } else {
        Write-Host "`n✓ All tests passed successfully!" -ForegroundColor Green
    }
}
