# NullInstaller v2.0 - Modern Universal Installer Tool

## Overview

NullInstaller v2.0 is a modern, lightweight Windows GUI application designed to simplify software installation and management. Built with C# WinForms for native Windows performance, it features a clean tabbed interface, 60+ curated programs across multiple categories, drag & drop support, silent installations, and real-time progress tracking.

## ✨ Key Features

### **Modern Tabbed Interface**
- **Essential (20 programs)** - Core applications everyone needs
- **Privacy (15 programs)** - Privacy-focused tools, browsers, VPNs, encryption
- **Security (15 programs)** - Antivirus, anti-malware, security tools
- **Development (10 programs)** - Developer tools, IDEs, utilities
- **Local Files** - Auto-detected and drag & drop installers

### **Advanced Functionality**
- ✅ **Silent Installation** - Automatic detection of MSI/EXE silent flags
- ✅ **Drag & Drop Support** - Add custom installers by dropping files
- ✅ **Auto-Detection** - Scans common download folders for installers
- ✅ **Real-Time Progress** - Per-item status tracking and overall progress
- ✅ **Activity Logging** - Verbose logging with timestamps
- ✅ **Modern UI** - Clean, minimalistic design with modern styling

### **Built for Speed**
- ⚡ **22KB Executable** - Ultra-lightweight, no bloat
- ⚡ **Native Performance** - C# WinForms with .NET Framework 4.0+
- ⚡ **Fast Builds** - Compiles in seconds with built-in Windows compiler
- ⚡ **Instant Launch** - No CGO dependencies or external runtime

## 📦 Program Categories

### **Essential Programs (20)**
| Program | Description |
|---------|-------------|
| Google Chrome | Web browser |
| Mozilla Firefox | Web browser |
| Microsoft Edge | Web browser |
| VLC Media Player | Media player |
| 7-Zip | File archiver |
| WinRAR | File archiver |
| Notepad++ | Text editor |
| Adobe Acrobat Reader | PDF reader |
| LibreOffice | Office suite |
| GIMP | Image editor |
| Discord | Communication |
| Spotify | Music streaming |
| Steam | Gaming platform |
| Zoom | Video conferencing |
| TeamViewer | Remote desktop |
| CCleaner | System cleaner |
| BleachBit | System cleaner |
| HandBrake | Video converter |
| OBS Studio | Screen recording |
| Audacity | Audio editor |

### **Privacy Programs (15)**
| Program | Description |
|---------|-------------|
| Mullvad Browser | Privacy browser |
| Mullvad VPN | Privacy VPN |
| NordVPN | VPN service |
| IPVanish VPN | VPN service |
| Tor Browser | Anonymous browser |
| Brave Browser | Privacy browser |
| Signal Desktop | Private messaging |
| ProtonVPN | Secure VPN |
| VeraCrypt | Disk encryption |
| KeePassXC | Password manager |
| Bitwarden | Password manager |
| qBittorrent | Torrent client |
| I2P | Anonymous network |
| Tails | Anonymous OS |
| GnuPG | Encryption tool |

### **Security Programs (15)**
| Program | Description |
|---------|-------------|
| Kaspersky Security Cloud | Antivirus |
| Bitdefender Antivirus | Antivirus |
| TotalAV Antivirus | Antivirus |
| Malwarebytes | Anti-malware |
| Windows Defender | Built-in antivirus |
| ESET NOD32 | Antivirus |
| Avast Free Antivirus | Antivirus |
| AVG AntiVirus | Antivirus |
| Spybot Search & Destroy | Anti-spyware |
| AdwCleaner | Adware remover |
| RootkitRevealer | Rootkit scanner |
| ClamWin Antivirus | Open source AV |
| HijackThis | System scanner |
| Process Monitor | System monitoring |
| Wireshark | Network analyzer |

### **Development Programs (10)**
| Program | Description |
|---------|-------------|
| Visual Studio Code | Code editor |
| Git for Windows | Version control |
| Node.js | JavaScript runtime |
| Python | Programming language |
| JetBrains Toolbox | IDE manager |
| Docker Desktop | Containerization |
| Postman | API testing |
| FileZilla | FTP client |
| PuTTY | SSH client |
| WinSCP | SCP client |

## 🚀 Quick Start

### **Prerequisites**
- Windows 7 or later
- .NET Framework 4.0+ (usually pre-installed)
- Administrator privileges (for installations)

### **Build from Source**
1. Clone or download the source code
2. Run the build script:
   ```batch
   .\build_modern.bat
   ```
3. Launch the application:
   ```batch
   .\dist\NullInstaller_Compatible.exe
   ```

