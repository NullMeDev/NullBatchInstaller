# How to Run NullInstaller

## 🚀 Quick Start

### Method 1: Run the Script (Recommended)
```cmd
# Double-click this file or run from command line:
run.bat
```
This will launch the enhanced NullInstaller with all features.

### Method 2: Direct Go Command
```cmd
go run .
```

## 📁 Production Files

The cleaned production version contains only these essential files:

```
NullInstaller/
├── main.go              # Main application (31KB - all UI and features)
├── installer_engine.go  # Installation engine (14KB - queue management)
├── go.mod              # Go dependencies
├── go.sum              # Dependency checksums
├── run.bat             # Easy run script
├── build.bat           # Build script (needs MinGW)
├── make.bat            # Alternative build script
├── README.md           # Documentation
└── .gitignore          # Git ignore file
```

## 🔧 Building the EXE (Requires MinGW)

### Prerequisites for Building:
1. **Install MinGW Compiler**:
   - Download TDM-GCC: https://jmeubank.github.io/tdm-gcc/
   - Install and ensure it's in your PATH
   - Restart your terminal

2. **Build the executable**:
   ```cmd
   # GUI version (no console window)
   go build -ldflags "-s -w -H=windowsgui" -o NullInstaller.exe .
   
   # Console version (with debug output)
   go build -ldflags "-s -w" -o NullInstaller-console.exe .
   ```

3. **Run the built exe**:
   ```cmd
   .\NullInstaller.exe
   ```

## ✨ Enhanced Features

When you run NullInstaller, you'll get:

### Tabbed Interface:
- **"Local Files"** tab: Drag & drop your installer files
- **"Universal Programs"** tab: Select from 20 popular programs to auto-download

### 20 Universal Programs Available:
- **Browsers**: Chrome, Firefox
- **Media**: VLC, Spotify, Discord, Steam, OBS Studio  
- **Office**: LibreOffice, Adobe Reader, Notepad++
- **Development**: VS Code, Git for Windows
- **Communication**: Skype, Zoom, TeamViewer
- **Utilities**: 7-Zip, WinRAR, CCleaner, Malwarebytes
- **Graphics**: GIMP

### Enhanced Controls:
- **Verbose Logging** checkbox (detailed logs)
- **Download Selected** button (for universal programs)
- **Start Installation** button (batch silent install)
- **Real-time progress** bars and status updates

## 📝 Usage Workflow

1. **Launch**: Run `run.bat` or double-click the exe
2. **Choose Programs**: 
   - Use "Universal Programs" tab to select popular software
   - Or use "Local Files" tab for your own installers
3. **Download**: Click "Download Selected" to get latest versions
4. **Install**: Switch to "Local Files", select programs, click "Start Installation"
5. **Monitor**: Watch progress bars and check `install_log.txt` for details

## 🐛 Troubleshooting

### If the GUI doesn't appear:
- Make sure Go 1.22+ is installed
- Try running `go run .` from command line to see error messages
- Check that no antivirus is blocking the application

### If builds fail:
- The current environment lacks MinGW compiler for CGO
- Use `run.bat` to run from source code instead
- Or install TDM-GCC and retry building

### If installations fail:
- Enable verbose logging in the UI
- Check `install_log.txt` for detailed error messages  
- Some installers may require administrator privileges
- Ensure internet connection for downloads

## 🎯 Production Ready

The application is fully functional and includes:
- ✅ 550+ lines of enhanced functionality
- ✅ Comprehensive error handling and logging
- ✅ Thread-safe concurrent operations
- ✅ Modern GUI with dark theme
- ✅ Auto-update URLs for latest program versions
- ✅ Silent installation detection and optimization

Perfect for setting up fresh Windows installations with all essential software!
