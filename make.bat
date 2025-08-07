@echo off
REM NullInstaller Build Script for Windows
REM This script builds the NullInstaller executable with optimization flags

echo Building NullInstaller for Windows...

REM Set CGO_ENABLED to 1 (required for Fyne GUI)
set CGO_ENABLED=1

REM Check if Go is installed
go version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Error: Go is not installed or not in PATH
    echo Please install Go ^>= 1.22 from https://golang.org/
    pause
    exit /b 1
)

REM Check if MinGW/GCC is available (required for CGO with Fyne)
gcc --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Warning: GCC not found in PATH
    echo Fyne requires GCC or MinGW for Windows builds
    echo Install TDM-GCC from https://jmeubank.github.io/tdm-gcc/
    echo or MinGW-w64 from https://www.mingw-w64.org/
    pause
)

REM Create output directory if it doesn't exist
if not exist "dist" mkdir dist

REM Build with optimization flags:
REM -s: strip symbol table and debug info
REM -w: strip DWARF debug info
REM -H=windowsgui: build as Windows GUI application (no console window)
echo Building optimized executable...
go build -ldflags "-s -w -H=windowsgui" -o dist\NullInstaller.exe .

REM Check if build was successful
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Executable created: dist\NullInstaller.exe
    
    REM Show file size
    for %%I in (dist\NullInstaller.exe) do echo File size: %%~zI bytes
    
    echo.
    echo To create a console version (with command window):
    echo go build -ldflags "-s -w" -o dist\NullInstaller-console.exe .
) else (
    echo.
    echo Build failed!
    echo Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Build complete. Press any key to exit...
pause >nul
