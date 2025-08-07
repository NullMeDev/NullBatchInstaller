@echo off
REM Mock installer that simulates real installer behavior
REM This script responds to common silent installation flags

setlocal enabledelayedexpansion

REM Parse command line arguments
set "silent_mode=false"
set "installer_name=%~n0"

:parse_args
if "%~1"=="" goto :install_start
if /i "%~1"=="/S" set "silent_mode=true"
if /i "%~1"=="/silent" set "silent_mode=true"
if /i "%~1"=="/quiet" set "silent_mode=true"
if /i "%~1"=="/VERYSILENT" set "silent_mode=true"
if /i "%~1"=="/qn" set "silent_mode=true"
shift
goto :parse_args

:install_start
if "%silent_mode%"=="true" (
    REM Silent installation mode
    echo [SILENT] Installing %installer_name%...
    
    REM Simulate installation progress
    timeout /t 1 >nul 2>&1
    echo [SILENT] Progress: 25%%
    
    timeout /t 1 >nul 2>&1
    echo [SILENT] Progress: 50%%
    
    timeout /t 1 >nul 2>&1
    echo [SILENT] Progress: 75%%
    
    timeout /t 1 >nul 2>&1
    echo [SILENT] Progress: 100%%
    
    echo [SILENT] Installation of %installer_name% completed successfully.
    exit /b 0
) else (
    REM Interactive mode (should not be used by NullInstaller)
    echo [INTERACTIVE] This installer would normally show a GUI.
    echo [INTERACTIVE] NullInstaller should use silent flags to avoid this.
    echo [INTERACTIVE] Supported silent flags: /S, /silent, /quiet, /VERYSILENT, /qn
    exit /b 1
)
