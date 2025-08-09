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
