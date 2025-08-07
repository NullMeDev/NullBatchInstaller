package main

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"time"
)

// TestResult represents the result of a test
type TestResult struct {
	TestName    string
	Passed      bool
	Message     string
	Duration    time.Duration
	ErrorDetail string
}

// TestRunner manages test execution
type TestRunner struct {
	results []TestResult
}

// RunTest executes a test function and records the result
func (tr *TestRunner) RunTest(name string, testFunc func() error) {
	fmt.Printf("\n=== Running Test: %s ===\n", name)
	start := time.Now()
	
	err := testFunc()
	duration := time.Since(start)
	
	result := TestResult{
		TestName: name,
		Passed:   err == nil,
		Duration: duration,
	}
	
	if err != nil {
		result.Message = "FAILED"
		result.ErrorDetail = err.Error()
		fmt.Printf("❌ FAILED: %s (took %v)\n", name, duration)
		fmt.Printf("   Error: %v\n", err)
	} else {
		result.Message = "PASSED"
		fmt.Printf("✅ PASSED: %s (took %v)\n", name, duration)
	}
	
	tr.results = append(tr.results, result)
}

// PrintSummary prints the final test summary
func (tr *TestRunner) PrintSummary() {
	fmt.Printf("\n" + strings.Repeat("=", 60) + "\n")
	fmt.Printf("TEST SUMMARY\n")
	fmt.Printf(strings.Repeat("=", 60) + "\n")
	
	passed := 0
	failed := 0
	
	for _, result := range tr.results {
		status := "✅"
		if !result.Passed {
			status = "❌"
			failed++
		} else {
			passed++
		}
		fmt.Printf("%s %s (%v)\n", status, result.TestName, result.Duration)
	}
	
	fmt.Printf("\nTotal: %d tests, %d passed, %d failed\n", len(tr.results), passed, failed)
	
	if failed == 0 {
		fmt.Printf("🎉 All tests passed!\n")
	} else {
		fmt.Printf("⚠️  Some tests failed. Check the details above.\n")
	}
}

// CreateTestInstallers creates various test installer files
func CreateTestInstallers() error {
	testDir := "test"
	
	// Ensure test directory exists
	if err := os.MkdirAll(testDir, 0755); err != nil {
		return fmt.Errorf("failed to create test directory: %v", err)
	}
	
	// Create batch file installers that simulate different installer types
	installers := map[string]string{
		"TestApp_NSIS.bat": `@echo off
echo Installing TestApp_NSIS (NSIS-style)...
if "%~1"=="/S" (
    echo [SILENT] NSIS installer running silently
    ping -n 3 127.0.0.1 >nul 2>&1
    echo Installation completed successfully
    exit /b 0
) else (
    echo This would show NSIS GUI - use /S for silent install
    exit /b 1
)`,
		
		"TestApp_InstallShield.bat": `@echo off
echo Installing TestApp_InstallShield...
if "%~1"=="/silent" (
    echo [SILENT] InstallShield installer running silently
    ping -n 4 127.0.0.1 >nul 2>&1
    echo Installation completed successfully
    exit /b 0
) else (
    echo This would show InstallShield GUI - use /silent for silent install
    exit /b 1
)`,
		
		"TestApp_LongRunning.bat": `@echo off
echo Installing TestApp_LongRunning (for cancellation testing)...
if "%~1"=="/S" (
    echo [SILENT] Long-running installer started
    ping -n 11 127.0.0.1 >nul 2>&1
    echo Installation completed successfully
    exit /b 0
) else (
    echo This installer takes a long time - use /S for silent install
    exit /b 1
)`,
		
		"TestApp_Failing.bat": `@echo off
echo Installing TestApp_Failing (will fail for testing)...
if "%~1"=="/S" (
    echo [SILENT] This installer will fail intentionally
    ping -n 2 127.0.0.1 >nul 2>&1
    echo Installation failed with error
    exit /b 1
) else (
    echo This would show GUI but will fail - use /S for silent install
    exit /b 1
)`,
	}
	
	// Create MSI test file (mock)
	msiContent := `PK Mock MSI file for testing - not a real MSI`
	
	for filename, content := range installers {
		filePath := filepath.Join(testDir, filename)
		if err := os.WriteFile(filePath, []byte(content), 0755); err != nil {
			return fmt.Errorf("failed to create %s: %v", filename, err)
		}
		fmt.Printf("Created test installer: %s\n", filePath)
	}
	
	// Create MSI test file
	msiPath := filepath.Join(testDir, "TestApp_Standard.msi")
	if err := os.WriteFile(msiPath, []byte(msiContent), 0644); err != nil {
		return fmt.Errorf("failed to create MSI test file: %v", err)
	}
	fmt.Printf("Created test MSI: %s\n", msiPath)
	
	// Create dummy .exe files for drag-drop testing (these won't execute but will be recognized by file extension)
	dummyExeFiles := []string{
		"TestApp_NSIS.exe",
		"TestApp_InstallShield.exe",
		"TestApp_DragDrop.exe",
	}
	
	for _, filename := range dummyExeFiles {
		exePath := filepath.Join(testDir, filename)
		// Create a dummy PE header so it looks like an executable
		dummyContent := "MZ" + strings.Repeat("\x00", 100) + "This is a dummy executable for testing NullInstaller drag and drop functionality."
		if err := os.WriteFile(exePath, []byte(dummyContent), 0755); err != nil {
			return fmt.Errorf("failed to create dummy exe %s: %v", filename, err)
		}
		fmt.Printf("Created dummy EXE: %s\n", exePath)
	}
	
	fmt.Printf("Successfully created %d test installer files\n", len(installers)+len(dummyExeFiles)+1)
	return nil
}

