# NullInstaller Testing Documentation

## Overview
This document provides comprehensive testing guidelines for NullInstaller v2.0, covering unit tests, integration tests, and manual QA procedures.

## Test Infrastructure

### Directory Structure
```
tests/
├── mock_installers/         # Mock installer executables for testing
├── generate_mocks_simple.ps1    # Mock installer generator
├── test_runner_simple.ps1       # Main test runner
├── integration_test.ps1         # Full integration test suite
├── NullInstaller.Tests.cs       # C# unit tests
├── ci_pipeline.yml              # CI/CD configuration
├── MANUAL_QA_CHECKLIST.md      # Manual testing checklist
└── test_reports/                # Test execution reports
```

## Quick Start

### Running Tests Locally
```powershell
# Run all tests
powershell -ExecutionPolicy Bypass -File ./tests/test_runner_simple.ps1

# Run integration tests only
powershell -ExecutionPolicy Bypass -File ./tests/integration_test.ps1

# Generate mock installers
powershell -ExecutionPolicy Bypass -File ./tests/generate_mocks_simple.ps1
```

### CI/CD Integration
Tests are automatically run on:
- Push to main/develop branches
- Pull requests
- Manual workflow dispatch

## Test Categories

### 1. Unit Tests
**Purpose:** Test individual components in isolation

| Test | Description | Expected Result |
|------|-------------|-----------------|
| Silent Flag Detection | Validates installer flag parsing | Correct flags identified |
| Registry Path Validation | Checks registry access | Paths accessible |
| Error Handling | Tests exception handling | Graceful failure |
| Timeout Detection | Validates 2-minute timeout | Process killed after timeout |

### 2. Integration Tests
**Purpose:** Test component interactions

| Test | Description | Expected Result |
|------|-------------|-----------------|
| Mock Installer Execution | Runs mock installers | Exit codes match expectations |
| Concurrent Installation | Multiple installers at once | All complete successfully |
| Retry Logic | Failed installer retry | 3 retry attempts |
| Post-Install Hooks | Script execution after install | Hook scripts run |

### 3. Performance Tests
**Purpose:** Ensure acceptable performance

| Metric | Target | Measurement |
|--------|--------|-------------|
| Startup Time | < 3 seconds | Time to UI ready |
| Memory Usage | < 500 MB | Peak working set |
| File Scanning | < 1 second | Time to scan 20+ files |
| Installation Queue | Linear scaling | Time vs installer count |

## Mock Installers

### Available Mocks
- **MockChrome.bat** - Successful installation (2s delay)
- **MockFirefox.bat** - Successful installation (3s delay)
- **MockVSCode.bat** - Successful installation (5s delay)
- **Mock7Zip.bat** - Successful MSI installation (1s delay)
- **MockFailingInstaller.bat** - Always fails (exit code 1)
- **MockTimeoutInstaller.bat** - Exceeds 2-minute timeout
- **MockSilentInstaller.bat** - Tests silent flag detection
- **MockJetBrains.bat** - IDE plugin installation test

### Creating New Mocks
Add to `generate_mocks_simple.ps1`:
```powershell
@{Name = "MockNewApp.bat"; ExitCode = 0; Delay = 2}
```

## Test Execution Flow

### Phase 1: Environment Setup
1. Verify PowerShell version >= 5.0
2. Create test directories
3. Generate mock installers

### Phase 2: Core Tests
1. Test successful installation
2. Test failed installation
3. Test retry mechanism
4. Test concurrent installations

### Phase 3: Advanced Tests
1. Post-installation hooks
2. JetBrains plugin integration
3. Registry verification
4. Logging functionality

### Phase 4: Reporting
1. Generate test summary
2. Calculate pass rate
3. Save report to file
4. Exit with appropriate code

## Manual QA Process

### Pre-Release Checklist
- [ ] Run full test suite locally
- [ ] Test on clean Windows 11 VM
- [ ] Verify all installers work
- [ ] Check memory/CPU usage
- [ ] Validate error messages
- [ ] Test with non-admin account
- [ ] Verify uninstall process

### VM Testing Setup
1. Create clean Windows 11 VM
2. Install test prerequisites
3. Copy NullInstaller.exe
4. Run through MANUAL_QA_CHECKLIST.md
5. Document any issues found

## CI/CD Pipeline

### GitHub Actions Workflow
```yaml
on: [push, pull_request]
jobs:
  test:
    runs-on: windows-latest
    steps:
      - Generate mock installers
      - Run unit tests
      - Run integration tests
      - Upload test results
```

### Test Results
- Unit test results: `tests/TestResults/*.trx`
- Integration results: `tests/test_results.log`
- Coverage report: `tests/coverage.xml`

## Troubleshooting

### Common Issues

**Issue:** Tests fail with "Access Denied"
**Solution:** Run PowerShell as Administrator or adjust test expectations for CI

**Issue:** Mock installers not found
**Solution:** Run `generate_mocks_simple.ps1` first

**Issue:** Registry tests fail
**Solution:** May require admin rights or skip in CI environment

**Issue:** Timeout tests take too long
**Solution:** Use `-Quick` flag to skip long-running tests

## Test Metrics

### Current Status
- **Total Tests:** 7 core tests + 15 integration tests
- **Pass Rate Target:** >= 80%
- **Coverage Target:** >= 70%
- **Performance Target:** All tests < 60 seconds

### Key Performance Indicators
1. Test execution time
2. Pass/fail ratio
3. Code coverage percentage
4. Defect detection rate

## Best Practices

### Writing New Tests
1. Keep tests isolated and independent
2. Use descriptive test names
3. Clean up test artifacts
4. Handle both success and failure cases
5. Add appropriate timeouts

### Test Data Management
- Use mock installers for consistency
- Clean up temporary files after tests
- Don't rely on external dependencies
- Use deterministic test data

### CI/CD Considerations
- Tests must work in headless environment
- Account for limited permissions
- Handle network restrictions
- Provide clear failure messages

## Validation Scenarios

### Scenario 1: Fresh Installation
1. Clean Windows 11 system
2. No previous NullInstaller
3. Run full test suite
4. Verify all components work

### Scenario 2: Upgrade Testing
1. Install previous version
2. Upgrade to new version
3. Verify settings preserved
4. Check for conflicts

### Scenario 3: Stress Testing
1. Add 50+ installers
2. Run concurrent installations
3. Monitor resource usage
4. Check for memory leaks

### Scenario 4: Error Recovery
1. Corrupt an installer
2. Remove files during installation
3. Kill processes mid-install
4. Verify graceful recovery

## Reporting Issues

When reporting test failures, include:
1. Test name and category
2. Expected vs actual result
3. Environment details (OS, PowerShell version)
4. Steps to reproduce
5. Any error messages or logs

## Future Improvements

### Planned Enhancements
- [ ] Automated UI testing with Selenium
- [ ] Performance benchmarking suite
- [ ] Mutation testing for better coverage
- [ ] Integration with more CI/CD platforms
- [ ] Docker container for test environment

### Testing Roadmap
1. **Q1 2025:** Implement automated UI tests
2. **Q2 2025:** Add performance regression tests
3. **Q3 2025:** Integrate security scanning
4. **Q4 2025:** Full E2E automation

## Contact

For testing questions or issues:
- Create issue in GitHub repository
- Tag with `testing` label
- Include test logs and reports

---

*Last Updated: August 2024*
*Version: 1.0*
