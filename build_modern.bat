@echo off
echo =================================
echo  NullInstaller Modern Build Script
echo =================================

REM Check if Go SDK or C# compiler is available
echo Checking for compilers...

dotnet --version >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ .NET SDK found, using dotnet build
    goto :dotnet_build
)

where csc >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ C# compiler found, using csc
    goto :csc_build
)

echo ❌ No compatible C# compiler found
echo Please install either:
echo   - .NET SDK (recommended): https://dotnet.microsoft.com/download
echo   - Visual Studio with C# support
exit /b 1

:dotnet_build
echo Building with .NET SDK...
echo Creating project file...
echo ^<Project Sdk="Microsoft.NET.Sdk"^> > NullInstaller.csproj
echo   ^<PropertyGroup^> >> NullInstaller.csproj
echo     ^<OutputType^>WinExe^</OutputType^> >> NullInstaller.csproj
echo     ^<TargetFramework^>net6.0-windows^</TargetFramework^> >> NullInstaller.csproj
echo     ^<UseWindowsForms^>true^</UseWindowsForms^> >> NullInstaller.csproj
echo     ^<AssemblyTitle^>NullInstaller^</AssemblyTitle^> >> NullInstaller.csproj
echo     ^<AssemblyDescription^>Modern Universal Installer Tool^</AssemblyDescription^> >> NullInstaller.csproj
echo     ^<AssemblyVersion^>2.0.0.0^</AssemblyVersion^> >> NullInstaller.csproj
echo   ^</PropertyGroup^> >> NullInstaller.csproj
echo ^</Project^> >> NullInstaller.csproj

echo Compiling application...
dotnet build -c Release -o dist
if %errorlevel% neq 0 (
    echo ❌ Build failed
    exit /b 1
)

echo ✓ Build successful!
echo Output: %cd%\dist\NullInstaller.exe
goto :end

:csc_build
echo Building with C# compiler...
echo Creating dist directory...
if not exist dist mkdir dist

echo Compiling application...
csc /target:winexe /out:dist\NullInstaller_Modern.exe /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Data.dll /optimize+ NullInstaller_Modern.cs
if %errorlevel% neq 0 (
    echo ❌ Build failed
    exit /b 1
)

echo ✓ Build successful!
echo Output: %cd%\dist\NullInstaller_Modern.exe

:end
echo.
echo Application built successfully!
echo To run: cd dist && NullInstaller*.exe
echo.
echo Features:
echo - 60+ programs across 4 categories
echo - Modern tabbed interface
echo - Drag & drop support
echo - Silent installation
echo - Real-time progress tracking
echo - Activity logging
echo - Automatic local file detection
echo.
pause
