# Test script to verify JetBrains plugin download functionality - Updated API

Write-Host "Testing JetBrains Plugin Download (Updated API)..." -ForegroundColor Cyan

# Test downloading ChatGPT Assistant plugin using different API endpoints
$pluginId = 21983
$pluginName = "ChatGPT_Assistant"

# Try different API endpoint formats
$endpoints = @(
    "https://plugins.jetbrains.com/plugin/download?pluginId=$pluginId",
    "https://plugins.jetbrains.com/files/21983/535400/ChatGPT-3.3.7.zip",  # Direct file URL for ChatGPT plugin
    "https://plugins.jetbrains.com/api/plugins/$pluginId/updates",
    "https://plugins.jetbrains.com/pluginManager?action=download&id=$pluginId"
)

foreach ($url in $endpoints) {
    Write-Host "`n----------------------------------------"
    Write-Host "Testing URL: $url" -ForegroundColor Yellow
    
    $testFile = Join-Path $env:TEMP "test_plugin_$([guid]::NewGuid().ToString('N').Substring(0,8)).zip"
    
    try {
        $ProgressPreference = 'SilentlyContinue'
        $response = Invoke-WebRequest -Uri $url -OutFile $testFile -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        $ProgressPreference = 'Continue'
        
        if (Test-Path $testFile) {
            $fileInfo = Get-Item $testFile
            if ($fileInfo.Length -gt 0) {
                Write-Host "Success! Downloaded file: $($fileInfo.Name)" -ForegroundColor Green
                Write-Host "File size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
                
                # Check if it's a valid ZIP file
                try {
                    Add-Type -AssemblyName System.IO.Compression.FileSystem
                    $zip = [System.IO.Compression.ZipFile]::OpenRead($testFile)
                    Write-Host "Valid ZIP file with $($zip.Entries.Count) entries" -ForegroundColor Green
                    $zip.Dispose()
                    
                    Write-Host "`nThis endpoint works!" -ForegroundColor Green
                    Remove-Item $testFile -Force
                    break
                } catch {
                    Write-Host "Not a valid ZIP file" -ForegroundColor Yellow
                }
            } else {
                Write-Host "Empty file downloaded" -ForegroundColor Yellow
            }
            Remove-Item $testFile -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Host "Failed: $_" -ForegroundColor Red
    }
}

Write-Host "`n----------------------------------------"
Write-Host "Alternative approach: Getting plugin info via API" -ForegroundColor Cyan

# Try to get plugin information first
$infoUrl = "https://plugins.jetbrains.com/api/plugins/$pluginId"
try {
    $pluginInfo = Invoke-RestMethod -Uri $infoUrl -Method Get -ErrorAction Stop
    Write-Host "Plugin Name: $($pluginInfo.name)" -ForegroundColor Green
    Write-Host "Plugin ID: $($pluginInfo.id)" -ForegroundColor Green
    
    if ($pluginInfo.downloads) {
        Write-Host "Download URLs found:" -ForegroundColor Green
        $pluginInfo.downloads | ForEach-Object { Write-Host "  - $_" }
    }
} catch {
    Write-Host "Could not fetch plugin info: $_" -ForegroundColor Yellow
}