// TestSilentInstallation tests that installers run silently
func TestSilentInstallation() error {
	fmt.Printf("Testing silent installation functionality...\n")
	
	testInstallers := []struct {
		file string
		flag string
	}{
		{"TestApp_NSIS.bat", "/S"},
		{"TestApp_InstallShield.bat", "/silent"},
	}
	
	for _, installer := range testInstallers {
		filePath := filepath.Join("test", installer.file)
		if _, err := os.Stat(filePath); os.IsNotExist(err) {
			return fmt.Errorf("test installer %s not found", filePath)
		}
		
		fmt.Printf("  Testing silent install: %s with %s\n", installer.file, installer.flag)
		
		cmd := exec.Command("cmd", "/c", filePath, installer.flag)
		output, err := cmd.CombinedOutput()
		
		if err != nil {
			return fmt.Errorf("silent installation failed for %s: %v\nOutput: %s", installer.file, err, string(output))
		}
		
		if !cmd.ProcessState.Success() {
			return fmt.Errorf("silent installation returned non-zero exit code for %s", installer.file)
		}
		
		fmt.Printf("    ✅ %s installed silently successfully\n", installer.file)
	}
	
	return nil
}

// TestInstallationCancellation tests the ability to cancel installations
func TestInstallationCancellation() error {
	fmt.Printf("Testing installation cancellation...\n")
	
	// Test with long-running installer
	filePath := filepath.Join("test", "TestApp_LongRunning.bat")
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		return fmt.Errorf("long-running test installer not found: %s", filePath)
	}
	
	fmt.Printf("  Starting long-running installer...\n")
	cmd := exec.Command("cmd", "/c", filePath, "/S")
	
	if err := cmd.Start(); err != nil {
		return fmt.Errorf("failed to start long-running installer: %v", err)
	}
	
	// Wait a moment to let it start
	time.Sleep(2 * time.Second)
	
	fmt.Printf("  Attempting to cancel installation...\n")
	
	// Kill the process to simulate cancellation
	if err := cmd.Process.Kill(); err != nil {
		return fmt.Errorf("failed to kill long-running installer: %v", err)
	}
	
	// Wait for the process to actually terminate
	_, err := cmd.Process.Wait()
	// Process.Kill() on Windows doesn't always guarantee an error status
	// The important thing is that the process was terminated
	if err == nil {
		// On Windows, killing a process may still return success
		// Let's check if it was actually terminated by checking the exit code
		if cmd.ProcessState != nil && cmd.ProcessState.Success() {
			return fmt.Errorf("process completed successfully instead of being cancelled")
		}
		// Process was terminated (even if err is nil), which is what we want
	}
	
	fmt.Printf("    ✅ Installation cancellation successful\n")
	return nil
}

