@echo off
echo Building NullInstaller...
echo.

REM Build in release mode
cargo build --release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Copying executable to root directory...
    copy /Y target\release\NullInstaller.exe .
    echo.
    echo NullInstaller.exe is ready!
    echo Size: 
    dir /b NullInstaller.exe | findstr .
) else (
    echo.
    echo Build failed! Please check the error messages above.
    exit /b 1
)
