# JetBrains IDE Plugin Auto-Installation Integration

## Overview
This module provides automatic plugin installation for IntelliJ-family IDEs (IntelliJ IDEA, PyCharm, WebStorm, etc.) after IDE installation. It integrates seamlessly with the NullInstaller system.

## Features
- ✅ Automatic IDE detection (AppData profiles and installation directories)
- ✅ Command-line plugin installation via IDE executable
- ✅ Fallback to manual installation guides when CLI fails
- ✅ IDE-specific plugin profiles
- ✅ Retry mechanism (up to 3 attempts)
- ✅ Monitoring mode for detecting new IDE installations
- ✅ Comprehensive logging

## Supported IDEs
- IntelliJ IDEA
- PyCharm
- WebStorm
- PhpStorm
- GoLand
- DataGrip
- Rider
- CLion
- RubyMine

## Default Plugin Catalog

### Core Plugins (All IDEs)
1. **ChatGPT Assistant** (ID: 21983)
   - AI-powered coding assistant
   - Requires API key configuration after installation

2. **GitToolBox** (ID: 7499)
   - Enhanced Git integration
   - Branch info in status bar, auto-fetch, and more

### Additional Plugins (IDE-specific)
3. **Key Promoter X** (ID: 9792)
   - Helps learn keyboard shortcuts
   - Shows shortcuts for actions performed with mouse

4. **Rainbow Brackets** (ID: 10080)
   - Colorful bracket matching
   - Improves code readability

5. **Tabnine AI** (ID: 12798)
   - AI-powered code completion
   - Alternative/complement to GitHub Copilot

## Usage

### Command Line Interface
```powershell
# Check for installed IDEs
.\jetbrains_plugin_integration.ps1 check

# Install plugins for all detected IDEs
.\jetbrains_plugin_integration.ps1 install

# Install plugins for specific IDE type
.\jetbrains_plugin_integration.ps1 install -IdeType "PyCharm"

# Monitor for new IDE installations (runs for 1 hour)
.\jetbrains_plugin_integration.ps1 monitor

# Export integration hook for main installer
.\jetbrains_plugin_integration.ps1 export
```

### Integration with Main Installer
After exporting the hook, add this to your main installer:

```powershell
# Source the hook
. .\jetbrains_plugin_hook.ps1

# After IDE installation
Install-JetBrainsPlugins -IdeType "PyCharm"
```

## Installation Process

### Automatic Installation Flow
1. Script detects installed IDEs
2. Checks for IDE configuration directories
3. Creates plugins directory if needed
4. Attempts CLI installation via `idea.exe installPlugins`
5. Falls back to marker files for manual installation if CLI fails
6. Logs all actions for troubleshooting

### Manual Installation (Fallback)
When automatic installation fails, the script creates marker files in the plugins directory with instructions:

1. Open the IDE
2. Go to File > Settings > Plugins
3. Search for plugin in Marketplace
4. Click Install and restart IDE

## File Structure
```
NullInstaller/
├── jetbrains_plugin_integration.ps1  # Main integration script
├── jetbrains_plugin_hook.ps1        # Integration hook for main installer
├── jetbrains_plugin.log             # Detailed log file
└── PLUGIN_INSTALLATION_GUIDE.txt    # Generated manual installation guide
```

## Retry Logic
- Maximum 3 retry attempts per plugin
- 5-second delay between retries
- Useful when IDE profile is being created

## Monitoring Mode
- Checks for new IDE installations every 30 seconds
- Runs for maximum 1 hour
- Automatically installs plugins when new IDE is detected
- Useful during unattended installations

## Troubleshooting

### Common Issues

1. **"No JetBrains IDEs found"**
   - Ensure IDE is properly installed
   - Check standard installation paths
   - Verify %APPDATA%\JetBrains directory exists

2. **"CLI installation failed"**
   - IDE executable might not support installPlugins command
   - Try running IDE once manually first
   - Check marker files in plugins directory for manual steps

3. **"IDE profile not ready"**
   - IDE needs to be run at least once
   - Profile directory creation can take up to 60 seconds
   - Use monitor mode for better results

### Log Locations
- Main log: `jetbrains_plugin.log`
- Installation markers: `%APPDATA%\JetBrains\<IDE>\config\plugins\INSTALL_*.txt`

## API Limitations
Note: Direct plugin download from JetBrains Marketplace requires authentication or may be rate-limited. The script uses CLI installation method which bypasses these limitations.

## Future Enhancements
- [ ] Support for custom plugin repositories
- [ ] Plugin version management
- [ ] Backup/restore plugin configurations
- [ ] Integration with JetBrains Toolbox
- [ ] Silent installation without user prompts

## License
Part of NullInstaller suite - Internal use only

## Support
For issues or questions, check the log files first, then consult the troubleshooting section above.