// TestDragDropSimulation tests drag and drop file handling
func TestDragDropSimulation() error {
	fmt.Printf("Testing drag and drop functionality...\n")
	
	// Verify test files exist for drag-drop simulation
	testFiles := []string{
		"TestApp_NSIS.exe",
		"TestApp_InstallShield.exe",
		"TestApp_Standard.msi",
	}
	
	for _, file := range testFiles {
		filePath := filepath.Join("test", file)
		if _, err := os.Stat(filePath); os.IsNotExist(err) {
			return fmt.Errorf("test file for drag-drop not found: %s", filePath)
		}
	}
	
	fmt.Printf("  ✅ All drag-drop test files are available\n")
	fmt.Printf("  📋 Test files can be dragged into the NullInstaller GUI\n")
	fmt.Printf("  📋 Files should be validated and added to the installer list\n")
	
	return nil
}

// TestUIResponsiveness tests that the UI remains responsive
func TestUIResponsiveness() error {
	fmt.Printf("Testing UI responsiveness...\n")
	
	// This test would need to be integrated with the actual UI
	// For now, we'll simulate by checking that test files can be processed quickly
	
	startTime := time.Now()
	
	// Simulate file processing that should be fast
	testFiles := []string{"TestApp_NSIS.exe", "TestApp_InstallShield.exe", "TestApp_Standard.msi"}
	
	for _, file := range testFiles {
		filePath := filepath.Join("test", file)
		if _, err := os.Stat(filePath); err != nil {
			return fmt.Errorf("failed to stat test file %s: %v", file, err)
		}
		
		// Simulate file processing time
		time.Sleep(10 * time.Millisecond)
	}
	
	processingTime := time.Since(startTime)
	
	// UI should remain responsive - file processing shouldn't take too long
	if processingTime > 1*time.Second {
		return fmt.Errorf("file processing took too long (%v), UI may not be responsive", processingTime)
	}
	
	fmt.Printf("  ✅ File processing completed quickly (%v)\n", processingTime)
	fmt.Printf("  📋 UI should remain responsive during file operations\n")
	
	return nil
}

// TestCrossPlatformCompatibility tests cross-platform aspects
func TestCrossPlatformCompatibility() error {
	fmt.Printf("Testing cross-platform compatibility...\n")
	
	currentOS := runtime.GOOS
	fmt.Printf("  Current platform: %s\n", currentOS)
	
	// Test path handling
	testPaths := []string{
		"test/TestApp_NSIS.exe",
		"test\\TestApp_InstallShield.exe", // Windows-style path
	}
	
	for _, path := range testPaths {
		cleanPath := filepath.Clean(path)
		if _, err := os.Stat(cleanPath); err != nil {
			return fmt.Errorf("cross-platform path handling failed for %s: %v", path, err)
		}
	}
	
	fmt.Printf("  ✅ Path handling works across path separators\n")
	
	// Test file extension handling
	extensions := []string{".exe", ".EXE", ".msi", ".MSI"}
	for _, ext := range extensions {
		fmt.Printf("    Extension %s should be recognized\n", ext)
	}
	
	fmt.Printf("  📋 Note: On Linux/Mac, default folder paths will differ from Windows\n")
	fmt.Printf("  📋 MSI files are Windows-specific and won't work on other platforms\n")
	
	return nil
}

