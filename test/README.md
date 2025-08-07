# NullInstaller - Step 10 Testing Suite

This directory contains comprehensive tests for **Step 10: Testing and Polish** of the NullInstaller project.

## Overview

Step 10 focuses on:

1. ✅ Populate test folder with sample installers; verify silent installation success and GUI responsiveness
2. ✅ Validate stop/cancel while mid-install
3. ✅ Confirm drag & drop additions are honoured
4. ✅ Smoke-test on another Windows machine; optional: test cross-compilation for Linux/Mac

## Test Files

### Automated Test Scripts

- **`test_runner.go`** - Main test suite that creates sample installers and runs core functionality tests
- **`gui_test.go`** - GUI responsiveness and UI interaction tests using Fyne's test framework
- **`run_all_tests.bat`** - Windows batch script that orchestrates all tests and builds the application

### Sample Installers (Created by Tests)

The test suite creates mock installer files for testing:

#### EXE Installers
- `TestApp_NSIS.exe` - Simulates NSIS installer (uses `/S` flag)
- `TestApp_InstallShield.exe` - Simulates InstallShield installer (uses `/silent` flag)
- `TestApp_InnoSetup.exe` - Simulates Inno Setup installer (uses `/VERYSILENT` flag)
- `TestApp_Generic.exe` - Generic installer (uses `/quiet` flag)
- `TestApp_LongRunning.exe` - Long-running installer for cancellation testing
- `TestApp_Failing.exe` - Installer that intentionally fails for error handling tests

#### MSI Packages
- `TestApp_MSI_Standard.msi` - Standard MSI package
- `TestApp_MSI_Large.msi` - Large MSI package
- `TestApp_MSI_Complex.msi` - Complex MSI package

## Running Tests

### Quick Start (Windows)

```batch
# Run all tests automatically
test\run_all_tests.bat
```

### Manual Test Execution

```bash
# 1. Create sample installers and run core tests
go run test/test_runner.go

# 2. Run GUI responsiveness tests
go run test/gui_test.go

# 3. Build the application for manual testing
go build -o test/NullInstaller.exe .
```

## Test Categories

### 1. Silent Installation Tests
- Verifies that different installer types run silently with appropriate flags
- Tests MSI installation via `msiexec /i package.msi /qn /norestart`
- Tests EXE installations with various silent flags (`/S`, `/silent`, `/VERYSILENT`, `/quiet`)

### 2. Installation Cancellation Tests
- Tests ability to cancel long-running installations
- Verifies proper cleanup when installations are stopped
- Ensures UI returns to ready state after cancellation

### 3. Drag & Drop Tests
- Validates file extension filtering (`.exe`, `.msi`)
- Tests duplicate detection and prevention
- Verifies UI updates when files are added via drag-and-drop

### 4. GUI Responsiveness Tests
- Tests UI response times during file operations
- Validates progress bar updates
- Tests button state changes and responsiveness
- Verifies theme changes don't break UI

### 5. Error Handling Tests
- Tests behavior with failing installers
- Validates error logging and display
- Tests handling of non-existent files

### 6. Cross-Platform Compatibility Tests
- Path handling across different path separators
- File extension case sensitivity
- Platform-specific considerations for MSI files

## Manual Testing Checklist

After running automated tests, perform these manual tests:

### Basic Functionality
- [ ] Launch `test/NullInstaller.exe`
- [ ] Verify GUI loads without errors
- [ ] Test window resizing and responsiveness
- [ ] Verify dark theme is applied correctly

### Drag & Drop Testing  
- [ ] Drag `TestApp_NSIS.exe` from Windows Explorer into the app
- [ ] Drag `TestApp_Standard.msi` into the app
- [ ] Try dragging invalid file types (should be rejected)
- [ ] Try dragging the same file twice (should detect duplicates)

### Installation Testing
- [ ] Select multiple test installers
- [ ] Click "Start" to begin batch installation
- [ ] Verify progress bar updates during installation
- [ ] Check that status messages are displayed correctly
- [ ] Monitor `install_log.txt` for detailed logging

### Cancellation Testing
- [ ] Start installation of `TestApp_LongRunning.exe`
- [ ] Click "Stop" while installation is running  
- [ ] Verify installation cancels properly
- [ ] Check UI returns to ready state
- [ ] Verify partial progress is handled correctly

### Error Handling
- [ ] Try installing `TestApp_Failing.exe`
- [ ] Verify error is handled gracefully
- [ ] Check error appears in `install_log.txt`
- [ ] Verify UI shows appropriate error message

### Performance Testing
- [ ] Add many installer files and test responsiveness
- [ ] Test with very large installer files (if available)
- [ ] Monitor memory usage during extended operation

## Log Files

The application creates detailed logs in:

- **`install_log.txt`** - Detailed installation attempts, successes, and failures
- Console output during testing shows real-time progress

## Cross-Platform Notes

### Windows (Primary Platform)
- Full MSI support via `msiexec`
- All EXE installer types supported
- Native drag-and-drop functionality

### Linux/Mac (Optional Testing)
- MSI files are Windows-specific and won't work
- EXE files may not execute (wine required for testing)
- Default paths need to be modified in `main.go`
- Drag-and-drop behavior may differ

To test on Linux/Mac:
1. Modify the default path in `main.go` line 285
2. Disable MSI support in `installer_engine.go`
3. Use shell scripts instead of batch files for mock installers

## Expected Test Results

All automated tests should pass. Typical results:

```
✅ Create Sample Installers (< 1s)
✅ Silent Installation (2-5s)
✅ Installation Cancellation (3-5s)
✅ Drag and Drop Simulation (< 1s)
✅ UI Responsiveness (< 1s)  
✅ Error Handling (1-2s)
✅ Cross-Platform Compatibility (< 1s)
✅ Build Application (5-10s)

Total: 8 tests, 8 passed, 0 failed
```

## Troubleshooting

### Common Issues

**Build Failures**
- Ensure Go 1.19+ is installed
- Run `go mod tidy` to update dependencies
- Check that all dependencies are available

**GUI Tests Fail**
- GUI tests require a display server
- May fail in headless/CI environments
- This is expected behavior in automated environments

**Permission Errors**
- Ensure write access to project directory
- On Windows, run as Administrator if needed
- Antivirus may block mock installer creation

**Test Files Not Found**
- Run `test_runner.go` first to create sample installers
- Ensure you're running from the project root directory

## Next Steps

After successful testing:

1. Deploy on additional Windows machines for broader compatibility testing
2. Test with real-world installer files
3. Consider performance optimizations based on test results
4. Document any platform-specific limitations discovered

## Contributing

When adding new tests:

1. Follow existing test patterns in `test_runner.go`
2. Add both automated and manual test cases
3. Update this README with new test procedures
4. Ensure tests clean up after themselves
