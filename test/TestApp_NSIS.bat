@echo off
echo Installing TestApp_NSIS (NSIS-style)...
if "%~1"=="/S" (
    echo [SILENT] NSIS installer running silently
    ping -n 3 127.0.0.1 >nul 2>&1
    echo Installation completed successfully
    exit /b 0
) else (
    echo This would show NSIS GUI - use /S for silent install
    exit /b 1
)