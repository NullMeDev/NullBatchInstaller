package main

import (
	"fmt"
	"fyne.io/fyne/v2"
	"fyne.io/fyne/v2/app"
	"fyne.io/fyne/v2/container"
	"fyne.io/fyne/v2/data/binding"
	"fyne.io/fyne/v2/storage"
	"fyne.io/fyne/v2/theme"
	"fyne.io/fyne/v2/widget"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

// UI holds all the UI components
type UI struct {
	window        fyne.Window
	installerList *widget.Form
	startButton   *widget.Button
	stopButton    *widget.Button
	clearButton   *widget.Button
	progressBar   *widget.ProgressBar
	statusLabel   *widget.Label
	installers    []InstallerItem
	installerMap  map[string]bool // For duplicate prevention
	dropZone      *DropZone
	engine        *InstallerEngine // Autonomous installation engine

	// Data bindings for progress reporting
	overallProgress binding.Float
	overallStatus   binding.String
	totalSelected   int
	completedItems  int
	mutex           sync.RWMutex // For thread-safe access to progress data
}

// InstallerItem represents an installer with its checkbox
type InstallerItem struct {
	Path        string
	Size        int64
	Checkbox    *widget.Check
	StatusLabel *widget.Label // New label for status display
	Selected    bool
	Status      string
	Progress    string // New field for detailed progress information

	// Data bindings for this item
	statusBinding binding.String
}

// DropZone represents a drag & drop area for files
type DropZone struct {
	widget.BaseWidget
	ui       *UI
	label    *widget.Label
	onDrop   func([]string)
	hovering bool
}

// NewDropZone creates a new drag & drop zone
func NewDropZone(ui *UI) *DropZone {
	dz := &DropZone{
		ui:    ui,
		label: widget.NewLabel("Drop installer files here (.exe, .msi)"),
	}
	dz.ExtendBaseWidget(dz)
	return dz
}

// CreateRenderer creates the renderer for the drop zone
func (dz *DropZone) CreateRenderer() fyne.WidgetRenderer {
	dz.label.Alignment = fyne.TextAlignCenter
	return widget.NewSimpleRenderer(container.NewBorder(
		nil, nil, nil, nil,
		container.NewCenter(dz.label),
	))
}

// Dragged handles drag events
func (dz *DropZone) Dragged(de *fyne.DragEvent) {
	// Visual feedback when dragging over the drop zone
	if !dz.hovering {
		dz.hovering = true
		dz.label.SetText("Release to drop files here")
		dz.Refresh()
	}
}

// DragEnd handles the end of drag events (file drop)
func (dz *DropZone) DragEnd() {
	dz.hovering = false
	dz.label.SetText("Drop installer files here (.exe, .msi)")
	dz.Refresh()

	// Note: In a real implementation, we would extract file URIs from the drag event
	// For demonstration, we'll simulate dropped files
	// In practice, this would come from the drag event's data
	droppedFiles := dz.getDroppedFiles() // This would be implemented based on platform
	if len(droppedFiles) > 0 {
		dz.processDroppedFiles(droppedFiles)
	}
}

// getDroppedFiles simulates getting file paths from drag event
// In a real implementation, this would extract URIs from the drag event
func (dz *DropZone) getDroppedFiles() []string {
	// This is a simulation - in practice, file paths would come from the drag event
	// The platform-specific drag & drop handler would populate this with actual file URIs
	// For demonstration purposes, this function would be replaced with actual
	// platform callbacks that extract file URIs from the native drag event
	return []string{}
}

// handleFileURIs processes file URIs from drag & drop events
// This method demonstrates how file URIs would be converted to file paths
func (dz *DropZone) handleFileURIs(uris []fyne.URI) {
	var filePaths []string

	for _, uri := range uris {
		// Convert URI to file path
		if uri.Scheme() == "file" {
			// Extract the file path from the URI
			filePath := uri.Path()
			if filePath != "" {
				filePaths = append(filePaths, filePath)
			}
		}
	}

	if len(filePaths) > 0 {
		dz.processDroppedFiles(filePaths)
	}
}

// processDroppedFiles handles the processing of dropped files
func (dz *DropZone) processDroppedFiles(filePaths []string) {
	addedCount := 0
	skippedCount := 0

	for _, filePath := range filePaths {
		// Verify file existence
		if _, err := os.Stat(filePath); os.IsNotExist(err) {
			skippedCount++
			continue
		}

		// Filter by extension
		ext := strings.ToLower(filepath.Ext(filePath))
		if ext != ".exe" && ext != ".msi" {
			skippedCount++
			continue
		}

		// Check for duplicates using map lookup
		if dz.ui.installerMap[filePath] {
			skippedCount++
			continue
		}

		// Add to installer list
		if dz.ui.addInstallerFromPath(filePath) {
			addedCount++
		}
	}

	// Update status
	if addedCount > 0 {
		dz.ui.refreshInstallerList()
		message := fmt.Sprintf("Added %d file(s)", addedCount)
		if skippedCount > 0 {
			message += fmt.Sprintf(", skipped %d", skippedCount)
		}
		dz.ui.overallStatus.Set(message)
	} else if skippedCount > 0 {
		dz.ui.overallStatus.Set(fmt.Sprintf("Skipped %d file(s) (duplicates or invalid)", skippedCount))
	}
}

// setupUI initializes and configures all UI components
func (ui *UI) setupUI() {
	// Initialize installer map for duplicate prevention
	ui.installerMap = make(map[string]bool)

	// Initialize data bindings for progress reporting
	ui.overallProgress = binding.NewFloat()
	ui.overallStatus = binding.NewString()
	ui.overallProgress.Set(0.0)
	ui.overallStatus.Set("Ready")

	// Create progress bar and status label with data bindings
	ui.progressBar = widget.NewProgressBarWithData(ui.overallProgress)
	ui.statusLabel = widget.NewLabelWithData(ui.overallStatus)
	ui.statusLabel.Wrapping = fyne.TextWrapWord

	// Initialize the installer engine with progress callback
	ui.engine = NewInstallerEngine(ui.progressBar, ui.statusLabel)
	ui.engine.SetProgressCallback(ui.updateOverallProgress)
	ui.engine.SetStatusCallback(ui.updateItemStatus)

	// Scan default folder for executables
	ui.scanDefaultFolder()

	// Create left panel components
	leftPanelLabel := widget.NewLabel("Available Installers")
	leftPanelLabel.TextStyle.Bold = true

	// Create form for installer checkboxes
	ui.installerList = widget.NewForm()
	ui.populateInstallerList()

	// Wrap installer list in scroll container
	installerScroll := container.NewScroll(ui.installerList)
	installerScroll.SetMinSize(fyne.NewSize(250, 300))

	// Create drag & drop zone
	ui.dropZone = NewDropZone(ui)
	ui.dropZone.Resize(fyne.NewSize(250, 80))

	// Create left panel with VBox container
	leftPanel := container.NewVBox(
		leftPanelLabel,
		installerScroll,
		widget.NewSeparator(),
		ui.dropZone,
	)

	// Create control buttons
	ui.startButton = widget.NewButton("Start", ui.onStartClicked)
	ui.startButton.Importance = widget.HighImportance

	ui.stopButton = widget.NewButton("Stop", ui.onStopClicked)
	ui.stopButton.Importance = widget.MediumImportance
	ui.stopButton.Disable() // Initially disabled

	ui.clearButton = widget.NewButton("Clear", ui.onClearClicked)
	ui.clearButton.Importance = widget.LowImportance

	// Create button container
	buttonContainer := container.NewHBox(
		ui.startButton,
		ui.stopButton,
		ui.clearButton,
	)

	// Create bottom panel
	bottomPanel := container.NewVBox(
		ui.progressBar,
		ui.statusLabel,
	)

	// Create right panel with controls
	rightPanel := container.NewVBox(
		buttonContainer,
		widget.NewSeparator(),
		bottomPanel,
	)

	// Create main layout using border layout
	mainContent := container.NewBorder(
		nil,        // top
		nil,        // bottom
		leftPanel,  // left
		nil,        // right
		rightPanel, // center (acts as right panel)
	)

	// Configure window
	ui.window.SetContent(mainContent)
	ui.window.Resize(fyne.NewSize(800, 600))
	ui.window.CenterOnScreen()

	// Enable drag and drop on the main window
	ui.setupWindowDragDrop()

	// Add test drag & drop functionality for demonstration
	ui.addTestDragDropHandling()
}

// scanDefaultFolder walks the default directory and finds .exe and .msi files
func (ui *UI) scanDefaultFolder() {
	defaultPath := `C:\Users\Administrator\Desktop\Down`
	var installerItems []*InstallerItem

	err := filepath.WalkDir(defaultPath, func(path string, d os.DirEntry, err error) error {
		if err != nil {
			return nil // Continue walking even if there's an error with this file/dir
		}

		// Check if file ends with .exe or .msi
		if !d.IsDir() {
			ext := strings.ToLower(filepath.Ext(d.Name()))
			if ext == ".exe" || ext == ".msi" {
				// Get file info for size
				info, err := d.Info()
				if err != nil {
					return nil // Continue if we can't get file info
				}

				// Create installer item with data binding
				item := &InstallerItem{
					Path:          path,
					Size:          info.Size(),
					Selected:      false,
					Status:        "Ready",
					statusBinding: binding.NewString(),
				}
				item.statusBinding.Set("Ready")

				// Create status label with data binding
				item.StatusLabel = widget.NewLabelWithData(item.statusBinding)
				item.StatusLabel.TextStyle.Monospace = true

				// Create checkbox with proper closure
				fileName := filepath.Base(path)
				sizeText := ui.formatFileSize(info.Size())
				labelText := fmt.Sprintf("%s (%s)", fileName, sizeText)

				// Capture item in closure for checkbox callback
				capturedItem := item
				item.Checkbox = widget.NewCheck(labelText, func(checked bool) {
					capturedItem.Selected = checked
					ui.onInstallerToggled(capturedItem.Path, checked)
				})

				installerItems = append(installerItems, item)
			}
		}

		return nil
	})

	if err != nil {
		ui.overallStatus.Set(fmt.Sprintf("Error scanning folder: %v", err))
		return
	}

	// Convert []*InstallerItem to []InstallerItem and populate map
	ui.installers = make([]InstallerItem, len(installerItems))
	for i, item := range installerItems {
		ui.installers[i] = *item
		// Add to map for duplicate prevention
		ui.installerMap[item.Path] = true
	}

	if len(ui.installers) == 0 {
		ui.overallStatus.Set("No .exe or .msi files found in the default folder")
	} else {
		ui.overallStatus.Set(fmt.Sprintf("Found %d installer files", len(ui.installers)))
	}
}

// populateInstallerList adds installer checkboxes and status labels to the form
func (ui *UI) populateInstallerList() {
	for i := range ui.installers {
		// Create a container with checkbox and status label
		itemContainer := container.NewHBox(
			ui.installers[i].Checkbox,
			ui.installers[i].StatusLabel,
		)
		ui.installerList.Append("", itemContainer)
	}
}

// setupWindowDragDrop configures drag and drop for the main window
func (ui *UI) setupWindowDragDrop() {
	// Note: This sets up the window to accept drag and drop events
	// The actual file URI extraction would be handled by the platform-specific code
	// For demonstration, we'll also add a file dialog button as backup
	addFileButton := widget.NewButton("Add Files (Alternative)", ui.onAddFiles)
	addFileButton.Importance = widget.LowImportance

	// Add the button to the existing left panel
	if content := ui.window.Content(); content != nil {
		if border, ok := content.(*container.Border); ok {
			if leftPanel, ok := border.Objects[0].(*container.VBox); ok {
				leftPanel.Add(addFileButton)
			}
		}
	}
}

// addInstallerFromPath adds a single installer from a file path
func (ui *UI) addInstallerFromPath(filePath string) bool {
	// Get file info
	info, err := os.Stat(filePath)
	if err != nil {
		return false
	}

	// Create installer item with data binding
	item := InstallerItem{
		Path:          filePath,
		Size:          info.Size(),
		Selected:      false,
		Status:        "Ready",
		statusBinding: binding.NewString(),
	}
	item.statusBinding.Set("Ready")

	// Create status label with data binding
	item.StatusLabel = widget.NewLabelWithData(item.statusBinding)
	item.StatusLabel.TextStyle.Monospace = true

	// Create checkbox with proper closure
	fileName := filepath.Base(filePath)
	sizeText := ui.formatFileSize(info.Size())
	labelText := fmt.Sprintf("%s (%s)", fileName, sizeText)

	// Capture item in closure for checkbox callback
	capturedItem := &item
	item.Checkbox = widget.NewCheck(labelText, func(checked bool) {
		capturedItem.Selected = checked
		ui.onInstallerToggled(capturedItem.Path, checked)
	})

	// Add to list and map
	ui.installers = append(ui.installers, item)
	ui.installerMap[filePath] = true

	return true
}

// refreshInstallerList updates the installer list UI
func (ui *UI) refreshInstallerList() {
	// Clear existing form items
	ui.installerList.Items = nil

	// Re-populate with all installers
	for i := range ui.installers {
		// Create a container with checkbox and status label
		itemContainer := container.NewHBox(
			ui.installers[i].Checkbox,
			ui.installers[i].StatusLabel,
		)
		ui.installerList.Append("", itemContainer)
	}

	// Refresh the form
	ui.installerList.Refresh()

	// Update status
	ui.overallStatus.Set(fmt.Sprintf("Total: %d installer files", len(ui.installers)))
}

// simulateFileDrop simulates dropping files for testing purposes
func (ui *UI) simulateFileDrop(filePaths []string) {
	if ui.dropZone != nil {
		// Convert file paths to URIs for demonstration
		var uris []fyne.URI
		for _, path := range filePaths {
			uri := storage.NewFileURI(path)
			uris = append(uris, uri)
		}

		// Process through URI handler (demonstrates the proper flow)
		ui.dropZone.handleFileURIs(uris)
	}
}

// addTestDragDropHandling demonstrates how real drag & drop would be implemented
// This function shows the structure needed for platform-specific integration
func (ui *UI) addTestDragDropHandling() {
	// In a real implementation, this is where you would:
	// 1. Register platform-specific drag & drop callbacks
	// 2. Set up native window event handlers
	// 3. Configure the window to accept dropped files

	// Example of what the platform callback would look like:
	// platform.SetDropCallback(ui.window, func(paths []string) {
	//     var uris []fyne.URI
	//     for _, path := range paths {
	//         uris = append(uris, storage.NewFileURI(path))
	//     }
	//     ui.dropZone.handleFileURIs(uris)
	// })

	// For now, we'll add a test button to simulate this functionality
	testButton := widget.NewButton("Test Drag & Drop", func() {
		ui.onAddFiles() // This simulates the drag & drop
	})
	testButton.Importance = widget.MediumImportance

	// Add to UI
	if content := ui.window.Content(); content != nil {
		if border, ok := content.(*container.Border); ok {
			if leftPanel, ok := border.Objects[0].(*container.VBox); ok {
				leftPanel.Add(testButton)
			}
		}
	}
}

// Event handlers
func (ui *UI) onStartClicked() {
	selectedCount := ui.getSelectedInstallersCount()
	if selectedCount == 0 {
		ui.overallStatus.Set("No installers selected")
		return
	}

	// Initialize progress tracking
	ui.mutex.Lock()
	ui.totalSelected = selectedCount
	ui.completedItems = 0
	ui.mutex.Unlock()

	// Reset all selected items to queued status
	for i := range ui.installers {
		if ui.installers[i].Selected {
			ui.updateItemStatus(ui.installers[i].Path, "Queued")
		}
	}

	// Start the installer engine
	ui.engine.Start()

	// Queue selected installers for installation
	for i := range ui.installers {
		if ui.installers[i].Selected {
			// Queue this installer item (use pointer to allow updates)
			ui.engine.Queue(&ui.installers[i])
		}
	}

	ui.overallStatus.Set("Installation process started...")
	ui.updateOverallProgress()
	ui.startButton.Disable()
	ui.stopButton.Enable()
}

func (ui *UI) onStopClicked() {
	// Stop the installer engine
	ui.engine.Stop()

	// Update status for any running/queued items
	for i := range ui.installers {
		if ui.installers[i].Selected && (ui.installers[i].Status == "Installing" || ui.installers[i].Status == "Queued") {
			ui.updateItemStatus(ui.installers[i].Path, "Cancelled")
		}
	}

	ui.overallStatus.Set("Installation stopped")
	ui.startButton.Enable()
	ui.stopButton.Disable()
	ui.overallProgress.Set(0.0)
}

func (ui *UI) onClearClicked() {
	// Clear all selections and reset statuses
	for i := range ui.installers {
		ui.installers[i].Checkbox.SetChecked(false)
		ui.installers[i].Selected = false
		ui.updateItemStatus(ui.installers[i].Path, "Ready")
	}

	// Reset progress tracking
	ui.mutex.Lock()
	ui.totalSelected = 0
	ui.completedItems = 0
	ui.mutex.Unlock()

	ui.overallStatus.Set("All selections cleared")
	ui.overallProgress.Set(0.0)
}

func (ui *UI) onInstallerToggled(path string, checked bool) {
	for i := range ui.installers {
		if ui.installers[i].Path == path {
			ui.installers[i].Selected = checked
			break
		}
	}

	selectedCount := ui.getSelectedInstallersCount()
	ui.overallStatus.Set(fmt.Sprintf("%d installer(s) selected", selectedCount))
}

func (ui *UI) onAddFiles() {
	// For demonstration, let's create some test files to simulate drag & drop
	// In a real implementation, this would open a file dialog

	// Create some test files for demonstration
	testDir := filepath.Join(os.TempDir(), "drag_drop_test")
	os.MkdirAll(testDir, 0755)

	// Create test .exe and .msi files
	testFiles := []string{
		filepath.Join(testDir, "test_installer1.exe"),
		filepath.Join(testDir, "test_installer2.msi"),
		filepath.Join(testDir, "test_installer3.exe"),
	}

	// Create the test files
	for _, testFile := range testFiles {
		f, err := os.Create(testFile)
		if err == nil {
			f.WriteString("This is a test installer file for drag & drop demonstration.")
			f.Close()
		}
	}

	// Simulate dropping these files
	ui.simulateFileDrop(testFiles)

	ui.overallStatus.Set("Simulated drag & drop with test files - check the drop zone!")
}

// Progress update methods

// updateOverallProgress calculates and updates overall progress
func (ui *UI) updateOverallProgress() {
	ui.mutex.Lock()
	defer ui.mutex.Unlock()

	if ui.totalSelected == 0 {
		ui.overallProgress.Set(0.0)
		return
	}

	// Calculate progress as completedItems / totalSelected
	progress := float64(ui.completedItems) / float64(ui.totalSelected)
	ui.overallProgress.Set(progress)

	// Update status text
	status := fmt.Sprintf("Progress: %d/%d completed (%.1f%%)", ui.completedItems, ui.totalSelected, progress*100)
	ui.overallStatus.Set(status)
}

// updateItemStatus updates the status of a specific installer item
func (ui *UI) updateItemStatus(path string, status string) {
	for i := range ui.installers {
		if ui.installers[i].Path == path {
			ui.installers[i].Status = status
			if ui.installers[i].statusBinding != nil {
				// Update the status label with appropriate icon
				var displayStatus string
				switch status {
				case "Completed":
					displayStatus = "✔ Completed"
				case "Failed":
					displayStatus = "✖ Failed"
				case "Installing":
					displayStatus = "… Installing"
				case "Queued":
					displayStatus = "⧗ Queued"
				case "Cancelled":
					displayStatus = "⊘ Cancelled"
				default:
					displayStatus = status
				}
				ui.installers[i].statusBinding.Set(displayStatus)
			}

			// Update completed count if installation finished
			if status == "Completed" || status == "Failed" || status == "Cancelled" {
				ui.mutex.Lock()
				ui.completedItems++
				ui.mutex.Unlock()
				ui.updateOverallProgress()
			}
			break
		}
	}
}

// Helper functions
func (ui *UI) getSelectedInstallersCount() int {
	count := 0
	for _, installer := range ui.installers {
		if installer.Selected {
			count++
		}
	}
	return count
}

// formatFileSize converts bytes to human-readable format
func (ui *UI) formatFileSize(bytes int64) string {
	const unit = 1024
	if bytes < unit {
		return fmt.Sprintf("%d B", bytes)
	}
	div, exp := int64(unit), 0
	for n := bytes / unit; n >= unit; n /= unit {
		div *= unit
		exp++
	}
	return fmt.Sprintf("%.1f %cB", float64(bytes)/float64(div), "KMGTPE"[exp])
}

// simulateInstallation is no longer needed as we now use the real InstallerEngine
// The engine handles all installation progress and UI updates automatically

func main() {
	myApp := app.New()

	// Apply dark theme
	myApp.Settings().SetTheme(theme.DarkTheme())

	myWindow := myApp.NewWindow("NullInstaller")
	myWindow.SetMaster()

	// Create UI instance
	ui := &UI{
		window: myWindow,
	}

	// Setup the UI
	ui.setupUI()

	// Set up cleanup when window is closed
	myWindow.SetCloseIntercept(func() {
		// Stop the installer engine and close log file
		if ui.engine != nil {
			ui.engine.Stop()
			ui.engine.Close()
		}
		myWindow.Close()
	})

	// Show and run the application
	myWindow.ShowAndRun()
}
