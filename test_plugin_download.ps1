# Test script to verify JetBrains plugin download functionality

Write-Host "Testing JetBrains Plugin Download..." -ForegroundColor Cyan

# Test downloading ChatGPT Assistant plugin
$pluginId = 21983
$pluginName = "ChatGPT_Assistant"
$buildId = "IC-241.14494.240"
$url = "https://plugins.jetbrains.com/pluginManager?action=download&id=$pluginId&build=$buildId"

Write-Host "`nPlugin: $pluginName (ID: $pluginId)"
Write-Host "URL: $url"
Write-Host "`nAttempting download..."

$testFile = Join-Path $env:TEMP "test_plugin.zip"

try {
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $url -OutFile $testFile -UseBasicParsing -TimeoutSec 30
    $ProgressPreference = 'Continue'
    
    if (Test-Path $testFile) {
        $fileInfo = Get-Item $testFile
        Write-Host "Success! Downloaded file: $($fileInfo.Name)" -ForegroundColor Green
        Write-Host "File size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
        
        # Check if it's a valid ZIP file
        try {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $zip = [System.IO.Compression.ZipFile]::OpenRead($testFile)
            Write-Host "Valid ZIP file with $($zip.Entries.Count) entries" -ForegroundColor Green
            $zip.Dispose()
        } catch {
            Write-Host "Warning: File may not be a valid ZIP archive" -ForegroundColor Yellow
        }
        
        # Clean up
        Remove-Item $testFile -Force
        Write-Host "`nTest completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Failed: File not created" -ForegroundColor Red
    }
} catch {
    Write-Host "Error during download: $_" -ForegroundColor Red
    Write-Host "This might be due to network restrictions or marketplace API changes" -ForegroundColor Yellow
}