### **Manual Build**
```batch
# Create output directory
mkdir dist

# Compile with Windows built-in C# compiler
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe ^
  /out:dist\NullInstaller_Compatible.exe ^
  /reference:System.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Data.dll ^
  /optimize+ ^
  NullInstaller_Compatible.cs
```

## 💡 How to Use

### **Download & Install Programs**
1. **Select Programs**: Check the programs you want in any category tab
2. **Download**: Click "Download Selected" to download from official sources
3. **Install**: Switch to "Local Files" tab and click "Install Selected"
4. **Monitor Progress**: Watch real-time status updates and activity log

### **Add Custom Installers**
- **Drag & Drop**: Drag `.exe` or `.msi` files onto the application
- **Auto-Detection**: Files in common download folders are automatically detected
- **Manual Addition**: Copy installers to monitored directories

### **Silent Installation Logic**
- **MSI Files**: Uses `msiexec /i "file.msi" /qn /norestart`
- **EXE Files**: Attempts common silent flags like `/S`
- **Progress Tracking**: Real-time status updates per installer
- **Error Handling**: Failed installations are marked and logged

## 🔧 Technical Details

### **Architecture**
- **Language**: C# 5.0 (compatible with .NET Framework 4.0+)
- **UI Framework**: Windows Forms with modern styling
- **Threading**: Async/await for downloads and background operations
- **Logging**: File-based activity logging with timestamps
- **Data Structures**: Generic collections for program management

### **File Structure**
```
NullInstaller/
├── NullInstaller_Compatible.cs    # Main application source
├── build_modern.bat               # Build script
├── dist/                          # Output directory
│   └── NullInstaller_Compatible.exe
├── README_Modern.md               # This file
└── NullInstaller_Log.txt          # Runtime activity log
```

### **Program Data Management**
- **Categories**: Dictionary-based organization
- **Download URLs**: Auto-updating links to official sources
- **Status Tracking**: Per-item progress with visual indicators
- **File Detection**: Recursive scanning of download directories

## 🎯 Advantages Over Previous Versions

### **vs Go + Fyne Version**
| Feature | NullInstaller v2.0 | Go + Fyne |
|---------|-------------------|-----------|
| **Build Time** | < 5 seconds | 2+ minutes |
| **Executable Size** | 22KB | 50+ MB |
| **Dependencies** | .NET Framework (built-in) | CGO, MinGW, external libs |
| **UI Performance** | Native Windows | Cross-platform overhead |
| **Build Complexity** | Single compiler command | Complex toolchain setup |
| **Compatibility** | Windows native | Cross-platform but heavy |

### **New in v2.0**
- ✅ **4x More Programs** - Expanded from 20 to 60+ applications
- ✅ **Category Organization** - Tabbed interface for easy navigation
- ✅ **Privacy Focus** - Dedicated privacy tools and VPN clients
- ✅ **Modern Styling** - Updated UI with contemporary design
- ✅ **Better Logging** - Enhanced activity tracking and error reporting
- ✅ **Faster Performance** - Native Windows controls and optimized code

## 🛡️ Security & Privacy

### **Download Security**
- All download URLs point to official sources
- HTTPS connections for secure downloads
- Checksum validation (where supported by source)
- No bundled installers or modified software

### **Privacy Features**
- Dedicated privacy category with 15 specialized tools
- VPN clients (Mullvad, NordVPN, IPVanish, ProtonVPN)
- Anonymous browsers (Tor, Mullvad Browser, Brave)
- Encryption tools (VeraCrypt, GnuPG)
- Password managers (KeePassXC, Bitwarden)

### **Installation Safety**
- Silent installation with standard flags
- No system modifications beyond program installation
- Administrator privileges only when required
- Detailed logging of all operations

## 📋 Known Limitations

- **Windows Only** - Designed specifically for Windows environments
- **Internet Required** - Downloads require active internet connection
- **Silent Install Support** - Some installers may not support silent installation
- **URL Updates** - Download URLs may occasionally need updates

## 🤝 Contributing

### **Adding New Programs**
1. Edit the `programCategories` dictionary in `NullInstaller_Compatible.cs`
2. Add new `ProgramInfo` entries with name, description, and download URL
3. Test the download and installation process
4. Update documentation

### **Reporting Issues**
- Include the contents of `NullInstaller_Log.txt`
- Specify Windows version and .NET Framework version
- Describe expected vs actual behavior
- Provide screenshots if UI-related

## 📄 License

This project is provided as-is for educational and personal use. All downloaded software is subject to their respective licenses and terms of service.

---

**NullInstaller v2.0** - Simplifying Windows software management with a modern, privacy-focused approach.
