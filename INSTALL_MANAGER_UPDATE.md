# InstallManager Update - Dynamic Download & Install Features

## Overview
The InstallManager has been significantly enhanced to provide dynamic download and installation capabilities with progress tracking, elevation handling, and automatic EULA acceptance.

## Key Features Implemented

### 1. Dynamic Download to %TEMP%
- Downloads are stored in temporary directories with unique identifiers
- Each download creates a folder: `%TEMP%\NullInstaller_[unique_id]`
- Temporary files are automatically cleaned up after successful installation
- Complete cleanup on application exit

### 2. HttpClient with Progress Callback
- Persistent HttpClient instance for better performance
- Real-time download progress tracking (0-100%)
- Stream-based downloading for memory efficiency
- Progress updates displayed in status label and list view
- 30-minute timeout for large downloads

### 3. Elevation Handling
- Automatic detection of administrator privileges at startup
- Smart elevation strategy:
  - If running as admin: Direct execution with full control
  - If not admin: Uses ShellExecute with "runas" verb to trigger UAC
- Fallback handling for elevation denial with user prompt
- Visual indicator in title bar when not running as admin

### 4. Silent Installation with Exit Code Logging
- Comprehensive silent switch detection based on installer type
- Custom silent switches from catalog when available
- Exit code logging for all installations
- Success codes: 0 (success) and 3010 (success, reboot required)
- Detailed logging to `install_log.txt`

### 5. Per-Item Status Display
- Visual status indicators with icons:
  - ⏳ Queued (gray)
  - 🔄 Downloading (light blue) with percentage
  - ⚙ Installing (yellow)
  - ✔ Done (light green)
  - ✖ Failed (light coral)
- Real-time status updates using thread-safe UI refresh
- Color-coded list items for quick visual feedback

### 6. Automatic EULA Acceptance
- Smart EULA acceptance for various installer types:
  - MSI: `/qn EULA=1 ACCEPT=YES ACCEPTEULA=1`
  - NSIS: `/S /ACCEPTEULA /EULA=1`
  - InnoSetup: `/VERYSILENT /SUPPRESSMSGBOXES`
  - Java: `/s AUTO_UPDATE=0 EULA=1`
  - Custom switches per application type
- Fallback to user prompt if EULA cannot be auto-accepted

### 7. Session-Based Temporary Storage
- Downloads kept only during application session
- Automatic cleanup of successful installations
- Manual cleanup on application exit
- Prevents disk space waste from accumulated downloads

## Technical Implementation

### Enhanced Classes

#### InstallerItem
```csharp
public class InstallerItem
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public long Size { get; set; }
    public string Status { get; set; }
    public ProgramEntry ProgramEntry { get; set; }
    public int DownloadProgress { get; set; }
    public bool IsDownloading { get; set; }
    public string TempFilePath { get; set; }
}
```

#### InstallStatus Enum
```csharp
public enum InstallStatus
{
    Queued,
    Downloading,
    Installing,
    Done,
    Failed
}
```

### Key Methods

#### DownloadWithProgress
- Streams download directly to file
- Updates progress in real-time
- Handles large files efficiently
- Returns success/failure status

#### InstallWithElevation
- Determines appropriate elevation method
- Applies silent switches intelligently
- Handles UAC prompts gracefully
- Logs all installation attempts and results

#### DetermineSilentSwitches
- Pattern matching for common installers
- Application-specific switch detection
- EULA acceptance parameters included
- Comprehensive fallback strategy

## Usage Guide

### Basic Workflow
1. Select programs from Software Catalog
2. Click "Download Selected"
3. System automatically:
   - Downloads to temp with progress display
   - Elevates if needed (UAC prompt)
   - Installs silently with EULA acceptance
   - Cleans up temp files
   - Updates status display

### Admin vs Non-Admin Mode
- **Admin Mode**: Full silent installation, no UAC prompts
- **Non-Admin Mode**: UAC prompts for each installer requiring elevation

### Error Handling
- Failed downloads: Marked as "Failed", can retry
- Elevation denied: Prompts user to retry with elevation
- Installation failures: Logged with exit codes for debugging

## Configuration

### Logging
- Verbose logging controlled by checkbox
- All operations logged to `install_log.txt`
- Exit codes captured for troubleshooting

### Customization
- Silent switches can be customized in catalog JSON
- EULA acceptance behavior configurable per application
- Timeout values adjustable in code

## Performance Optimizations
- Single HttpClient instance reused
- Stream-based file operations
- Asynchronous download and install
- Efficient memory usage for large files
- Parallel capability (future enhancement)

## Security Considerations
- Temporary files in user-specific temp directory
- Automatic cleanup prevents orphaned files
- UAC elevation only when necessary
- No sensitive data stored in temp files

## Future Enhancements
- Parallel downloads option
- Resume capability for interrupted downloads
- Checksum verification
- Custom installation directories
- Post-installation configuration scripts
