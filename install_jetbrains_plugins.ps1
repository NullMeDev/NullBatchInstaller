# JetBrains IDE Plugin Auto-Installer
# This script automatically installs plugins for IntelliJ-family IDEs after installation

param(
    [string]$IdeType = "",  # Optional: specific IDE type (e.g., "IntelliJIdea", "PyCharm", "WebStorm")
    [int]$MaxRetries = 3
)

# Plugin catalog with IDs
$PluginCatalog = @{
    "ChatGPT_Assistant" = 21983
    "GitToolBox" = 7499
    "Key_Promoter_X" = 9792
    "Rainbow_Brackets" = 10080
    "Tabnine_AI" = 12798
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
    "AppCode" = @("ChatGPT_Assistant", "GitToolBox")
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    switch ($Level) {
        "ERROR" { Write-Host $logMessage -ForegroundColor Red }
        "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $logMessage -ForegroundColor Green }
        default { Write-Host $logMessage }
    }
    
    # Also write to log file
    $logFile = Join-Path $PSScriptRoot "jetbrains_plugin_install.log"
    Add-Content -Path $logFile -Value $logMessage
}

function Find-JetBrainsIDEs {
    Write-Log "Searching for installed JetBrains IDEs..."
    
    $jetbrainsPath = "$env:APPDATA\JetBrains"
    $foundIDEs = @()
    
    if (Test-Path $jetbrainsPath) {
        $ideFolders = Get-ChildItem -Path $jetbrainsPath -Directory | Where-Object { $_.Name -match '2024\.' }
        
        foreach ($folder in $ideFolders) {
            $ideName = $folder.Name -replace '2024\..*', ''
            $ideVersion = $folder.Name -replace '^[^2]*', ''
            
            $ideInfo = @{
                Name = $ideName
                Version = $ideVersion
                Path = $folder.FullName
                PluginsPath = Join-Path $folder.FullName "config\plugins"
            }
            
            $foundIDEs += $ideInfo
            Write-Log "Found IDE: $ideName version $ideVersion at $($folder.FullName)"
        }
    }
    
    return $foundIDEs
}

function Download-Plugin {
    param(
        [string]$PluginId,
        [string]$PluginName,
        [string]$DestinationPath
    )
    
    $buildId = "IC-241.14494.240"  # IntelliJ Community build ID for compatibility
    $url = "https://plugins.jetbrains.com/pluginManager?action=download&id=$PluginId&build=$buildId"
    
    $zipPath = Join-Path $DestinationPath "$PluginName.zip"
    
    Write-Log "Downloading plugin: $PluginName (ID: $PluginId)"
    Write-Log "URL: $url"
    
    try {
        # Create destination directory if it doesn't exist
        if (!(Test-Path $DestinationPath)) {
            New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
        }
        
        # Download the plugin
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing
        $ProgressPreference = 'Continue'
        
        if (Test-Path $zipPath) {
            Write-Log "Successfully downloaded: $PluginName" "SUCCESS"
            return $zipPath
        } else {
            Write-Log "Failed to download: $PluginName" "ERROR"
            return $null
        }
    } catch {
        Write-Log "Error downloading $PluginName : $_" "ERROR"
        return $null
    }
}

function Install-Plugin {
    param(
        [string]$ZipPath,
        [string]$PluginsDirectory,
        [string]$PluginName
    )
    
    try {
        Write-Log "Installing plugin: $PluginName to $PluginsDirectory"
        
        # Create plugins directory if it doesn't exist
        if (!(Test-Path $PluginsDirectory)) {
            New-Item -ItemType Directory -Path $PluginsDirectory -Force | Out-Null
            Write-Log "Created plugins directory: $PluginsDirectory"
        }
        
        # Extract the plugin
        $extractPath = Join-Path $PluginsDirectory $PluginName
        
        # Remove existing plugin folder if it exists
        if (Test-Path $extractPath) {
            Remove-Item -Path $extractPath -Recurse -Force
            Write-Log "Removed existing plugin folder: $extractPath"
        }
        
        # Extract ZIP file
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $PluginsDirectory)
        
        Write-Log "Successfully installed: $PluginName" "SUCCESS"
        
        # Clean up ZIP file
        Remove-Item -Path $ZipPath -Force
        
        return $true
    } catch {
        Write-Log "Error installing $PluginName : $_" "ERROR"
        return $false
    }
}

function Install-PluginsForIDE {
    param(
        [hashtable]$IdeInfo,
        [string[]]$PluginNames
    )
    
    Write-Log "Installing plugins for $($IdeInfo.Name) at $($IdeInfo.Path)"
    
    $successCount = 0
    $failureCount = 0
    
    foreach ($pluginName in $PluginNames) {
        if ($PluginCatalog.ContainsKey($pluginName)) {
            $pluginId = $PluginCatalog[$pluginName]
            
            $retryCount = 0
            $installed = $false
            
            while ($retryCount -lt $MaxRetries -and !$installed) {
                if ($retryCount -gt 0) {
                    Write-Log "Retry attempt $retryCount for $pluginName" "WARNING"
                    Start-Sleep -Seconds 5
                }
                
                $zipPath = Download-Plugin -PluginId $pluginId -PluginName $pluginName -DestinationPath $env:TEMP
                
                if ($zipPath) {
                    $installed = Install-Plugin -ZipPath $zipPath -PluginsDirectory $IdeInfo.PluginsPath -PluginName $pluginName
                }
                
                $retryCount++
            }
            
            if ($installed) {
                $successCount++
            } else {
                $failureCount++
                Write-Log "Failed to install $pluginName after $MaxRetries attempts" "ERROR"
            }
        } else {
            Write-Log "Plugin $pluginName not found in catalog" "WARNING"
        }
    }
    
    Write-Log "Installation complete for $($IdeInfo.Name): $successCount successful, $failureCount failed" $(if ($failureCount -eq 0) { "SUCCESS" } else { "WARNING" })
    
    return @{
        Success = $successCount
        Failed = $failureCount
        Total = $PluginNames.Count
    }
}

