package main

import (
	"bytes"
	"context"
	"fmt"
	"log"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"fyne.io/fyne/v2"
)

// InstallerEngine manages sequential execution of installers
type InstallerEngine struct {
	queue chan *InstallerItem
	stop  chan struct{}

	// Internal state
	ctx     context.Context
	cancel  context.CancelFunc
	running bool
	mutex   sync.RWMutex

	// State write mutex for concurrency safety
	stateMutex sync.Mutex

	// Logger for installation attempts
	logger  *log.Logger
	logFile *os.File

	// UI references for progress updates
	progressBar      *fyne.ProgressBar
	statusLabel      *fyne.Label
	progressCallback func()               // Callback to update overall progress
	statusCallback   func(string, string) // Callback for status updates (path, status)
}

// NewInstallerEngine creates a new installer engine
func NewInstallerEngine(progressBar *fyne.ProgressBar, statusLabel *fyne.Label) *InstallerEngine {
	engine := &InstallerEngine{
		queue:       make(chan *InstallerItem, 100), // Buffer for queued installers
		stop:        make(chan struct{}),
		progressBar: progressBar,
		statusLabel: statusLabel,
	}

	// Initialize logging to install_log.txt in project root
	engine.setupLogging()

	return engine
}

// SetProgressCallback sets the callback function for progress updates
func (engine *InstallerEngine) SetProgressCallback(callback func()) {
	engine.progressCallback = callback
}

// SetStatusCallback sets the callback function for status updates
func (engine *InstallerEngine) SetStatusCallback(callback func(string, string)) {
	engine.statusCallback = callback
}

// setupLogging initializes the logger to write to install_log.txt in project root
func (engine *InstallerEngine) setupLogging() {
	// Create or open install_log.txt in the project root
	logFile, err := os.OpenFile("install_log.txt", os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0666)
	if err != nil {
		// Fallback to stdout if we can't create the log file
		engine.logger = log.New(os.Stdout, "[INSTALLER] ", log.LstdFlags)
		return
	}

	engine.logFile = logFile
	engine.logger = log.New(logFile, "[INSTALLER] ", log.LstdFlags)
	engine.logger.Printf("Installer engine initialized at %v", time.Now())
}

// Close closes the log file and cleans up resources
func (engine *InstallerEngine) Close() {
	if engine.logFile != nil {
		engine.logger.Printf("Installer engine shutting down at %v", time.Now())
		engine.logFile.Close()
	}
}

// Start begins the installation engine
func (engine *InstallerEngine) Start() {
	engine.mutex.Lock()
	defer engine.mutex.Unlock()

	if engine.running {
		return // Already running
	}

	engine.ctx, engine.cancel = context.WithCancel(context.Background())
	engine.running = true

	// Start the main installation goroutine
	go engine.processInstallations()
}

// Stop halts the installation engine and cancels running installations
func (engine *InstallerEngine) Stop() {
	engine.mutex.Lock()
	defer engine.mutex.Unlock()

	if !engine.running {
		return // Already stopped
	}

	engine.logger.Printf("Stopping installer engine")

	// Cancel current context to stop running installations
	if engine.cancel != nil {
		engine.cancel()
	}

	// Signal stop
	close(engine.stop)
	engine.running = false

	// Mark remaining queued items as cancelled
	go engine.cancelQueuedItems()
}

// Queue adds an installer to the installation queue
func (engine *InstallerEngine) Queue(item *InstallerItem) {
	engine.mutex.RLock()
	running := engine.running
	engine.mutex.RUnlock()

	if !running {
		// Thread-safe state write
		engine.stateMutex.Lock()
		item.Status = "Engine not running"
		engine.stateMutex.Unlock()
		engine.logger.Printf("Failed to queue %s: engine not running", item.Path)
		return
	}

	// Thread-safe state writes
	engine.stateMutex.Lock()
	item.Status = "Queued"
	item.Progress = "Waiting in queue"
	engine.stateMutex.Unlock()

	engine.logger.Printf("Queued installer: %s", item.Path)

	select {
	case engine.queue <- item:
		// Successfully queued
		engine.logger.Printf("Successfully added %s to installation queue", item.Path)
	case <-engine.ctx.Done():
		engine.stateMutex.Lock()
		item.Status = "Cancelled"
		item.Progress = "Installation engine was stopped"
		engine.stateMutex.Unlock()
		engine.logger.Printf("Cancelled queueing of %s: engine stopped", item.Path)
	}
}

// IsRunning returns whether the engine is currently running
func (engine *InstallerEngine) IsRunning() bool {
	engine.mutex.RLock()
	defer engine.mutex.RUnlock()
	return engine.running
}

