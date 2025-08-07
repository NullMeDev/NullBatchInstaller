@echo off
echo Installing TestApp_InstallShield...
if "%~1"=="/silent" (
    echo [SILENT] InstallShield installer running silently
    ping -n 4 127.0.0.1 >nul 2>&1
    echo Installation completed successfully
    exit /b 0
) else (
    echo This would show InstallShield GUI - use /silent for silent install
    exit /b 1
)