function Try-IDECommandLineInstall {
    param(
        [hashtable]$IdeInfo,
        [string[]]$PluginIds
    )
    
    # Look for idea.exe or similar executable
    $possibleExePaths = @(
        "$env:ProgramFiles\JetBrains\$($IdeInfo.Name) $($IdeInfo.Version)\bin\idea64.exe",
        "$env:ProgramFiles\JetBrains\$($IdeInfo.Name) $($IdeInfo.Version)\bin\idea.exe",
        "${env:ProgramFiles(x86)}\JetBrains\$($IdeInfo.Name) $($IdeInfo.Version)\bin\idea.exe",
        "$env:LocalAppData\JetBrains\Toolbox\apps\$($IdeInfo.Name)\ch-0\$($IdeInfo.Version)\bin\idea64.exe"
    )
    
    $ideExe = $null
    foreach ($path in $possibleExePaths) {
        if (Test-Path $path) {
            $ideExe = $path
            break
        }
    }
    
    if ($ideExe) {
        Write-Log "Found IDE executable: $ideExe"
        Write-Log "Attempting command-line plugin installation..."
        
        foreach ($pluginId in $PluginIds) {
            try {
                $arguments = "installPlugins $pluginId"
                Start-Process -FilePath $ideExe -ArgumentList $arguments -Wait -NoNewWindow
                Write-Log "Installed plugin via CLI: $pluginId" "SUCCESS"
            } catch {
                Write-Log "CLI installation failed for $pluginId : $_" "WARNING"
                return $false
            }
        }
        return $true
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
    Write-Log "No JetBrains IDEs found. Waiting for IDE installation..." "WARNING"
    
    # Monitor for IDE installation (check every 30 seconds for up to 10 minutes)
    $maxWaitTime = 600  # 10 minutes
    $checkInterval = 30  # 30 seconds
    $elapsed = 0
    
    while ($elapsed -lt $maxWaitTime) {
        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
        
        $installedIDEs = Find-JetBrainsIDEs
        if ($installedIDEs.Count -gt 0) {
            Write-Log "IDE installation detected!" "SUCCESS"
            break
        }
        
        Write-Log "Still waiting for IDE installation... ($elapsed/$maxWaitTime seconds)"
    }
    
    if ($installedIDEs.Count -eq 0) {
        Write-Log "No IDE installation detected within timeout period." "ERROR"
        exit 1
    }
}

# Process each installed IDE
$overallResults = @()

foreach ($ide in $installedIDEs) {
    Write-Log "Processing: $($ide.Name)"
    
    # Determine which plugins to install
    $pluginsToInstall = @()
    
    if ($IdeType -and $ide.Name -eq $IdeType) {
        # Install plugins for specific IDE type
        if ($IdeSpecificPlugins.ContainsKey($IdeType)) {
            $pluginsToInstall = $IdeSpecificPlugins[$IdeType]
        } else {
            # Default to core plugins if IDE type not in catalog
            $pluginsToInstall = @("ChatGPT_Assistant", "GitToolBox")
        }
    } elseif (!$IdeType) {
        # Install plugins based on detected IDE type
        $ideKey = $ide.Name -replace '\s+', ''
        if ($IdeSpecificPlugins.ContainsKey($ideKey)) {
            $pluginsToInstall = $IdeSpecificPlugins[$ideKey]
        } else {
            # Default to core plugins
            $pluginsToInstall = @("ChatGPT_Assistant", "GitToolBox")
        }
    }
    
    if ($pluginsToInstall.Count -gt 0) {
        # Try CLI installation first
        $pluginIds = $pluginsToInstall | ForEach-Object { $PluginCatalog[$_] }
        $cliSuccess = Try-IDECommandLineInstall -IdeInfo $ide -PluginIds $pluginIds
        
        if (!$cliSuccess) {
            # Fall back to manual installation
            $result = Install-PluginsForIDE -IdeInfo $ide -PluginNames $pluginsToInstall
            $overallResults += $result
        } else {
            $overallResults += @{
                Success = $pluginsToInstall.Count
                Failed = 0
                Total = $pluginsToInstall.Count
            }
        }
    }
}

# Summary
Write-Log "========================================="
Write-Log "Installation Summary"
Write-Log "========================================="

$totalSuccess = ($overallResults | Measure-Object -Property Success -Sum).Sum
$totalFailed = ($overallResults | Measure-Object -Property Failed -Sum).Sum
$totalPlugins = ($overallResults | Measure-Object -Property Total -Sum).Sum

Write-Log "Total plugins installed: $totalSuccess/$totalPlugins" $(if ($totalFailed -eq 0) { "SUCCESS" } else { "WARNING" })

if ($totalFailed -gt 0) {
    Write-Log "Some plugins failed to install. Check the log for details." "WARNING"
    exit 1
} else {
    Write-Log "All plugins installed successfully!" "SUCCESS"
    exit 0
}
