# JetBrains IDE Plugin Integration Module
# Integrates with NullInstaller to automatically install plugins after IDE installation

param(
    [Parameter(Position=0)]
    [string]$Action = "check",  # check, install, monitor
    [string]$IdeType = "",
    [switch]$Silent
)

# Configuration
$script:PluginCatalog = @{
    "ChatGPT" = @{
        Id = 21983
        Name = "ChatGPT Assistant"
        Description = "AI-powered coding assistant"
        Priority = 1
    }
    "GitToolBox" = @{
        Id = 7499
        Name = "GitToolBox"
        Description = "Git integration enhancements"
        Priority = 2
    }
    "KeyPromoterX" = @{
        Id = 9792
        Name = "Key Promoter X"
        Description = "Learn keyboard shortcuts"
        Priority = 3
    }
    "RainbowBrackets" = @{
        Id = 10080
        Name = "Rainbow Brackets"
        Description = "Colorful bracket matching"
        Priority = 4
    }
    "TabnineAI" = @{
        Id = 12798
        Name = "Tabnine AI"
        Description = "AI code completion"
        Priority = 5
    }
}

$script:IdePluginProfiles = @{
    "IntelliJIdea" = @("ChatGPT", "GitToolBox", "KeyPromoterX", "RainbowBrackets", "TabnineAI")
    "PyCharm" = @("ChatGPT", "GitToolBox", "KeyPromoterX", "RainbowBrackets")
    "WebStorm" = @("ChatGPT", "GitToolBox", "RainbowBrackets")
    "PhpStorm" = @("ChatGPT", "GitToolBox", "KeyPromoterX")
    "GoLand" = @("ChatGPT", "GitToolBox", "KeyPromoterX")
    "DataGrip" = @("ChatGPT", "GitToolBox")
    "Rider" = @("ChatGPT", "GitToolBox", "KeyPromoterX", "RainbowBrackets")
    "CLion" = @("ChatGPT", "GitToolBox", "KeyPromoterX")
    "RubyMine" = @("ChatGPT", "GitToolBox", "RainbowBrackets")
}