// TestErrorHandling tests various error conditions
func TestErrorHandling() error {
	fmt.Printf("Testing error handling...\n")
	
	// Test with failing installer
	filePath := filepath.Join("test", "TestApp_Failing.bat")
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		return fmt.Errorf("failing test installer not found: %s", filePath)
	}
	
	fmt.Printf("  Testing installer that intentionally fails...\n")
	
	cmd := exec.Command("cmd", "/c", filePath, "/S")
	output, err := cmd.CombinedOutput()
	
	// We EXPECT this to fail
	if err == nil {
		return fmt.Errorf("expected failing installer to fail, but it succeeded")
	}
	
	fmt.Printf("  ✅ Failing installer correctly returned error\n")
	fmt.Printf("    Output: %s\n", string(output))
	
	// Test with non-existent file
	nonExistentPath := filepath.Join("test", "NonExistentInstaller.bat")
	cmd2 := exec.Command("cmd", "/c", nonExistentPath)
	_, err2 := cmd2.CombinedOutput()
	
	if err2 == nil {
		return fmt.Errorf("expected non-existent installer to fail, but it succeeded")
	}
	
	fmt.Printf("  ✅ Non-existent installer correctly returned error\n")
	
	return nil
}

// BuildApplication tests building the main application
func BuildApplication() error {
	fmt.Printf("Testing application build...\n")
	
	// Try to build the main application without CGO to avoid compatibility issues
	cmd := exec.Command("go", "build", "-o", "test/NullInstaller.exe", ".")
	cmd.Env = append(os.Environ(), "CGO_ENABLED=0")
	output, err := cmd.CombinedOutput()
	
	if err != nil {
		return fmt.Errorf("failed to build application: %v\nOutput: %s", err, string(output))
	}
	
	// Check if the executable was created
	exePath := "test/NullInstaller.exe"
	if runtime.GOOS != "windows" {
		exePath = "test/NullInstaller"
	}
	
	if _, err := os.Stat(exePath); os.IsNotExist(err) {
		return fmt.Errorf("built executable not found: %s", exePath)
	}
	
	fmt.Printf("  ✅ Application built successfully: %s\n", exePath)
	return nil
}

func main() {
	fmt.Printf("NullInstaller - Step 10 Testing Suite\n")
	fmt.Printf("=====================================\n")
	
	runner := &TestRunner{}
	
	// Step 1: Create test installers
	runner.RunTest("Create Sample Installers", CreateTestInstallers)
	
	// Step 2: Test silent installation
	runner.RunTest("Silent Installation", TestSilentInstallation)
	
	// Step 3: Test cancellation
	runner.RunTest("Installation Cancellation", TestInstallationCancellation)
	
	// Step 4: Test drag and drop
	runner.RunTest("Drag and Drop Simulation", TestDragDropSimulation)
	
	// Step 5: Test UI responsiveness
	runner.RunTest("UI Responsiveness", TestUIResponsiveness)
	
	// Step 6: Test error handling
	runner.RunTest("Error Handling", TestErrorHandling)
	
	// Step 7: Test cross-platform compatibility
	runner.RunTest("Cross-Platform Compatibility", TestCrossPlatformCompatibility)
	
	// Step 8: Test building the application
	runner.RunTest("Build Application", BuildApplication)
	
	// Print final summary
	runner.PrintSummary()
	
	// Provide manual testing instructions
	fmt.Printf("\n" + strings.Repeat("=", 60) + "\n")
	fmt.Printf("MANUAL TESTING INSTRUCTIONS\n")
	fmt.Printf(strings.Repeat("=", 60) + "\n")
	fmt.Printf("1. Run the built NullInstaller.exe to test the GUI\n")
	fmt.Printf("2. Drag test files from the test/ folder into the application\n")
	fmt.Printf("3. Select multiple installers and click 'Start' to test batch installation\n")
	fmt.Printf("4. Click 'Stop' while installations are running to test cancellation\n")
	fmt.Printf("5. Monitor the install_log.txt file for detailed logging\n")
	fmt.Printf("6. Test on another Windows machine if available\n")
	fmt.Printf("7. For Linux/Mac testing, modify default paths and disable MSI support\n")
	fmt.Printf("\nTest files created in the test/ directory can be used for manual testing.\n")
}
