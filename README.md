# NullInstaller v4.2.6

**Professional Windows Software Installer with Modern WinRAR-Style Interface**

Ultra-fast, lightweight batch software installer written in Rust - just download and run!

## Quick Start

Simply run `NullInstaller.exe` - no installation or dependencies required!

## Features

✅ **Modern WinRAR-Style GUI**
- Left panel: Category tree view
- Right panel: Detailed software list with checkboxes
- Top toolbar with large action buttons
- Bottom console for real-time logging
- Professional native Windows interface

✅ **Core Functionality**
- Batch software installation and download
- Select All / Deselect All
- Stealth mode for privacy tools
- Real-time progress tracking
- Category-based organization
- Silent/unattended installation support

## Requirements

- Windows 10/11 (64-bit)
- Administrator privileges (for software installation)
- No runtime dependencies - completely standalone!

## Software Catalog

Reads from `assets/software_catalog.json` with categories:
- Browsers
- Development Tools  
- Security Software
- Media Applications
- Productivity Tools
- System Utilities

## Technical Details

- **Size:** 203KB (ultra-lightweight!)
- **Language:** Rust with native Windows API
- **Architecture:** x64
- **Build:** Single static binary, zero dependencies
- **Performance:** < 100ms startup, < 10MB RAM usage
- **Previous C# version:** 66MB → Now only 203KB (338x smaller!)

## Building from Source

```bash
# Prerequisites: Rust 1.70+

# Build with script
build.bat

# Or directly
cargo build --release
```

## License
Proprietary - NullMeDev