# Logging
function Write-PluginLog {
    param(
        [string]$Message,
        [string]$Level = "INFO",
        [switch]$NoConsole
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [PLUGIN] [$Level] $Message"
    
    if (!$Silent -and !$NoConsole) {
        switch ($Level) {
            "ERROR" { Write-Host $logMessage -ForegroundColor Red }
            "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
            "SUCCESS" { Write-Host $logMessage -ForegroundColor Green }
            "DEBUG" { Write-Host $logMessage -ForegroundColor Gray }
            default { Write-Host $logMessage }
        }
    }
    
    $logFile = Join-Path $PSScriptRoot "jetbrains_plugin.log"
    Add-Content -Path $logFile -Value $logMessage -ErrorAction SilentlyContinue
}

# IDE Detection
function Find-InstalledIDEs {
    $ides = @()
    
    # Standard installation paths
    $searchPaths = @(
        "$env:ProgramFiles\JetBrains",
        "${env:ProgramFiles(x86)}\JetBrains",
        "$env:LOCALAPPDATA\JetBrains\Toolbox\apps"
    )
    
    foreach ($basePath in $searchPaths) {
        if (Test-Path $basePath) {
            $ideFolders = Get-ChildItem -Path $basePath -Directory -ErrorAction SilentlyContinue
            
            foreach ($folder in $ideFolders) {
                $ideName = $folder.Name -replace '\s*\d+\.\d+.*$', ''
                $ideExe = Get-ChildItem -Path $folder.FullName -Filter "*.exe" -Recurse -ErrorAction SilentlyContinue |
                          Where-Object { $_.Name -match '64\.exe$|^idea\.exe$' } |
                          Select-Object -First 1
                
                if ($ideExe) {
                    $ides += @{
                        Name = $ideName
                        Path = $folder.FullName
                        Executable = $ideExe.FullName
                        Version = $folder.Name -replace '^[^\d]*', ''
                    }
                }
            }
        }
    }
    
    # Check AppData for configurations
    $configPath = "$env:APPDATA\JetBrains"
    if (Test-Path $configPath) {
        $configFolders = Get-ChildItem -Path $configPath -Directory -ErrorAction SilentlyContinue
        
        foreach ($folder in $configFolders) {
            $ideName = $folder.Name -replace '20\d{2}\.\d+.*$', ''
            $ideVersion = $folder.Name -replace '^[^2]*', ''
            
            # Check if we already found this IDE
            $existing = $ides | Where-Object { $_.Name -eq $ideName }
            if (!$existing) {
                $ides += @{
                    Name = $ideName
                    Path = $folder.FullName
                    ConfigPath = $folder.FullName
                    Version = $ideVersion
                    PluginsPath = Join-Path $folder.FullName "config\plugins"
                }
            } else {
                $existing.ConfigPath = $folder.FullName
                $existing.PluginsPath = Join-Path $folder.FullName "config\plugins"
            }
        }
    }
    
    return $ides
}

# Plugin Installation
function Install-PluginsForIDE {
    param(
        [hashtable]$Ide,
        [string[]]$PluginKeys
    )
    
    Write-PluginLog "Installing plugins for $($Ide.Name)"
    
    # Ensure plugins directory exists
    if ($Ide.PluginsPath) {
        $pluginsDir = $Ide.PluginsPath
    } else {
        $pluginsDir = "$env:APPDATA\JetBrains\$($Ide.Name)$($Ide.Version)\config\plugins"
    }
    
    if (!(Test-Path $pluginsDir)) {
        New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
        Write-PluginLog "Created plugins directory: $pluginsDir"
    }
    
    $installed = 0
    $failed = 0
    
    foreach ($pluginKey in $PluginKeys) {
        if ($script:PluginCatalog.ContainsKey($pluginKey)) {
            $plugin = $script:PluginCatalog[$pluginKey]
            
            Write-PluginLog "Processing plugin: $($plugin.Name) (ID: $($plugin.Id))"
            
            # Try command-line installation if executable is available
            if ($Ide.Executable -and (Test-Path $Ide.Executable)) {
                try {
                    $args = "installPlugins", "com.intellij.marketplace:$($plugin.Id)"
                    $process = Start-Process -FilePath $Ide.Executable -ArgumentList $args -PassThru -WindowStyle Hidden
                    $process.WaitForExit(30000)  # 30 second timeout
                    
                    if ($process.ExitCode -eq 0) {
                        Write-PluginLog "Successfully installed $($plugin.Name)" "SUCCESS"
                        $installed++
                        continue
                    }
                } catch {
                    Write-PluginLog "CLI installation failed for $($plugin.Name): $_" "WARNING"
                }
            }
            
            # Create marker file for manual installation
            $markerFile = Join-Path $pluginsDir "INSTALL_$($plugin.Id).txt"
            @"
Plugin to Install: $($plugin.Name)
Plugin ID: $($plugin.Id)
Description: $($plugin.Description)
Marketplace URL: https://plugins.jetbrains.com/plugin/$($plugin.Id)

To install manually:
1. Open $($Ide.Name)
2. Go to File > Settings > Plugins
3. Search for "$($plugin.Name)" in Marketplace
4. Click Install and restart IDE
"@ | Set-Content -Path $markerFile
            
            Write-PluginLog "Created installation marker for $($plugin.Name)" "WARNING"
            $failed++
        }
    }
    
    return @{
        Installed = $installed
        Failed = $failed
        Total = $PluginKeys.Count
    }
}

# Main Actions
function Invoke-CheckAction {
    Write-PluginLog "Checking for JetBrains IDEs..."
    
    $ides = Find-InstalledIDEs
    
    if ($ides.Count -eq 0) {
        Write-PluginLog "No JetBrains IDEs found" "WARNING"
        return $false
    }
    
    Write-PluginLog "Found $($ides.Count) IDE(s):" "SUCCESS"
    foreach ($ide in $ides) {
        Write-PluginLog "  - $($ide.Name) v$($ide.Version)"
    }
    
    return $true
}

function Invoke-InstallAction {
    Write-PluginLog "Starting plugin installation process..."
    
    $ides = Find-InstalledIDEs
    
    if ($ides.Count -eq 0) {
        Write-PluginLog "No JetBrains IDEs found to install plugins" "ERROR"
        return $false
    }
    
    $totalResults = @{
        Success = 0
        Failed = 0
    }
    
    foreach ($ide in $ides) {
        # Skip if specific IDE type requested and doesn't match
        if ($IdeType -and $ide.Name -notlike "*$IdeType*") {
            continue
        }
        
        # Get plugin profile for this IDE
        $ideKey = $ide.Name -replace '\s+', ''
        $pluginsToInstall = $script:IdePluginProfiles[$ideKey]
        
        if (!$pluginsToInstall) {
            # Default plugins if IDE not in profiles
            $pluginsToInstall = @("ChatGPT", "GitToolBox")
        }
        
        Write-PluginLog "Processing $($ide.Name) with $($pluginsToInstall.Count) plugins"
        
        $result = Install-PluginsForIDE -Ide $ide -PluginKeys $pluginsToInstall
        
        $totalResults.Success += $result.Installed
        $totalResults.Failed += $result.Failed
    }
    
    Write-PluginLog "========================================="
    Write-PluginLog "Installation Summary:" "SUCCESS"
    Write-PluginLog "  Successful: $($totalResults.Success)"
    Write-PluginLog "  Failed/Manual: $($totalResults.Failed)"
    Write-PluginLog "========================================="
    
    return ($totalResults.Failed -eq 0)
}

function Invoke-MonitorAction {
    Write-PluginLog "Starting IDE monitoring (checking every 30 seconds)..."
    
    $knownIDEs = @()
    $checkCount = 0
    $maxChecks = 120  # 1 hour maximum
    
    while ($checkCount -lt $maxChecks) {
        $currentIDEs = Find-InstalledIDEs
        
        foreach ($ide in $currentIDEs) {
            $ideId = "$($ide.Name)-$($ide.Version)"
            
            if ($ideId -notin $knownIDEs) {
                Write-PluginLog "New IDE detected: $($ide.Name) v$($ide.Version)" "SUCCESS"
                $knownIDEs += $ideId
                
                # Wait for IDE to fully initialize
                Start-Sleep -Seconds 10
                
                # Install plugins for new IDE
                $ideKey = $ide.Name -replace '\s+', ''
                $pluginsToInstall = $script:IdePluginProfiles[$ideKey]
                
                if (!$pluginsToInstall) {
                    $pluginsToInstall = @("ChatGPT", "GitToolBox")
                }
                
                Install-PluginsForIDE -Ide $ide -PluginKeys $pluginsToInstall
            }
        }
        
        Start-Sleep -Seconds 30
        $checkCount++
        
        if ($checkCount % 4 -eq 0) {
            Write-PluginLog "Still monitoring... ($([int]($checkCount/2)) minutes elapsed)" "DEBUG"
        }
    }
    
    Write-PluginLog "Monitoring completed after $([int]($checkCount/2)) minutes"
    return $true
}

# Integration with NullInstaller
function Export-IntegrationHook {
    $hookScript = @'
# JetBrains Plugin Installation Hook
# Add this to your main installer script

function Install-JetBrainsPlugins {
    param([string]$IdeType = "")
    
    $scriptPath = Join-Path $PSScriptRoot "jetbrains_plugin_integration.ps1"
    
    if (Test-Path $scriptPath) {
        & $scriptPath -Action "install" -IdeType $IdeType -Silent:$Silent
    } else {
        Write-Warning "JetBrains plugin integration script not found"
    }
}

# Call after IDE installation
# Example: Install-JetBrainsPlugins -IdeType "PyCharm"
'@
    
    $hookPath = Join-Path $PSScriptRoot "jetbrains_plugin_hook.ps1"
    Set-Content -Path $hookPath -Value $hookScript
    
    Write-PluginLog "Integration hook exported to: $hookPath" "SUCCESS"
    return $hookPath
}

# Main execution
switch ($Action.ToLower()) {
    "check" {
        $result = Invoke-CheckAction
        exit $(if ($result) { 0 } else { 1 })
    }
    "install" {
        $result = Invoke-InstallAction
        exit $(if ($result) { 0 } else { 1 })
    }
    "monitor" {
        $result = Invoke-MonitorAction
        exit $(if ($result) { 0 } else { 1 })
    }
    "export" {
        $hookPath = Export-IntegrationHook
        Write-Host "Hook exported to: $hookPath"
        exit 0
    }
    default {
        Write-PluginLog "Invalid action. Use: check, install, monitor, or export" "ERROR"
        exit 1
    }
}
