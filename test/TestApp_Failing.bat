@echo off
echo Installing TestApp_Failing (will fail for testing)...
if "%~1"=="/S" (
    echo [SILENT] This installer will fail intentionally
    ping -n 2 127.0.0.1 >nul 2>&1
    echo Installation failed with error
    exit /b 1
) else (
    echo This would show GUI but will fail - use /S for silent install
    exit /b 1
)