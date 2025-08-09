# JetBrains IDE Plugin Auto-Installer - Final Version
# This script handles plugin installation for IntelliJ-family IDEs

param(
    [string]$IdeType = "",
    [int]$MaxRetries = 3,
    [switch]$ManualMode
)

# Plugin catalog with marketplace URLs
$PluginCatalog = @{
    "ChatGPT_Assistant" = @{
        Id = 21983
        Name = "ChatGPT"
        MarketplaceUrl = "https://plugins.jetbrains.com/plugin/21983-chatgpt"
    }
    "GitToolBox" = @{
        Id = 7499
        Name = "GitToolBox"
        MarketplaceUrl = "https://plugins.jetbrains.com/plugin/7499-gittoolbox"
    }
    "Key_Promoter_X" = @{
        Id = 9792
        Name = "Key Promoter X"
        MarketplaceUrl = "https://plugins.jetbrains.com/plugin/9792-key-promoter-x"
    }
    "Rainbow_Brackets" = @{
        Id = 10080
        Name = "Rainbow Brackets"
        MarketplaceUrl = "https://plugins.jetbrains.com/plugin/10080-rainbow-brackets"
    }
    "Tabnine_AI" = @{
        Id = 12798
        Name = "Tabnine AI Code Completion"
        MarketplaceUrl = "https://plugins.jetbrains.com/plugin/12798-tabnine-ai-code-completion"
    }
}

# IDE-specific plugin recommendations
$IdeSpecificPlugins = @{
    "IntelliJIdea" = @("ChatGPT_Assistant", "GitToolBox", "Key_Promoter_X", "Rainbow_Brackets", "Tabnine_AI")
    "PyCharm" = @("ChatGPT_Assistant", "GitToolBox", "Key_Promoter_X", "Rainbow_Brackets")
    "WebStorm" = @("ChatGPT_Assistant", "GitToolBox", "Rainbow_Brackets")
    "PhpStorm" = @("ChatGPT_Assistant", "GitToolBox", "Key_Promoter_X")
    "GoLand" = @("ChatGPT_Assistant", "GitToolBox", "Key_Promoter_X")
    "DataGrip" = @("ChatGPT_Assistant", "GitToolBox")
    "Rider" = @("ChatGPT_Assistant", "GitToolBox", "Key_Promoter_X", "Rainbow_Brackets")
    "CLion" = @("ChatGPT_Assistant", "GitToolBox", "Key_Promoter_X")
    "RubyMine" = @("ChatGPT_Assistant", "GitToolBox", "Rainbow_Brackets")
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    switch ($Level) {
        "ERROR" { Write-Host $logMessage -ForegroundColor Red }
        "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $logMessage -ForegroundColor Green }
        "MANUAL" { Write-Host $logMessage -ForegroundColor Cyan }
        default { Write-Host $logMessage }
    }
    
    $logFile = Join-Path $PSScriptRoot "jetbrains_plugin_install.log"
    Add-Content -Path $logFile -Value $logMessage
}

function Find-JetBrainsIDEs {
    Write-Log "Searching for installed JetBrains IDEs..."
    
    $foundIDEs = @()
    
    # Check AppData path
    $jetbrainsPath = "$env:APPDATA\JetBrains"
    if (Test-Path $jetbrainsPath) {
        $ideFolders = Get-ChildItem -Path $jetbrainsPath -Directory | Where-Object { $_.Name -match '202[0-9]\.' }
        
        foreach ($folder in $ideFolders) {
            $ideName = $folder.Name -replace '20[0-9]{2}\..*', ''
            $ideVersion = $folder.Name -replace '^[^2]*', ''
            
            $ideInfo = @{
                Name = $ideName
                Version = $ideVersion
                Path = $folder.FullName
                PluginsPath = Join-Path $folder.FullName "config\plugins"
            }
            
            $foundIDEs += $ideInfo
            Write-Log "Found IDE: $ideName version $ideVersion"
        }
    }
    
    # Check LocalAppData path (for Toolbox installations)
    $toolboxPath = "$env:LOCALAPPDATA\JetBrains\Toolbox\apps"
    if (Test-Path $toolboxPath) {
        $toolboxApps = Get-ChildItem -Path $toolboxPath -Directory
        
        foreach ($app in $toolboxApps) {
            $channelPath = Join-Path $app.FullName "ch-0"
            if (Test-Path $channelPath) {
                $versions = Get-ChildItem -Path $channelPath -Directory
                foreach ($version in $versions) {
                    $ideName = $app.Name
                    $ideVersion = $version.Name
                    
                    # Find config path
                    $configPath = "$env:APPDATA\JetBrains\$ideName$ideVersion"
                    
                    $ideInfo = @{
                        Name = $ideName
                        Version = $ideVersion
                        Path = $version.FullName
                        PluginsPath = Join-Path $configPath "plugins"
                        ExecutablePath = Join-Path $version.FullName "bin\idea64.exe"
                    }
                    
                    $foundIDEs += $ideInfo
                    Write-Log "Found Toolbox IDE: $ideName version $ideVersion"
                }
            }
        }
    }
    
    return $foundIDEs
}

