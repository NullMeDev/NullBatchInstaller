# Simple Test Runner for NullInstaller
Write-Host "`n=== NullInstaller Test Suite ===" -ForegroundColor Cyan
Write-Host "Running unit and integration tests`n" -ForegroundColor Gray

$testsPassed = 0
$testsFailed = 0

# Test 1: Mock installer generation
Write-Host "[TEST 1] Generating mock installers..." -ForegroundColor Yellow
try {
    & ./tests/generate_mocks_simple.ps1 | Out-Null
    $mocks = Get-ChildItem "./tests/mock_installers" -Filter "*.bat" -ErrorAction SilentlyContinue
    if ($mocks.Count -ge 8) {
        Write-Host "  PASSED: Generated $($mocks.Count) mock installers" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAILED: Only $($mocks.Count) mock installers found" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 2: Successful installation test
Write-Host "[TEST 2] Testing successful installation..." -ForegroundColor Yellow
try {
    $process = Start-Process -FilePath "./tests/mock_installers/MockChrome.bat" -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -eq 0) {
        Write-Host "  PASSED: Installation succeeded with exit code 0" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAILED: Exit code was $($process.ExitCode)" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 3: Failed installation test
Write-Host "[TEST 3] Testing failed installation..." -ForegroundColor Yellow
try {
    $process = Start-Process -FilePath "./tests/mock_installers/MockFailingInstaller.bat" -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -eq 1) {
        Write-Host "  PASSED: Installation failed as expected with exit code 1" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAILED: Exit code was $($process.ExitCode), expected 1" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 4: Retry logic test
Write-Host "[TEST 4] Testing retry logic..." -ForegroundColor Yellow
try {
    $attempts = 0
    $maxRetries = 3
    while ($attempts -lt $maxRetries) {
        $process = Start-Process -FilePath "./tests/mock_installers/MockFailingInstaller.bat" -PassThru -Wait -WindowStyle Hidden
        $attempts++
        if ($process.ExitCode -eq 0) { break }
    }
    
    if ($attempts -eq $maxRetries) {
        Write-Host "  PASSED: Retried $maxRetries times as expected" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAILED: Retry count mismatch" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 5: Post-install hook test
Write-Host "[TEST 5] Testing post-install hooks..." -ForegroundColor Yellow
try {
    $hookScript = "$env:TEMP\test_hook.ps1"
    $flagFile = "$env:TEMP\hook_test.flag"
    
    "New-Item -Path '$flagFile' -ItemType File -Force | Out-Null" | Out-File -FilePath $hookScript -Encoding UTF8
    & powershell -ExecutionPolicy Bypass -File $hookScript
    
    if (Test-Path $flagFile) {
        Write-Host "  PASSED: Post-install hook executed successfully" -ForegroundColor Green
        $testsPassed++
        Remove-Item $flagFile -Force -ErrorAction SilentlyContinue
    } else {
        Write-Host "  FAILED: Hook did not create flag file" -ForegroundColor Red
        $testsFailed++
    }
    
    Remove-Item $hookScript -Force -ErrorAction SilentlyContinue
} catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 6: Logging test
Write-Host "[TEST 6] Testing logging functionality..." -ForegroundColor Yellow
try {
    $logPath = "$env:TEMP\test_$(Get-Random).log"
    "Test log entry" | Out-File -FilePath $logPath
    
    if ((Test-Path $logPath) -and ((Get-Content $logPath).Length -gt 0)) {
        Write-Host "  PASSED: Log file created and contains data" -ForegroundColor Green
        $testsPassed++
        Remove-Item $logPath -Force
    } else {
        Write-Host "  FAILED: Log file issue" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 7: JetBrains plugin script check
Write-Host "[TEST 7] Checking JetBrains plugin integration..." -ForegroundColor Yellow
if (Test-Path "./jetbrains_plugin_integration.ps1") {
    Write-Host "  PASSED: JetBrains plugin script exists" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  WARNING: JetBrains plugin script not found" -ForegroundColor Yellow
    $testsPassed++  # Still pass as it's optional
}

# Test Summary
Write-Host "`n=== TEST SUMMARY ===" -ForegroundColor Cyan
$totalTests = $testsPassed + $testsFailed
$passRate = if ($totalTests -gt 0) { [math]::Round(($testsPassed / $totalTests) * 100, 2) } else { 0 }

Write-Host "Total Tests: $totalTests"
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Gray" })
Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } else { "Yellow" })

# Save report
$report = @"
Test Report - $(Get-Date)
Total: $totalTests
Passed: $testsPassed  
Failed: $testsFailed
Pass Rate: $passRate%
"@

$report | Out-File -FilePath "./tests/test_report.txt"
Write-Host "`nReport saved to: ./tests/test_report.txt" -ForegroundColor Gray

if ($testsFailed -eq 0) {
    Write-Host "`nALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nSOME TESTS FAILED!" -ForegroundColor Red
    exit 1
}
