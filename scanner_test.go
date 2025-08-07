package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// TestDirectoryScanning tests the basic logic of our directory scanning
func TestDirectoryScanning(t *testing.T) {
	// Create a temporary directory for testing
	tempDir := t.TempDir()

	// Create test files
	testFiles := []string{
		"installer1.exe",
		"installer2.msi",
		"document.txt", // Should be ignored
		"script.bat",   // Should be ignored
		"setup.EXE",    // Should be detected (case insensitive)
	}

	for _, filename := range testFiles {
		testFile := filepath.Join(tempDir, filename)
		file, err := os.Create(testFile)
		if err != nil {
			t.Fatalf("Failed to create test file %s: %v", filename, err)
		}
		file.WriteString("test content")
		file.Close()
	}

	// Scan the directory using similar logic to our main function
	var foundFiles []string
	err := filepath.WalkDir(tempDir, func(path string, d os.DirEntry, err error) error {
		if err != nil {
			return nil
		}

		if !d.IsDir() {
			ext := strings.ToLower(filepath.Ext(d.Name()))
			if ext == ".exe" || ext == ".msi" {
				foundFiles = append(foundFiles, filepath.Base(path))
			}
		}

		return nil
	})

	if err != nil {
		t.Fatalf("Error walking directory: %v", err)
	}

	// Verify we found the correct files
	expectedFiles := []string{"installer1.exe", "installer2.msi", "setup.EXE"}
	if len(foundFiles) != len(expectedFiles) {
		t.Errorf("Expected %d files, found %d", len(expectedFiles), len(foundFiles))
	}

	for _, expected := range expectedFiles {
		found := false
		for _, actual := range foundFiles {
			if actual == expected {
				found = true
				break
			}
		}
		if !found {
			t.Errorf("Expected file %s not found in results", expected)
		}
	}
}

// TestFormatFileSize tests our file size formatting function
func TestFormatFileSize(t *testing.T) {
	ui := &UI{}

	testCases := []struct {
		bytes    int64
		expected string
	}{
		{0, "0 B"},
		{500, "500 B"},
		{1024, "1.0 KB"},
		{1536, "1.5 KB"},
		{1048576, "1.0 MB"},
		{1073741824, "1.0 GB"},
	}

	for _, tc := range testCases {
		result := ui.formatFileSize(tc.bytes)
		if result != tc.expected {
			t.Errorf("formatFileSize(%d) = %s, expected %s", tc.bytes, result, tc.expected)
		}
	}
}
