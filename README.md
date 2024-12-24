# Null Batch Installer

A modern, user-friendly GUI application for batch installation of executables and archive management.

## Features

- Drag-and-drop interface for easy file management
- Support for ZIP and 7z archive formats
- Real-time system metrics monitoring
- Progress tracking for each installation
- Dark mode interface
- Multi-threaded installation processing
- Installation logging

## Requirements

- Python 3.x
- Dependencies:
  - PyQt6 >= 6.5.0
  - py7zr >= 0.20.0
  - aiofiles >= 23.2.1
  - cryptography >= 41.0.0
  - aiosqlite >= 0.19.0
  - pyinstaller >= 6.3.0

## Installation

1. Clone this repository or download the source code
2. Install the required dependencies:
   ```bash
   pip install -r requirements.txt
   ```

## Usage

1. Run the application:
   ```bash
   python NullBatchInstaller.py
   ```
   
2. Use the application by:
   - Dragging and dropping executable files or archives onto the window
   - Using the "Browse" button to select files
   - Click "Start Installation" to begin the batch installation process
   - Monitor progress through the built-in logging window
   - Use "Stop Installation" to halt the process if needed
   - "Clear List" to remove all items from the queue

## Building from Source

To create a standalone executable:
```bash
pyinstaller NullBatchInstaller.spec
```

## Version

Current Version: 1.2.2

## License

This project is proprietary software. All rights reserved.

## Last Updated

December 24, 2024