// processInstallations is the main goroutine that processes installations sequentially
func (engine *InstallerEngine) processInstallations() {
	defer func() {
		engine.mutex.Lock()
		engine.running = false
		engine.mutex.Unlock()
	}()

	for {
		select {
		case <-engine.stop:
			return

		case <-engine.ctx.Done():
			return

		case item := <-engine.queue:
			if item == nil {
				continue
			}

			// Process this installation
			engine.executeInstaller(item)
		}
	}
}

// executeInstaller executes a single installer with progress updates
func (engine *InstallerEngine) executeInstaller(item *InstallerItem) {
	// Thread-safe state writes
	engine.stateMutex.Lock()
	item.Status = "Installing"
	item.Progress = "Determining installation flags..."
	engine.stateMutex.Unlock()

	engine.logger.Printf("Starting installation: %s", item.Path)

	// Update UI through callback if available
	if engine.statusCallback != nil {
		engine.statusCallback(item.Path, "Installing")
	}

	engine.updateUI(fmt.Sprintf("Installing %s", filepath.Base(item.Path)))

	// Determine silent installation flags
	cmd, args := engine.determineSilentFlags(item.Path)
	if cmd == "" {
		engine.stateMutex.Lock()
		item.Status = "Failed"
		item.Progress = "Could not determine installation command"
		engine.stateMutex.Unlock()

		engine.logger.Printf("FAILED: %s - Unknown installer type", item.Path)

		// Update UI through callback if available
		if engine.statusCallback != nil {
			engine.statusCallback(item.Path, "Failed")
		}

		engine.updateUI(fmt.Sprintf("Failed to install %s: Unknown installer type", filepath.Base(item.Path)))
		return
	}

	engine.logger.Printf("Executing command: %s %v", cmd, args)

	// Thread-safe state write
	engine.stateMutex.Lock()
	item.Progress = "Starting installation process..."
	engine.stateMutex.Unlock()

	// Execute the installer with context for cancellation
	execCmd := exec.CommandContext(engine.ctx, cmd, args...)

	// Capture stdout and stderr for logging
	var stdout, stderr bytes.Buffer
	execCmd.Stdout = &stdout
	execCmd.Stderr = &stderr

	// Thread-safe state write
	engine.stateMutex.Lock()
	item.Progress = "Installation in progress..."
	engine.stateMutex.Unlock()

	// Start the command
	err := execCmd.Start()
	if err != nil {
		engine.stateMutex.Lock()
		item.Status = "Failed"
		item.Progress = fmt.Sprintf("Failed to start: %v", err)
		engine.stateMutex.Unlock()

		engine.logger.Printf("FAILED TO START: %s - Error: %v", item.Path, err)

		// Update UI through callback if available
		if engine.statusCallback != nil {
			engine.statusCallback(item.Path, "Failed")
		}

		engine.updateUI(fmt.Sprintf("Failed to start %s: %v", filepath.Base(item.Path), err))
		return
	}

	// Wait for completion or cancellation
	done := make(chan error, 1)
	go func() {
		done <- execCmd.Wait()
	}()

	select {
	case <-engine.ctx.Done():
		// Installation was cancelled
		engine.stateMutex.Lock()
		item.Status = "Cancelled"
		item.Progress = "Installation was cancelled"
		engine.stateMutex.Unlock()

		engine.logger.Printf("CANCELLED: %s", item.Path)

		// Update UI through callback if available
		if engine.statusCallback != nil {
			engine.statusCallback(item.Path, "Cancelled")
		}

		engine.updateUI(fmt.Sprintf("Cancelled installation of %s", filepath.Base(item.Path)))

		// Try to kill the process if it's still running
		if execCmd.Process != nil {
			execCmd.Process.Kill()
		}
		return

	case err := <-done:
		// Log stdout and stderr for troubleshooting
		stdoutStr := stdout.String()
		stderrStr := stderr.String()

		if stdoutStr != "" {
			engine.logger.Printf("STDOUT for %s:\n%s", item.Path, stdoutStr)
		}
		if stderrStr != "" {
			engine.logger.Printf("STDERR for %s:\n%s", item.Path, stderrStr)
		}

		// Installation completed
		if err != nil {
			if exitError, ok := err.(*exec.ExitError); ok {
				exitCode := exitError.ExitCode()
				engine.stateMutex.Lock()
				item.Status = "Failed"
				item.Progress = fmt.Sprintf("Installation failed with exit code %d", exitCode)
				engine.stateMutex.Unlock()

				engine.logger.Printf("FAILED: %s - Exit code: %d, STDERR: %s", item.Path, exitCode, stderrStr)

				// Update UI through callback if available
				if engine.statusCallback != nil {
					engine.statusCallback(item.Path, "Failed")
				}

				engine.updateUI(fmt.Sprintf("Failed to install %s (exit code %d)", filepath.Base(item.Path), exitCode))
			} else {
				engine.stateMutex.Lock()
				item.Status = "Failed"
				item.Progress = fmt.Sprintf("Installation failed: %v", err)
				engine.stateMutex.Unlock()

				engine.logger.Printf("FAILED: %s - Error: %v, STDERR: %s", item.Path, err, stderrStr)

				// Update UI through callback if available
				if engine.statusCallback != nil {
					engine.statusCallback(item.Path, "Failed")
				}

				engine.updateUI(fmt.Sprintf("Failed to install %s: %v", filepath.Base(item.Path), err))
			}
		} else {
			engine.stateMutex.Lock()
			item.Status = "Completed"
			item.Progress = "Installation completed successfully"
			engine.stateMutex.Unlock()

			engine.logger.Printf("COMPLETED: %s - Installation successful", item.Path)

			// Update UI through callback if available
			if engine.statusCallback != nil {
				engine.statusCallback(item.Path, "Completed")
			}

			engine.updateUI(fmt.Sprintf("Successfully installed %s", filepath.Base(item.Path)))
		}
	}
}

