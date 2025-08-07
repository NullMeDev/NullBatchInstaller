@echo off
echo ========================================
echo    Building C# NullInstaller 
echo    (Much better than Go + Fyne!)
echo ========================================
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET is not installed
    echo.
    echo SOLUTION: 
    echo 1. Download .NET 8.0 SDK: https://dotnet.microsoft.com/download
    echo 2. Install and restart this script
    echo.
    echo ALTERNATIVE: Use csc.exe (comes with Windows)
    goto :csc_build
)

echo Using .NET SDK to build...
echo Creating project file...

REM Create a simple project file
(
echo ^<Project Sdk="Microsoft.NET.Sdk"^>
echo   ^<PropertyGroup^>
echo     ^<OutputType^>WinExe^</OutputType^>
echo     ^<TargetFramework^>net6.0-windows^</TargetFramework^>
echo     ^<UseWindowsForms^>true^</UseWindowsForms^>
echo     ^<PublishSingleFile^>true^</PublishSingleFile^>
echo     ^<SelfContained^>true^</SelfContained^>
echo   ^</PropertyGroup^>
echo ^</Project^>
) > NullInstaller.csproj

echo Building single EXE file...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

if exist "bin\Release\net6.0-windows\win-x64\publish\NullInstaller.exe" (
    copy "bin\Release\net6.0-windows\win-x64\publish\NullInstaller.exe" .
    echo.
    echo ✅ SUCCESS! NullInstaller.exe created
    echo   Size: Single file, ~30-50MB (includes .NET runtime)
    echo   Dependencies: NONE - runs on any Windows 10+ machine
    echo.
    echo To run: Double-click NullInstaller.exe
    goto :end
)

:csc_build
echo.
echo Using Windows built-in C# compiler...
echo This creates a smaller EXE but needs .NET Framework

REM Use the built-in C# compiler (comes with Windows)
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:NullInstaller.exe NullInstaller.cs

if exist NullInstaller.exe (
    echo.
    echo ✅ SUCCESS! NullInstaller.exe created with built-in compiler
    echo   Size: ~10-20KB (needs .NET Framework - already on Windows)
    echo   Dependencies: .NET Framework 4.0+ (pre-installed on Windows)
    echo.
) else (
    echo.
    echo ❌ Build failed. Try installing .NET SDK or Visual Studio
)

:end
if exist NullInstaller.exe (
    echo.
    echo 🚀 Ready to run!
    echo   .\NullInstaller.exe
    echo.
    echo Features:
    echo   ✅ Native Windows GUI (WinForms)
    echo   ✅ 20 Universal programs with auto-download
    echo   ✅ Verbose logging toggle
    echo   ✅ Drag & drop support
    echo   ✅ No CGO issues!
    echo   ✅ Small, fast, native Windows app
    echo.
    choice /M "Run NullInstaller now"
    if !errorlevel!==1 start NullInstaller.exe
)

pause
