# Mock Installer Generator for CI Testing
# Generates small dummy executables that simulate real installers

param(
    [string]$OutputPath = ".",
    [switch]$GenerateAll
)

$mockInstallers = @(
    @{
        Name = "MockChrome.exe"
        ExitCode = 0
        Delay = 2
        Registry = "Google Chrome"
        Size = "1MB"
    },
    @{
        Name = "MockFirefox.exe"
        ExitCode = 0
        Delay = 3
        Registry = "Mozilla Firefox"
        Size = "2MB"
    },
    @{
        Name = "MockVSCode.exe"
        ExitCode = 0
        Delay = 5
        Registry = "Microsoft Visual Studio Code"
        Size = "3MB"
    },
    @{
        Name = "Mock7Zip.msi"
        ExitCode = 0
        Delay = 1
        Registry = "7-Zip"
        Size = "500KB"
    },
    @{
        Name = "MockFailingInstaller.exe"
        ExitCode = 1
        Delay = 1
        Registry = $null
        Size = "100KB"
    },
    @{
        Name = "MockTimeoutInstaller.exe"
        ExitCode = -1
        Delay = 150  # Exceeds 2-minute timeout
        Registry = $null
        Size = "200KB"
    },
    @{
        Name = "MockSilentInstaller.exe"
        ExitCode = 0
        Delay = 0
        Registry = "Silent App"
        Size = "1MB"
    },
    @{
        Name = "MockJetBrains.exe"
        ExitCode = 0
        Delay = 4
        Registry = "JetBrains Toolbox"
        Size = "5MB"
    }
)

function New-MockInstaller {
    param(
        [string]$Name,
        [int]$ExitCode,
        [int]$Delay,
        [string]$Registry,
        [string]$Size
    )
    
    $csharpCode = @"
using System;
using System.Threading;
using System.IO;
using Microsoft.Win32;

namespace MockInstaller
{
    class Program
    {
        static int Main(string[] args)
        {
            string installerName = "$Name";
            int exitCode = $ExitCode;
            int delaySeconds = $Delay;
            string registryEntry = $(if ($Registry) { "`"$Registry`"" } else { "null" });
            
            Console.WriteLine($"[MOCK] Starting installation of {installerName}");
            
            // Parse command line for silent flags
            bool isSilent = false;
            foreach (var arg in args)
            {
                if (arg.ToLower() == "/s" || arg.ToLower() == "/silent" || 
                    arg.ToLower() == "/quiet" || arg.ToLower() == "/qn")
                {
                    isSilent = true;
                    break;
                }
            }
            
            if (!isSilent)
            {
                Console.WriteLine("[MOCK] Running in interactive mode");
            }
            else
            {
                Console.WriteLine("[MOCK] Running in silent mode");
            }
            
            // Simulate installation work
            if (delaySeconds > 0)
            {
                Console.WriteLine($"[MOCK] Simulating installation... ({delaySeconds} seconds)");
                for (int i = 0; i < delaySeconds; i++)
                {
                    Thread.Sleep(1000);
                    Console.Write(".");
                }
                Console.WriteLine();
            }
            
            // Write to registry if successful
            if (exitCode == 0 && registryEntry != null)
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\MockInstallers"))
                    {
                        key.SetValue(registryEntry, DateTime.Now.ToString());
                        Console.WriteLine($"[MOCK] Registry entry created: {registryEntry}");
                    }
                    
                    // Also write to standard uninstall location
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
                        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{registryEntry}"))
                    {
                        key.SetValue("DisplayName", registryEntry);
                        key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                        key.SetValue("Publisher", "Mock Installer");
                        key.SetValue("UninstallString", $@"C:\Windows\System32\msiexec.exe /x {{MOCK-GUID}}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MOCK] Warning: Could not write to registry: {ex.Message}");
                }
            }
            
            // Create installation log
            string logPath = Path.Combine(Path.GetTempPath(), $"{installerName}.log");
            File.WriteAllText(logPath, $"Mock installation completed at {DateTime.Now}\nExit Code: {exitCode}");
            Console.WriteLine($"[MOCK] Log written to: {logPath}");
            
            if (exitCode == 0)
            {
                Console.WriteLine("[MOCK] Installation completed successfully!");
            }
            else
            {
                Console.WriteLine($"[MOCK] Installation failed with exit code: {exitCode}");
            }
            
            return exitCode;
        }
    }
}
"@
    
    $outputFile = Join-Path $OutputPath $Name
    
    # Compile the C# code to executable
    $tempCs = [System.IO.Path]::GetTempFileName() + ".cs"
    $csharpCode | Out-File -FilePath $tempCs -Encoding UTF8
    
    try {
        # Try to compile with csc
        $cscPath = (Get-ChildItem -Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\" -Recurse -Filter "csc.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
        if (-not $cscPath) {
            $cscPath = "${env:WINDIR}\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
        }
        
        if (Test-Path $cscPath) {
            & $cscPath /out:"$outputFile" /target:exe $tempCs 2>&1 | Out-Null
            Write-Host "✓ Generated: $Name" -ForegroundColor Green
        } else {
            Write-Warning "C# compiler not found. Creating stub file instead."
            # Create a simple batch file as fallback
            $batchContent = @"
@echo off
echo [MOCK] Starting installation of $Name
timeout /t $Delay /nobreak >nul 2>&1
exit /b $ExitCode
"@
            $batchContent | Out-File -FilePath $outputFile -Encoding ASCII
            Write-Host "Generated stub: $Name" -ForegroundColor Yellow
        }
    }
    finally {
        Remove-Item $tempCs -ErrorAction SilentlyContinue
    }
}

# Main execution
Write-Host "=== Mock Installer Generator ===" -ForegroundColor Cyan
Write-Host "Generating mock installers for testing..." -ForegroundColor White

if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

foreach ($mock in $mockInstallers) {
    New-MockInstaller @mock
}

Write-Host "`nMock installers generated successfully!" -ForegroundColor Green
Write-Host "Location: $OutputPath" -ForegroundColor Gray