// determineSilentFlags determines the appropriate silent installation flags for a file
func (engine *InstallerEngine) determineSilentFlags(filePath string) (string, []string) {
	ext := strings.ToLower(filepath.Ext(filePath))

	switch ext {
	case ".msi":
		// MSI files use msiexec
		return "msiexec", []string{"/i", filePath, "/qn", "/norestart"}

	case ".exe":
		// EXE files - try different silent flags until one works
		silentFlags := [][]string{
			{"/S"},          // NSIS installers
			{"/silent"},     // InstallShield and others
			{"/qn"},         // Some MSI-based exe installers
			{"/quiet"},      // Various installers
			{"/VERYSILENT"}, // Inno Setup installers
		}

		// For demonstration, we'll return the first option
		// In a real implementation, you might want to try each one
		// or detect the installer type more precisely
		return filePath, silentFlags[0]

	default:
		// Unknown file type
		return "", nil
	}
}

// tryExecuteWithFlags attempts to execute an installer with specific flags
// This could be used for EXE files to try different silent flag combinations
func (engine *InstallerEngine) tryExecuteWithFlags(filePath string, flagSets [][]string) error {
	for _, flags := range flagSets {
		cmd := exec.CommandContext(engine.ctx, filePath, flags...)

		// Try to execute with a short timeout to test if flags are accepted
		ctx, cancel := context.WithTimeout(engine.ctx, 10*time.Second)
		cmd = exec.CommandContext(ctx, filePath, flags...)

		err := cmd.Run()
		cancel()

		// If the context was cancelled due to engine stop, return immediately
		select {
		case <-engine.ctx.Done():
			return engine.ctx.Err()
		default:
		}

		// If command executed successfully (exit code 0), use these flags
		if err == nil {
			// Now run the full installation with these flags
			fullCmd := exec.CommandContext(engine.ctx, filePath, flags...)
			return fullCmd.Run()
		}

		// If it's an exit error but not a "bad flag" error, these flags might work
		// This is a heuristic - in practice, you'd need more sophisticated detection
		if exitError, ok := err.(*exec.ExitError); ok {
			exitCode := exitError.ExitCode()
			// Some installers return specific codes for bad parameters vs other errors
			// This is installer-specific and would need more research
			if exitCode != 1 && exitCode != 87 { // Common "bad parameter" codes
				// Try the full installation
				fullCmd := exec.CommandContext(engine.ctx, filePath, flags...)
				return fullCmd.Run()
			}
		}
	}

	// No suitable flags found
	return fmt.Errorf("no suitable silent installation flags found")
}

// cancelQueuedItems marks all remaining items in the queue as cancelled
func (engine *InstallerEngine) cancelQueuedItems() {
	for {
		select {
		case item := <-engine.queue:
			if item != nil {
				// Thread-safe state writes
				engine.stateMutex.Lock()
				item.Status = "Cancelled"
				item.Progress = "Installation was cancelled before execution"
				engine.stateMutex.Unlock()

				engine.logger.Printf("CANCELLED (queued): %s", item.Path)
			}
		default:
			return // Queue is empty
		}
	}
}

// updateUI updates the global progress bar and status label
func (engine *InstallerEngine) updateUI(message string) {
	if engine.statusLabel != nil {
		engine.statusLabel.SetText(message)
	}

	// Send notification through Fyne's notification system
	if fyne.CurrentApp() != nil {
		fyne.CurrentApp().SendNotification(&fyne.Notification{
			Title:   "NullInstaller",
			Content: message,
		})
	}
}

// GetQueueLength returns the number of items currently in the queue
func (engine *InstallerEngine) GetQueueLength() int {
	return len(engine.queue)
}
