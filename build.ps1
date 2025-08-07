# NullInstaller PowerShell Build Script
# Supports Windows, Linux, and macOS builds

param(
    [string]$Target = "windows",
    [switch]$Console,
    [switch]$Clean,
    [switch]$Help
)

function Show-Help {
    Write-Host "NullInstaller Build Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\build.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Target <platform>   Target platform (windows, linux, macos) [default: windows]"
    Write-Host "  -Console             Build with console window (Windows only)"
    Write-Host "  -Clean               Clean dist directory before building"
    Write-Host "  -Help                Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1                    # Build Windows GUI version"
    Write-Host "  .\build.ps1 -Console           # Build Windows console version"
    Write-Host "  .\build.ps1 -Target linux      # Build Linux version"
    Write-Host "  .\build.ps1 -Clean             # Clean and build Windows version"
}

if ($Help) {
    Show-Help
    return
}

Write-Host "NullInstaller Build Script" -ForegroundColor Green
Write-Host "Target: $Target" -ForegroundColor Yellow

# Check if Go is installed
try {
    $goVersion = & go version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Go not found"
    }
    Write-Host "Go found: $goVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: Go is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Go >= 1.22 from https://golang.org/" -ForegroundColor Yellow
    return 1
}

# Check for CGO dependencies based on target platform
$env:CGO_ENABLED = "1"

switch ($Target.ToLower()) {
    "windows" {
        $env:GOOS = "windows"
        $env:GOARCH = "amd64"
        $outputExt = ".exe"
        
        # Check for GCC/MinGW on Windows
        try {
            $gccVersion = & gcc --version 2>$null
            if ($LASTEXITCODE -ne 0) {
                throw "GCC not found"
            }
            Write-Host "GCC found for Windows CGO builds" -ForegroundColor Green
        } catch {
            Write-Host "Warning: GCC not found in PATH" -ForegroundColor Yellow
            Write-Host "Fyne requires GCC or MinGW for Windows builds" -ForegroundColor Yellow
            Write-Host "Install TDM-GCC from https://jmeubank.github.io/tdm-gcc/" -ForegroundColor Yellow
            Write-Host "or MinGW-w64 from https://www.mingw-w64.org/" -ForegroundColor Yellow
        }
    }
    "linux" {
        $env:GOOS = "linux"
        $env:GOARCH = "amd64"
        $outputExt = ""
        Write-Host "Building for Linux (requires build dependencies on target system)" -ForegroundColor Green
    }
    "macos" {
        $env:GOOS = "darwin"
        $env:GOARCH = "amd64"
        $outputExt = ""
        Write-Host "Building for macOS (requires Xcode Command Line Tools on target system)" -ForegroundColor Green
    }
    default {
        Write-Host "Error: Unsupported target platform: $Target" -ForegroundColor Red
        Write-Host "Supported platforms: windows, linux, macos" -ForegroundColor Yellow
        return 1
    }
}

# Create dist directory
$distDir = "dist"
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
    Write-Host "Created dist directory" -ForegroundColor Green
} elseif ($Clean) {
    Remove-Item "$distDir\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Cleaned dist directory" -ForegroundColor Green
}

# Determine build flags and output filename
$baseFilename = "NullInstaller"
$ldflags = "-s -w"  # Strip debug info for smaller binary

if ($Target -eq "windows") {
    if ($Console) {
        $outputFile = "$distDir\${baseFilename}-console${outputExt}"
        Write-Host "Building Windows console version..." -ForegroundColor Yellow
    } else {
        $ldflags += " -H=windowsgui"  # Hide console window for GUI app
        $outputFile = "$distDir\${baseFilename}${outputExt}"
        Write-Host "Building Windows GUI version..." -ForegroundColor Yellow
    }
} else {
    $outputFile = "$distDir\${baseFilename}-${Target}${outputExt}"
    Write-Host "Building $Target version..." -ForegroundColor Yellow
}

# Build the application
Write-Host "Running: go build -ldflags `"$ldflags`" -o $outputFile ." -ForegroundColor Cyan

try {
    & go build -ldflags "$ldflags" -o $outputFile .
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Build successful!" -ForegroundColor Green
        Write-Host "Executable created: $outputFile" -ForegroundColor Green
        
        # Show file size
        if (Test-Path $outputFile) {
            $fileSize = (Get-Item $outputFile).Length
            $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
            Write-Host "File size: $fileSize bytes ($fileSizeMB MB)" -ForegroundColor Cyan
        }
        
        if (($Target -eq "windows") -and (-not $Console)) {
            Write-Host ""
            Write-Host "Note: To create a console version, run:" -ForegroundColor Yellow
            Write-Host ".\build.ps1 -Console" -ForegroundColor Cyan
        }
    } else {
        throw "Build failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Host ""
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above." -ForegroundColor Yellow
    return 1
}

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
