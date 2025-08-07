# Release Notes - NullInstaller v2.0.0

**Release Date**: August 7, 2025  
**Build**: NullInstallerCompact.exe (113 MB)

## 🎉 Major Release - Complete Rewrite

This is a **major release** featuring a complete rewrite from Python/PyQt to C#/.NET 9.0, bringing significant performance improvements and new features.

## 🆕 New Features

### 🔒 **Stealth Privacy Mode**
- One-click installation of essential privacy and security tools
- Includes: Tor Browser, Brave, Mullvad VPN, VeraCrypt, Signal, KeePassXC, BleachBit
- Automatic download and silent installation
- Perfect for quickly setting up a secure system

### ✅ **Registry Verification System**
- Verify installations by checking Windows registry
- Real-time status updates showing verified installations
- Detailed verification results window
- Detects both regular and stealth-installed programs

### 🎨 **Modern Dark Interface**
- Compact 580x420 window design
- Professional dark theme matching modern Windows apps
- Clean, minimalist layout with improved usability
- Native Windows Forms look and feel

### 📁 **Enhanced Drag & Drop**
- Drag installer files directly onto the window
- Automatic duplicate detection
- Visual feedback during drag operations
- Support for multiple files simultaneously

## 🚀 Performance Improvements

| Metric | v1.x (Python) | v2.0 (C#) | Improvement |
|--------|---------------|-----------|-------------|
| **Startup Time** | 3-5 seconds | <1 second | **10x faster** |
| **Memory Usage** | 80-120 MB | 50-80 MB | **40% less** |
| **File Size** | 50MB + deps | 113MB | Self-contained |
| **Response Time** | Moderate | Instant | **Much faster** |

## 🛠️ Technical Improvements

### **Installation Engine**
- Smarter installer detection and handling
- Support for common silent installation flags
- 2-minute timeout protection prevents hanging
- Better error handling and recovery
- Process isolation for stability

### **Logging & Debugging**
- Comprehensive verbose logging system
- Option to show installer output windows
- Detailed installation logs with timestamps
- Error tracking and troubleshooting information

### **Architecture**
- **Framework**: Migrated from Python 3.x to C# .NET 9.0
- **UI**: Replaced PyQt6 with native Windows Forms
- **Distribution**: Self-contained executable (no dependencies)
- **Platform**: Optimized specifically for Windows 10/11

## 🔧 Supported Installers

### **File Types**
- `.exe` - Executable installers with silent flags (`/S`, `/SILENT`, `/QUIET`)
- `.msi` - Windows Installer packages with `msiexec /qn`
- Auto-detection of installer type and appropriate installation method

### **Installation Methods**
```
MSI Files: msiexec /i "installer.msi" /qn /norestart
EXE Files: installer.exe /S (with fallback options)
Timeout:   120 seconds maximum per installer
```

## 📱 User Interface Changes

### **Main Window**
```
┌─────────────────────────────────────────────┐
│ NullInstaller v2.0      18 installers detected │
├─────────────────────────────────────────────┤
│ INSTALLER FILES                             │
│ ☑ Program.exe             Size      Status  │
├─────────────────────────────────────────────┤
│ [Install All] [Stealth] [Verify] [Clear]    │
│ ☑ Verbose    ☑ Show Output                 │
├─────────────────────────────────────────────┤
│ Progress: ████████████████████████  95%    │
│ Status: Installing program... (3/5)         │
└─────────────────────────────────────────────┘
```

### **New Buttons**
- **🔒 Stealth Setup**: One-click privacy tool installation
- **✓ Verify**: Check installation status via registry
- **Clear**: Unselect all installers
- **Show Output**: Display installer windows for debugging

## 🔄 Migration from v1.x

### **What's Removed**
- ❌ Python dependencies requirement
- ❌ PyQt6 UI framework
- ❌ ZIP/7z archive extraction (focus shifted to installers)
- ❌ System metrics monitoring
- ❌ Separate requirements.txt

### **What's Changed**
- ✅ Single executable file (was: Python + multiple packages)
- ✅ Native Windows integration (was: Cross-platform with compromise)
- ✅ Instant startup (was: 3-5 second Python initialization)
- ✅ Dark theme by default (was: System theme dependent)

### **What's New**
- ✅ Stealth privacy mode
- ✅ Registry verification
- ✅ Advanced logging options
- ✅ Drag & drop support
- ✅ Process timeout protection
- ✅ Smart installer detection

## 🎯 Use Cases

### **System Setup**
Perfect for setting up new Windows installations:
1. Download NullInstallerCompact.exe
2. Click "🔒 Stealth Setup" for privacy tools
3. Drag additional installers for other software
4. Use "✓ Verify" to confirm everything installed correctly

### **IT Administration**
Streamline software deployment:
- Batch install common business applications
- Verify installations across multiple machines
- Silent installation reduces user interaction
- Comprehensive logging for compliance

### **Privacy-Conscious Users**
Quick privacy tool deployment:
- Tor Browser for anonymous browsing
- VPN software for connection privacy
- Encryption tools for data protection
- Secure messaging applications

## ⚠️ Breaking Changes

### **File Format**
- No longer supports archive files (ZIP, 7z)
- Focus exclusively on installer files (.exe, .msi)

### **Dependencies**
- Requires Windows 10 (1903+) or Windows 11
- No longer supports older Windows versions
- .NET 9.0 runtime included (self-contained)

### **Configuration**
- Configuration format changed from TOML to Windows-native
- Log files now use Windows-standard locations
- Settings stored in application directory

## 🐛 Known Issues

1. **Antivirus Detection**: Some security software may flag the application due to batch installation capabilities
2. **UAC Prompts**: Some installers may still show UAC prompts despite silent flags
3. **Network Downloads**: Stealth mode requires internet connection for downloading tools
4. **File Paths**: Very long file paths (>260 characters) may cause issues

## 🔜 Future Plans

### **v2.1 (Planned)**
- Plugin system for custom installers
- Scheduled installations
- Installation profiles and presets
- Enhanced logging and reporting

### **Long-term**
- Cloud synchronization of installation lists
- Integration with Windows Package Manager (winget)
- Multi-language support
- Web-based remote management interface

---

## 📞 Support

- **GitHub Issues**: https://github.com/NullMeDev/NullBatchInstaller/issues
- **Documentation**: README.md in repository
- **Discussions**: GitHub Discussions for community support

**Thank you for using NullInstaller v2.0!** 🚀