function Install-PluginViaIDE {
    param(
        [hashtable]$IdeInfo,
        [string]$PluginId
    )
    
    # Try to find IDE executable
    $possiblePaths = @(
        $IdeInfo.ExecutablePath,
        (Join-Path $IdeInfo.Path "bin\idea64.exe"),
        (Join-Path $IdeInfo.Path "bin\idea.exe"),
        (Join-Path $IdeInfo.Path "bin\pycharm64.exe"),
        (Join-Path $IdeInfo.Path "bin\webstorm64.exe"),
        (Join-Path $IdeInfo.Path "bin\phpstorm64.exe"),
        (Join-Path $IdeInfo.Path "bin\goland64.exe"),
        (Join-Path $IdeInfo.Path "bin\datagrip64.exe"),
        (Join-Path $IdeInfo.Path "bin\rider64.exe"),
        (Join-Path $IdeInfo.Path "bin\clion64.exe"),
        (Join-Path $IdeInfo.Path "bin\rubymine64.exe")
    )
    
    $ideExe = $null
    foreach ($path in $possiblePaths) {
        if ($path -and (Test-Path $path)) {
            $ideExe = $path
            break
        }
    }
    
    if ($ideExe) {
        Write-Log "Found IDE executable: $ideExe"
        try {
            $arguments = "installPlugins com.intellij.marketplace:$PluginId"
            Start-Process -FilePath $ideExe -ArgumentList $arguments -Wait -NoNewWindow -ErrorAction Stop
            Write-Log "Plugin installation command sent to IDE" "SUCCESS"
            return $true
        } catch {
            Write-Log "Failed to execute IDE command: $_" "WARNING"
        }
    }
    
    return $false
}

function Create-PluginInstallationGuide {
    param(
        [hashtable]$IdeInfo,
        [string[]]$PluginNames
    )
    
    $guidePath = Join-Path $PSScriptRoot "PLUGIN_INSTALLATION_GUIDE.txt"
    
    $guideContent = @"
================================================================================
JETBRAINS IDE PLUGIN INSTALLATION GUIDE
================================================================================

IDE DETECTED: $($IdeInfo.Name) version $($IdeInfo.Version)
Installation Path: $($IdeInfo.Path)
Plugins Directory: $($IdeInfo.PluginsPath)

RECOMMENDED PLUGINS TO INSTALL:
================================================================================
"@

    foreach ($pluginName in $PluginNames) {
        if ($PluginCatalog.ContainsKey($pluginName)) {
            $plugin = $PluginCatalog[$pluginName]
            $guideContent += @"

- $($plugin.Name) (ID: $($plugin.Id))
  Marketplace URL: $($plugin.MarketplaceUrl)
  
"@
        }
    }

    $guideContent += @"

INSTALLATION METHODS:
================================================================================

METHOD 1: Install from IDE (Recommended)
-----------------------------------------
1. Open $($IdeInfo.Name)
2. Go to File > Settings (or Preferences on macOS)
3. Navigate to Plugins
4. Click on Marketplace tab
5. Search for each plugin by name
6. Click Install and restart IDE when prompted

METHOD 2: Install from JetBrains Marketplace Website
----------------------------------------------------
1. Visit the marketplace URL for each plugin listed above
2. Click "Install to IDE" button
3. Select your $($IdeInfo.Name) installation
4. The IDE will handle the installation

METHOD 3: Manual Installation
-----------------------------
1. Download plugin ZIP from marketplace URL
2. Open $($IdeInfo.Name)
3. Go to File > Settings > Plugins
4. Click gear icon > Install Plugin from Disk
5. Select the downloaded ZIP file
6. Restart IDE

PLUGIN IDS FOR COMMAND LINE INSTALLATION:
================================================================================
"@

    foreach ($pluginName in $PluginNames) {
        if ($PluginCatalog.ContainsKey($pluginName)) {
            $plugin = $PluginCatalog[$pluginName]
            $guideContent += "  $($plugin.Id)`n"
        }
    }

    $guideContent += @"

NOTES:
================================================================================
- Some plugins may require IDE restart after installation
- Ensure your IDE is up to date for best compatibility
- ChatGPT Assistant plugin requires API key configuration after installation

================================================================================
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
================================================================================
"@

    Set-Content -Path $guidePath -Value $guideContent
    Write-Log "Installation guide created: $guidePath" "SUCCESS"
    
    return $guidePath
}

