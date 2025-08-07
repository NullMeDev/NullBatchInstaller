@echo off
echo Installing TestApp_LongRunning (for cancellation testing)...
if "%~1"=="/S" (
    echo [SILENT] Long-running installer started
    ping -n 11 127.0.0.1 >nul 2>&1
    echo Installation completed successfully
    exit /b 0
) else (
    echo This installer takes a long time - use /S for silent install
    exit /b 1
)