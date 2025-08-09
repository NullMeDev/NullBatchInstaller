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
echo     ^<TargetFramework^>net9.0-windows^</TargetFramework^> >> NullInstaller.csproj
echo     ^<UseWindowsForms^>true^</UseWindowsForms^> >> NullInstaller.csproj
echo     ^<AssemblyTitle^>NullInstaller^</AssemblyTitle^> >> NullInstaller.csproj
echo     ^<AssemblyDescription^>Modern Universal Installer Tool^</AssemblyDescription^> >> NullInstaller.csproj
echo     ^<AssemblyVersion^>2.0.0.0^</AssemblyVersion^> >> NullInstaller.csproj
echo     ^<EnableDefaultCompileItems^>false^</EnableDefaultCompileItems^> >> NullInstaller.csproj
echo     ^<PublishSingleFile^>true^</PublishSingleFile^> >> NullInstaller.csproj
echo     ^<SelfContained^>true^</SelfContained^> >> NullInstaller.csproj
echo     ^<RuntimeIdentifier^>win-x64^</RuntimeIdentifier^> >> NullInstaller.csproj
echo     ^<IncludeAllContentForSelfExtract^>true^</IncludeAllContentForSelfExtract^> >> NullInstaller.csproj
echo   ^</PropertyGroup^> >> NullInstaller.csproj
echo   ^<ItemGroup^> >> NullInstaller.csproj
echo     ^<Compile Include="NullInstaller_Compact.cs" /^> >> NullInstaller.csproj
echo   ^</ItemGroup^> >> NullInstaller.csproj
echo ^</Project^> >> NullInstaller.csproj

echo Creating dist directory if not exists...
if not exist dist mkdir dist

echo Publishing single-file executable...
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true -o dist NullInstaller.csproj
if %errorlevel% neq 0 (
    echo ❌ Build failed
    del NullInstaller.csproj
    exit /b 1
)

REM Check for code signing certificate (optional)
echo Checking for code signing certificate...
where signtool >nul 2>&1
if %errorlevel% equ 0 (
    echo Looking for available certificates...
    signtool sign /a /t http://timestamp.digicert.com /fd SHA256 dist\NullInstallerEnhanced.exe >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✓ Executable signed successfully
    ) else (
        echo ⚠ No valid certificate found, skipping signing
    )
) else (
    echo ⚠ Signtool not found, skipping signing
)

REM Rename output to NullInstallerEnhanced.exe
if exist dist\NullInstaller.exe (
    move /y dist\NullInstaller.exe dist\NullInstallerEnhanced.exe >nul 2>&1
)

del NullInstaller.csproj

echo ✓ Build successful!
echo Output: %cd%\dist\NullInstallerEnhanced.exe
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