function Wait-ForIDEProfile {
    param(
        [string]$PluginsPath,
        [int]$MaxWaitSeconds = 60
    )
    
    $waited = 0
    while ($waited -lt $MaxWaitSeconds) {
        if (Test-Path (Split-Path $PluginsPath -Parent)) {
            Write-Log "IDE profile directory found"
            
            # Ensure plugins directory exists
            if (!(Test-Path $PluginsPath)) {
                New-Item -ItemType Directory -Path $PluginsPath -Force | Out-Null
                Write-Log "Created plugins directory: $PluginsPath"
            }
            return $true
        }
        
        Start-Sleep -Seconds 5
        $waited += 5
        Write-Log "Waiting for IDE profile creation... ($waited/$MaxWaitSeconds seconds)"
    }
    
    return $false
}

# Main execution
Write-Log "========================================="
Write-Log "JetBrains Plugin Auto-Installer Started"
Write-Log "========================================="

# Find installed IDEs
$installedIDEs = Find-JetBrainsIDEs

if ($installedIDEs.Count -eq 0) {
    Write-Log "No JetBrains IDEs found." "WARNING"
    
    # Create a setup guide for future installation
    $setupGuidePath = Join-Path $PSScriptRoot "JETBRAINS_SETUP_GUIDE.txt"
    $setupGuide = @"
================================================================================
JETBRAINS IDE SETUP GUIDE
================================================================================

No JetBrains IDEs are currently installed on this system.

AFTER INSTALLING A JETBRAINS IDE:
---------------------------------
1. Run this script again to auto-configure plugins
2. The script will detect the installed IDE and guide you through plugin setup

RECOMMENDED IDES:
----------------
- IntelliJ IDEA (Java/Kotlin development)
- PyCharm (Python development)
- WebStorm (JavaScript/TypeScript development)
- GoLand (Go development)
- PhpStorm (PHP development)
- Rider (.NET development)

INSTALLATION OPTIONS:
--------------------
1. Download from: https://www.jetbrains.com/
2. Use JetBrains Toolbox for easier management: https://www.jetbrains.com/toolbox-app/

To run this installer again after IDE installation:
powershell -ExecutionPolicy Bypass -File "$PSCommandPath"

================================================================================
"@
    Set-Content -Path $setupGuidePath -Value $setupGuide
    Write-Log "Setup guide created: $setupGuidePath" "MANUAL"
    Write-Log "Please install a JetBrains IDE first, then run this script again." "MANUAL"
    exit 0
}

# Process each installed IDE
foreach ($ide in $installedIDEs) {
    Write-Log "Processing: $($ide.Name)"
    
    # Wait for IDE profile if needed
    if (!(Wait-ForIDEProfile -PluginsPath $ide.PluginsPath)) {
        Write-Log "IDE profile not ready for $($ide.Name), skipping..." "WARNING"
        continue
    }
    
    # Determine which plugins to install
    $pluginsToInstall = @()
    $ideKey = $ide.Name -replace '\s+', ''
    
    if ($IdeSpecificPlugins.ContainsKey($ideKey)) {
        $pluginsToInstall = $IdeSpecificPlugins[$ideKey]
    } else {
        $pluginsToInstall = @("ChatGPT_Assistant", "GitToolBox")
    }
    
    Write-Log "Recommended plugins for $($ide.Name): $($pluginsToInstall -join ', ')"
    
    # Try automated installation
    $autoInstallSuccess = $false
    foreach ($pluginName in $pluginsToInstall) {
        if ($PluginCatalog.ContainsKey($pluginName)) {
            $plugin = $PluginCatalog[$pluginName]
            
            for ($retry = 1; $retry -le $MaxRetries; $retry++) {
                if ($retry -gt 1) {
                    Write-Log "Retry attempt $retry for $($plugin.Name)" "WARNING"
                    Start-Sleep -Seconds 5
                }
                
                if (Install-PluginViaIDE -IdeInfo $ide -PluginId $plugin.Id) {
                    $autoInstallSuccess = $true
                    break
                }
            }
        }
    }
    
    # Create installation guide
    $guidePath = Create-PluginInstallationGuide -IdeInfo $ide -PluginNames $pluginsToInstall
    
    if (!$autoInstallSuccess -or $ManualMode) {
        Write-Log "==================================" "MANUAL"
        Write-Log "MANUAL INSTALLATION REQUIRED" "MANUAL"
        Write-Log "==================================" "MANUAL"
        Write-Log "Automated plugin installation was not successful." "MANUAL"
        Write-Log "Please follow the instructions in: $guidePath" "MANUAL"
        Write-Log "" "MANUAL"
        Write-Log "You can also open the guide with: notepad `"$guidePath`"" "MANUAL"
        
        # Optionally open the guide
        $openGuide = Read-Host "Would you like to open the installation guide now? (Y/N)"
        if ($openGuide -eq 'Y' -or $openGuide -eq 'y') {
            Start-Process notepad -ArgumentList $guidePath
        }
    }
}

Write-Log "========================================="
Write-Log "Plugin installation process completed"
Write-Log "========================================="

if ($installedIDEs.Count -gt 0) {
    Write-Log "Please restart your IDE(s) to ensure all plugins are properly loaded." "SUCCESS"
}
