# NullInstaller v2.0 🚀

> **A modern, compact, and privacy-focused batch installer for Windows with stealth capabilities**

![Version](https://img.shields.io/badge/version-2.0.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![License](https://img.shields.io/badge/license-Proprietary-red.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)

## ✨ What's New in v2.0

**Complete Rewrite**: Transitioned from Python/PyQt to C#/.NET for better performance and native Windows integration.

### 🎨 **Modern Dark Interface**
- **Compact Design**: 580x420 window optimized for efficiency
- **Dark Theme**: Professional dark UI matching modern Windows apps  
- **Drag & Drop**: Simply drag installer files onto the window
- **Real-time Status**: Color-coded progress indicators (Green ✓, Red ✗, Orange installing)

### 🔒 **Stealth Privacy Mode**
One-click installation of essential privacy and security tools:
- **Tor Browser** - Anonymous web browsing
- **Brave Browser** - Privacy-focused browser with ad blocking
- **Mullvad VPN** - Privacy-first VPN service
- **VeraCrypt** - Professional disk encryption
- **Signal Desktop** - Secure encrypted messaging
- **KeePassXC** - Open-source password manager
- **BleachBit** - Advanced privacy cleaner

### 🛠️ **Advanced Features**
- **Silent Installation**: Automated background installation with common installer flags
- **Registry Verification**: Checks Windows registry to verify successful installations
- **Multiple Installer Support**: Handles .exe, .msi files with appropriate methods
- **Progress Tracking**: Real-time progress bars and status updates
- **Verbose Logging**: Detailed installation logs for troubleshooting
- **Process Timeout**: 2-minute timeout protection prevents hanging installations
- **Duplicate Detection**: Prevents adding the same installer multiple times

## 📥 Download

**Latest Release: v2.0.0**
- **[NullInstallerCompact.exe](https://github.com/NullMeDev/NullBatchInstaller/releases/latest/download/NullInstallerCompact.exe)** (113 MB)
- Self-contained executable - no dependencies required
- Compatible with Windows 10/11

## 🚀 Quick Start

1. **Download** the latest NullInstallerCompact.exe
2. **Run** the executable (no installation required)
3. **Drag & Drop** .exe/.msi files onto the window, or
4. **Click "🔒 Stealth Setup"** for automatic privacy tool installation
5. **Select** installers you want to install
6. **Click "↓ Install All"** to begin batch installation
7. **Click "✓ Verify"** to check installation status

## 🎯 Usage Examples

### Basic Batch Installation
```
1. Drag multiple installer files to the window
2. Check the installers you want to install
3. Click "↓ Install All"
4. Monitor progress in real-time
5. Use "✓ Verify" to confirm installations
```

### Stealth Privacy Setup
```
1. Click "🔒 Stealth Setup" button
2. Confirm installation of privacy tools
3. Tools are automatically downloaded and installed
4. Perfect for setting up a secure system quickly
```

### Advanced Options
- **Verbose**: Enable detailed logging for troubleshooting
- **Show Output**: Display installer output windows (useful for debugging)
- **Clear**: Uncheck all selected installers
- **Verify**: Check Windows registry for installed programs

## 📋 System Requirements

- **OS**: Windows 10 (1903+) or Windows 11
- **Architecture**: x64 (64-bit)
- **RAM**: 100MB+ available memory
- **Disk**: 150MB+ free space (for runtime and temporary files)
- **Permissions**: Administrator rights recommended for system-wide installations

## 🔧 Technical Details

### **Architecture**
- **Language**: C# with .NET 9.0
- **UI Framework**: Windows Forms
- **Distribution**: Self-contained single executable
- **Size**: ~113 MB (includes .NET runtime)

### **Installation Methods**
- **MSI files**: Uses `msiexec /i /qn /norestart`
- **EXE files**: Uses common silent flags (`/S`, `/SILENT`, `/QUIET`)
- **Process Management**: 2-minute timeout per installer
- **Error Handling**: Graceful failure handling with status updates

### **File Locations**
```
NullInstallerCompact.exe     # Main executable
NullInstaller_Log.txt        # Installation log file
%TEMP%\NullInstaller_Stealth # Downloaded privacy tools
```

## 🆚 Version Comparison

| Feature | v1.x (Python/PyQt) | v2.0 (C#/.NET) |
|---------|-------------------|----------------|
| **File Size** | ~50MB + Python deps | 113MB self-contained |
| **Startup Time** | 3-5 seconds | <1 second |
| **Memory Usage** | 80-120MB | 50-80MB |
| **Dependencies** | Python 3.x + multiple packages | None (self-contained) |
| **Stealth Mode** | ❌ | ✅ Privacy tools |
| **Registry Verification** | ❌ | ✅ Built-in |
| **Native Look** | PyQt themed | ✅ Native Windows |
| **Installer Detection** | Basic | ✅ Smart detection |

## 🎨 Interface Preview

```
┌─────────────────────────────────────────────┐
│ NullInstaller v2.0        18 installers detected │
├─────────────────────────────────────────────┤
│ INSTALLER FILES                             │
│ ☑ BraveInstaller.exe       1.3 MB    Ready │
│ ☑ Docker Desktop...exe     612 MB    Ready │
│ ☑ Firefox Installer.exe    415 MB    Ready │
│ ☑ Proton Drive Setup...    73 MB     Ready │
│ ☑ WarpSetup.exe           44 MB     Ready │
├─────────────────────────────────────────────┤
│ ↓Install All │🔒Stealth Setup│✓Verify│Clear│
│ ☑ Verbose    ☑ Show Output              │
├─────────────────────────────────────────────┤
│ ████████████████████████████████████  95% │
│ Installing Docker Desktop... (3/5)          │
└─────────────────────────────────────────────┘
```

## 📝 Changelog

### v2.0.0 (2025-08-07) - **Major Release**
- **NEW**: Complete rewrite in C#/.NET 9.0
- **NEW**: Stealth Privacy Mode with 7 essential tools
- **NEW**: Registry verification system
- **NEW**: Modern dark UI with compact design
- **NEW**: Drag & drop support
- **NEW**: Smart installer detection and handling
- **NEW**: Process timeout protection
- **NEW**: Verbose logging and output options
- **IMPROVED**: 10x faster startup time
- **IMPROVED**: Better error handling and user feedback
- **IMPROVED**: Native Windows integration
- **REMOVED**: Python dependencies

### v1.2.2 (Previous Python Version)
- Archive management with ZIP/7z support
- Real-time system metrics monitoring
- Multi-threaded processing
- Basic installation logging

## 🤝 Contributing

While this project is proprietary, we welcome feedback and bug reports:

1. **Issues**: Report bugs via GitHub Issues
2. **Feature Requests**: Submit enhancement ideas
3. **Testing**: Help test new features and report compatibility issues

## 📜 License

This project is **proprietary software**. All rights reserved.

- ✅ **Personal Use**: Free for personal/non-commercial use
- ❌ **Commercial Use**: Requires license agreement
- ❌ **Redistribution**: Not permitted without authorization
- ❌ **Reverse Engineering**: Not permitted

## 🔗 Links

- **GitHub Repository**: https://github.com/NullMeDev/NullBatchInstaller
- **Releases**: https://github.com/NullMeDev/NullBatchInstaller/releases
- **Issues**: https://github.com/NullMeDev/NullBatchInstaller/issues
- **Developer**: [NullMeDev](https://github.com/NullMeDev)

## ⚠️ Disclaimer

- **Use at your own risk**: This software modifies your system by installing programs
- **Administrator Rights**: Some installations require elevated privileges
- **Antivirus**: Some security software may flag this as suspicious due to batch installation capabilities
- **Stealth Mode**: Downloads software from official sources but installs silently
- **Privacy**: No telemetry or data collection - everything runs locally

---

<div align="center">

**Made with ❤️ for the Windows community**

[![GitHub](https://img.shields.io/badge/GitHub-NullMeDev-black?logo=github)](https://github.com/NullMeDev)

</div>
