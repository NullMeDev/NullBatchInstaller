package main

import (
	"fmt"
	"os"
	"path/filepath"
)

// createSampleInstaller creates a mock installer file that simulates real installer behavior
func createSampleInstaller(name string, fileType string) error {
	testDir := "test"
	fileName := name + "." + fileType
	filePath := filepath.Join(testDir, fileName)
	
	// Create directory if it doesn't exist
	if err := os.MkdirAll(testDir, 0755); err != nil {
		return fmt.Errorf("failed to create test directory: %v", err)
	}
	
	// Create the mock installer file
	file, err := os.Create(filePath)
	if err != nil {
		return fmt.Errorf("failed to create sample installer %s: %v", fileName, err)
	}
	defer file.Close()
	
	// Write mock content that simulates an installer
	content := fmt.Sprintf(`@echo off
REM Mock %s installer for testing purposes
REM This is a test installer created by NullInstaller test suite
REM Installer: %s
REM Type: %s
REM Created for testing silent installation and UI responsiveness

echo Installing %s...
timeout /t 2 >nul 2>&1

REM Simulate installation process with progress
echo Progress: 25%%
timeout /t 1 >nul 2>&1
echo Progress: 50%%
timeout /t 1 >nul 2>&1
echo Progress: 75%%
timeout /t 1 >nul 2>&1
echo Progress: 100%%

echo Installation of %s completed successfully.
exit /b 0
`, fileType, fileName, fileType, name, name)
	
	_, err = file.WriteString(content)
	if err != nil {
		return fmt.Errorf("failed to write content to %s: %v", fileName, err)
	}
	
	fmt.Printf("Created sample installer: %s\n", filePath)
	return nil
}

// createMSIInstaller creates a mock MSI installer (batch file that simulates MSI behavior)
func createMSIInstaller(name string) error {
	testDir := "test"
	fileName := name + ".msi"
	filePath := filepath.Join(testDir, fileName)
	
	// For MSI files, we'll create a mock batch file that responds to msiexec parameters
	// In real testing, you would use actual MSI files
	file, err := os.Create(filePath)
	if err != nil {
		return fmt.Errorf("failed to create MSI installer %s: %v", fileName, err)
	}
	defer file.Close()
	
	// Mock MSI content (this would be binary in real MSI files)
	content := fmt.Sprintf(`REM Mock MSI installer: %s
REM This file simulates an MSI package for testing
REM In real testing, this would be an actual MSI file
REM The installer engine will call: msiexec /i "%s" /qn /norestart
`, name, fileName)
	
	_, err = file.WriteString(content)
	if err != nil {
		return fmt.Errorf("failed to write MSI content: %v", err)
	}
	
	fmt.Printf("Created mock MSI installer: %s\n", filePath)
	return nil
}

func main() {
	fmt.Println("Creating sample installers for NullInstaller testing...")
	
	// Create various EXE installers with different characteristics
	exeInstallers := []string{
		"TestApp_NSIS",           // NSIS installer (uses /S)
		"TestApp_InstallShield",  // InstallShield (uses /silent)
		"TestApp_InnoSetup",      // Inno Setup (uses /VERYSILENT)
		"TestApp_Generic",        // Generic (uses /quiet)
		"TestApp_LongRunning",    // Long-running installer for cancellation testing
	}
	
	for _, name := range exeInstallers {
		if err := createSampleInstaller(name, "exe"); err != nil {
			fmt.Printf("Error creating %s: %v\n", name, err)
		}
	}
	
	// Create MSI installers
	msiInstallers := []string{
		"TestApp_MSI_Standard",
		"TestApp_MSI_Large",
		"TestApp_MSI_Complex",
	}
	
	for _, name := range msiInstallers {
		if err := createMSIInstaller(name); err != nil {
			fmt.Printf("Error creating MSI %s: %v\n", name, err)
		}
	}
	
	fmt.Println("\nSample installer creation completed!")
	fmt.Println("Files created in the 'test' directory:")
	
	// List created files
	files, err := os.ReadDir("test")
	if err != nil {
		fmt.Printf("Error reading test directory: %v\n", err)
		return
	}
	
	for _, file := range files {
		if !file.IsDir() {
			info, err := file.Info()
			if err == nil {
				fmt.Printf("  - %s (%d bytes)\n", file.Name(), info.Size())
			}
		}
	}
}
