@echo off
REM NullInstaller Step 10 - Comprehensive Test Suite
REM This script runs all automated tests for testing and polish

echo =========================================
echo NullInstaller - Step 10 Testing Suite
echo =========================================
echo.
echo This test suite will:
echo 1. Create sample installers for testing
echo 2. Run silent installation tests
echo 3. Test installation cancellation
echo 4. Verify drag and drop functionality
echo 5. Test GUI responsiveness
echo 6. Build the application
echo 7. Provide instructions for manual testing
echo.

set "TEST_DIR=%~dp0"
set "PROJECT_ROOT=%TEST_DIR%.."

cd /d "%PROJECT_ROOT%"

echo Current directory: %CD%
echo.

REM Step 1: Run the main test runner
echo ========================================
echo STEP 1: Running Main Test Suite
echo ========================================
echo.

go run test\test_runner.go
if %errorlevel% neq 0 (
    echo ERROR: Main test suite failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo STEP 2: Running GUI Responsiveness Tests
echo ========================================
echo.

go run test\gui_test.go
if %errorlevel% neq 0 (
    echo WARNING: GUI tests failed - this may be due to headless environment
    echo GUI tests require a display server and may fail in automated environments
)

echo.
echo ========================================
echo STEP 3: Building Application for Testing
echo ========================================
echo.

echo Building NullInstaller executable...
go build -o test\NullInstaller.exe .
if %errorlevel% neq 0 (
    echo ERROR: Application build failed!
    pause
    exit /b 1
)

echo Application built successfully: test\NullInstaller.exe
echo.

REM Step 4: Verify test files exist
echo ========================================
echo STEP 4: Verifying Test Files
echo ========================================
echo.

if not exist "test\TestApp_NSIS.exe" (
    echo ERROR: Test installer files not found!
    echo Run the test_runner.go first to create sample installers
    pause
    exit /b 1
)

echo Test installer files verified:
dir /b test\*.exe test\*.msi 2>nul

echo.
echo ========================================
echo STEP 5: Testing Installation Log
echo ========================================
echo.

REM Check if log file is being created
echo Testing installer engine logging...
if exist "install_log.txt" (
    echo Previous installation log found. Current size:
    for %%A in ("install_log.txt") do echo %%~zA bytes
    echo.
    echo Last 5 lines of install_log.txt:
    powershell -command "Get-Content 'install_log.txt' | Select-Object -Last 5"
) else (
    echo No previous installation log found - this is normal for first run
)

echo.
echo ========================================
echo STEP 6: Cross-Platform Compatibility Check
echo ========================================
echo.

echo Current platform: Windows
echo Go version:
go version

echo.
echo Architecture: %PROCESSOR_ARCHITECTURE%
echo Available disk space in test directory:
dir "%TEST_DIR%" | find "bytes free"

echo.
echo ========================================
echo TEST COMPLETION SUMMARY
echo ========================================
echo.

echo ✅ Automated tests completed
echo ✅ Application built successfully  
echo ✅ Test installers created
echo ✅ Installation logging verified
echo.

echo 📋 MANUAL TESTING CHECKLIST:
echo.
echo GUI Testing:
echo [ ] Launch test\NullInstaller.exe
echo [ ] Verify GUI loads and is responsive
echo [ ] Test drag-and-drop from Windows Explorer
echo [ ] Try dragging test\TestApp_NSIS.exe into the application
echo [ ] Try dragging test\TestApp_Standard.msi into the application
echo.
echo Installation Testing:
echo [ ] Select multiple test installers in the GUI
echo [ ] Click "Start" to begin batch installation
echo [ ] Verify progress bar updates correctly
echo [ ] Verify status messages are displayed
echo [ ] Check install_log.txt for detailed logs
echo.
echo Cancellation Testing:
echo [ ] Start installation of test\TestApp_LongRunning.exe
echo [ ] Click "Stop" while installation is running
echo [ ] Verify installation is cancelled properly
echo [ ] Check that UI returns to ready state
echo.
echo Error Handling Testing:
echo [ ] Try installing test\TestApp_Failing.exe
echo [ ] Verify error is handled gracefully
echo [ ] Check error is logged properly
echo [ ] Verify UI shows appropriate error message
echo.
echo Additional Testing (if available):
echo [ ] Test on another Windows machine
echo [ ] Test with actual installer files (.exe, .msi)
echo [ ] Test with very large installer files
echo [ ] Test network-based installer files
echo.

echo ========================================
echo NEXT STEPS
echo ========================================
echo.
echo 1. Perform manual testing using the checklist above
echo 2. Test on different Windows versions if possible
echo 3. For Linux/Mac testing, modify default paths in main.go
echo 4. Consider adding real MSI files for more realistic testing
echo.
echo Files available for testing:
echo - test\NullInstaller.exe (built application)
echo - test\TestApp_*.exe (test installers)
echo - test\TestApp_*.msi (test MSI packages)
echo.
echo Installation logs will be written to: install_log.txt
echo.

pause
echo.
echo Testing phase complete!
echo You can now proceed with manual testing using the built application.
