# NullInstaller

A Go-based autonomous installer application with drag & drop support, built using the Fyne GUI framework. NullInstaller allows you to batch install multiple .exe and .msi files with progress tracking and detailed logging.

## Features

- 🖱️ **Drag & Drop Interface**: Simply drop installer files into the application
- 📦 **Batch Installation**: Install multiple programs sequentially
- 📊 **Progress Tracking**: Real-time progress bars and status updates
- 📝 **Detailed Logging**: All installation attempts logged to `install_log.txt`
- 🎯 **Silent Installation Detection**: Automatically detects and uses silent install flags when possible
- 🛑 **Cancellable Operations**: Stop installations in progress
- 🔍 **Automatic Discovery**: Scans default download folders for installer files
- 💾 **Cross-Platform Support**: Works on Windows, Linux, and macOS

## Prerequisites

### Required
- **Go**: Version 1.22 or higher ([Download Go](https://golang.org/dl/))
- **CGO-capable compiler** (required by Fyne framework)

### Platform-Specific Requirements

#### Windows
- **GCC or MinGW**: Required for CGO compilation used by Fyne
  - **Recommended**: [TDM-GCC](https://jmeubank.github.io/tdm-gcc/) (easiest to install)
  - **Alternative**: [MinGW-w64](https://www.mingw-w64.org/) or MSYS2
  - **Enterprise**: Visual Studio Build Tools with `CGO_ENABLED=1`

#### Linux (Ubuntu/Debian)
```bash
sudo apt-get update
sudo apt-get install gcc pkg-config libgl1-mesa-dev libxcursor-dev libxrandr-dev libxinerama-dev libxi-dev libglfw3-dev libxxf86vm-dev
```

#### Linux (Fedora/RHEL/CentOS)
```bash
sudo dnf install gcc pkg-config mesa-libGL-devel libXcursor-devel libXrandr-devel libXinerama-devel libXi-devel glfw-devel libXxf86vm-devel
```

#### macOS
```bash
xcode-select --install  # Install Xcode Command Line Tools
```

## Quick Start

### Option 1: Using Build Scripts (Recommended)

**Windows (Batch Script):**
```cmd
# Build GUI version (no console window)
make.bat

# Result: dist\NullInstaller.exe
```

**Windows/Linux/macOS (PowerShell):**
```powershell
# Build Windows GUI version
.\build.ps1

# Build Windows console version (shows debug output)
.\build.ps1 -Console

# Build for other platforms
.\build.ps1 -Target linux
.\build.ps1 -Target macos

# Clean build
.\build.ps1 -Clean
```

### Option 2: Manual Build

```bash
# Basic build
go build -o NullInstaller.exe .

# Optimized build (smaller file size)
go build -ldflags "-s -w" -o NullInstaller.exe .

# Windows GUI build (no console window)
go build -ldflags "-s -w -H=windowsgui" -o NullInstaller.exe .
```

### Running the Application

```bash
# Run from source (development)
go run .

# Run built executable
.\dist\NullInstaller.exe    # Windows
./dist/NullInstaller-linux   # Linux
./dist/NullInstaller-macos   # macOS
```

## Usage

### Basic Workflow

1. **Launch NullInstaller**: Double-click the executable
2. **Add Installer Files**: 
   - Drag & drop .exe or .msi files into the application window
   - Or use "Add Files" button to browse for files
   - Application automatically scans `C:\Users\Administrator\Desktop\Down` on startup (Windows)
3. **Select Installers**: Check the boxes next to installers you want to run
4. **Start Installation**: Click the "Start" button
5. **Monitor Progress**: Watch real-time progress bars and status updates
6. **Review Results**: Check `install_log.txt` for detailed installation logs

### Silent Installation Logic

NullInstaller automatically attempts to use silent installation flags when possible:

- **MSI Files**: Uses `/quiet /norestart` flags by default
- **EXE Files**: Attempts common silent flags in order:
  - `/S` (NSIS installers)
  - `/silent` (InstallShield)
  - `/quiet` (Microsoft installers)
  - `/verysilent` (Inno Setup)
  - Falls back to interactive installation if silent flags fail

### Command-Line Usage

While primarily a GUI application, NullInstaller can be run from command line:

```bash
# Run with console output (Windows console build)
NullInstaller-console.exe

# Set custom download folder via environment variable
set NULLINSTALLER_SCAN_PATH=C:\MyInstallers
NullInstaller.exe
```

## Configuration

### Environment Variables

- `NULLINSTALLER_SCAN_PATH`: Override default scan directory
- `CGO_ENABLED`: Must be set to `1` for building (automatically handled by build scripts)

### Log Files

- `install_log.txt`: Detailed installation logs in the application directory
- Logs include timestamps, installation commands, exit codes, and error messages

## Limitations & Known Issues

### Current Limitations

1. **Windows Focus**: Primarily designed for Windows installer files (.exe, .msi)
2. **Sequential Installation**: Installers run one at a time (by design for stability)
3. **Silent Flag Detection**: Not all installer types support automatic silent installation
4. **Administrator Rights**: Some installers may require elevated privileges
5. **Network Dependencies**: Installers requiring internet connection may fail in offline environments

### Platform-Specific Notes

#### Windows
- GUI version runs without console window
- Some installers may prompt for UAC elevation
- Windows Defender may scan downloaded installers, causing delays

#### Linux
- Primarily useful for Wine-wrapped Windows installers
- Native Linux package managers (apt, yum, pacman) not integrated

#### macOS
- Limited usefulness for native macOS applications
- May work with some cross-platform installers

## Development

### Project Structure
```
NullInstaller/
├── main.go              # Main application and UI logic
├── installer_engine.go  # Installation engine with queue management
├── go.mod              # Go module dependencies
├── go.sum              # Dependency checksums
├── make.bat            # Windows batch build script
├── build.ps1           # PowerShell cross-platform build script
├── dist/               # Built executables (created by build scripts)
├── install_log.txt     # Runtime installation logs
└── vendor/             # Vendored dependencies (Fyne)
```

### Key Dependencies

- **[Fyne v2.6.2](https://fyne.io/)**: Cross-platform GUI toolkit for Go
- **Go Standard Library**: Context, OS, filepath, sync packages
- **CGO**: Required for Fyne's native UI rendering

### Building from Source

```bash
# Clone the repository
git clone <your-repo-url>
cd NullInstaller

# Download dependencies
go mod tidy

# Run tests (if any)
go test ./...

# Build and run
go run .
```

### Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Make your changes
4. Test on your target platform(s)
5. Commit changes: `git commit -am 'Add feature-name'`
6. Push to the branch: `git push origin feature-name`
7. Submit a pull request

## Troubleshooting

### Common Build Issues

**Error: `gcc: command not found` (Windows)**
```
Solution: Install TDM-GCC or MinGW-w64
- Download TDM-GCC from https://jmeubank.github.io/tdm-gcc/
- Add to PATH: C:\TDM-GCC-64\bin
- Restart terminal/IDE
```

**Error: `package fyne.io/fyne/v2: no Go files in...`**
```
Solution: CGO is disabled or compiler missing
- Ensure CGO_ENABLED=1
- Install appropriate C compiler for your platform
- Run: go env CGO_ENABLED (should show "1")
```

**Error: `undefined: fyne.App` or similar**
```
Solution: Dependencies not downloaded
- Run: go mod tidy
- Run: go mod download
- Ensure internet connection for initial download
```

### Runtime Issues

**Application crashes on startup**
- Check `install_log.txt` for error details
- Ensure all dependencies are installed
- Try console version for debug output: `NullInstaller-console.exe`

**Installers fail to run silently**
- Check installer documentation for correct silent flags
- Some installers don't support silent installation
- Run installers manually first to test

**Permission errors**
- Run NullInstaller as Administrator (Windows)
- Check installer file permissions
- Ensure installers are not corrupted

## License

[Add your license information here]

## Version History

### v0.1.0 (Current)
- Initial release
- Drag & drop interface
- Batch installation support
- Progress tracking
- Silent installation detection
- Cross-platform build support
