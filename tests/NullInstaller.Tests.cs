using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace NullInstaller.Tests
{
    [TestClass]
    public class InstallerUnitTests
    {
        private string testDirectory;
        private string mockInstallersPath;
        
        [TestInitialize]
        public void Setup()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), "NullInstallerTests_" + Guid.NewGuid().ToString("N"));
            mockInstallersPath = Path.Combine(testDirectory, "mocks");
            Directory.CreateDirectory(mockInstallersPath);
            
            // Generate mock installers
            GenerateMockInstallers();
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
                
                // Clean up registry entries
                CleanupMockRegistry();
            }
            catch { }
        }
        
        private void GenerateMockInstallers()
        {
            // Create simple batch file mocks for testing
            var mocks = new Dictionary<string, string>
            {
                ["MockSuccess.exe"] = @"@echo off
echo [MOCK] Installing...
timeout /t 1 /nobreak >nul 2>&1
echo [MOCK] Installation complete
exit /b 0",
                
                ["MockFailure.exe"] = @"@echo off
echo [MOCK] Installation failed
exit /b 1",
                
                ["MockTimeout.exe"] = @"@echo off
echo [MOCK] Starting long installation...
timeout /t 150 /nobreak >nul 2>&1
exit /b 0",
                
                ["MockSilent.exe"] = @"@echo off
if ""%1""==""/S"" goto silent
echo [MOCK] Interactive mode
goto end
:silent
echo [MOCK] Silent mode
:end
exit /b 0"
            };
            
            foreach (var mock in mocks)
            {
                File.WriteAllText(Path.Combine(mockInstallersPath, mock.Key), mock.Value);
            }
        }
        
        private void CleanupMockRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE", true))
                {
                    key?.DeleteSubKeyTree("MockInstallers", false);
                }
            }
            catch { }
        }
        
        [TestMethod]
        public void Test_SilentFlagDetection()
        {
            var testCases = new Dictionary<string, string[]>
            {
                ["installer.exe"] = new[] { "/S", "/SILENT", "/QUIET", "/q", "/qn" },
                ["installer.msi"] = new[] { "/qn", "/quiet" }
            };
            
            foreach (var testCase in testCases)
            {
                foreach (var flag in testCase.Value)
                {
                    var result = GetSilentInstallCommand(testCase.Key, flag);
                    Assert.IsTrue(result.Contains(flag), $"Silent flag {flag} not detected for {testCase.Key}");
                }
            }
        }
        
        [TestMethod]
        public void Test_InstallationSuccess()
        {
            var installer = Path.Combine(mockInstallersPath, "MockSuccess.exe");
            var result = RunInstaller(installer, "/S", 10);
            
            Assert.AreEqual(0, result.ExitCode, "Installation should succeed");
            Assert.IsTrue(result.Success, "Success flag should be true");
            Assert.IsFalse(result.TimedOut, "Should not timeout");
        }
        
        [TestMethod]
        public void Test_InstallationFailure()
        {
            var installer = Path.Combine(mockInstallersPath, "MockFailure.exe");
            var result = RunInstaller(installer, "/S", 10);
            
            Assert.AreEqual(1, result.ExitCode, "Installation should fail with exit code 1");
            Assert.IsFalse(result.Success, "Success flag should be false");
        }
        
        [TestMethod]
        public void Test_InstallationTimeout()
        {
            var installer = Path.Combine(mockInstallersPath, "MockTimeout.exe");
            var result = RunInstaller(installer, "/S", 2); // 2 second timeout
            
            Assert.IsTrue(result.TimedOut, "Installation should timeout");
            Assert.IsFalse(result.Success, "Success flag should be false on timeout");
        }
        
        [TestMethod]
        public void Test_ConcurrentInstallations()
        {
            var installers = new[]
            {
                Path.Combine(mockInstallersPath, "MockSuccess.exe"),
                Path.Combine(mockInstallersPath, "MockSilent.exe")
            };
            
            var tasks = installers.Select(installer => 
                Task.Run(() => RunInstaller(installer, "/S", 10))
            ).ToArray();
            
            Task.WaitAll(tasks);
            
            foreach (var task in tasks)
            {
                Assert.IsTrue(task.Result.Success, "All concurrent installations should succeed");
            }
        }
        
        [TestMethod]
        public void Test_RetryLogic()
        {
            var installer = Path.Combine(mockInstallersPath, "MockFailure.exe");
            var attempts = 0;
            var maxRetries = 3;
            InstallResult result = null;
            
            while (attempts < maxRetries)
            {
                result = RunInstaller(installer, "/S", 10);
                attempts++;
                
                if (result.Success)
                    break;
                    
                Thread.Sleep(1000); // Wait before retry
            }
            
            Assert.AreEqual(maxRetries, attempts, "Should attempt maximum retries");
            Assert.IsFalse(result.Success, "Installation should still fail after retries");
        }
        
        [TestMethod]
        public void Test_LogFileCreation()
        {
            var logPath = Path.Combine(testDirectory, "test_install.log");
            var installer = Path.Combine(mockInstallersPath, "MockSuccess.exe");
            
            var result = RunInstallerWithLogging(installer, "/S", logPath);
            
            Assert.IsTrue(File.Exists(logPath), "Log file should be created");
            
            var logContent = File.ReadAllText(logPath);
            Assert.IsTrue(logContent.Length > 0, "Log file should contain content");
            Assert.IsTrue(logContent.Contains("Installation"), "Log should contain installation info");
        }
        
        [TestMethod]
        public void Test_RegistryVerification()
        {
            // This test would require admin privileges in real scenario
            var testKey = @"SOFTWARE\MockInstallers\TestApp";
            
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(testKey))
                {
                    key?.SetValue("InstallDate", DateTime.Now.ToString());
                }
                
                var isInstalled = VerifyInstallation("TestApp");
                Assert.IsTrue(isInstalled, "Should detect installed application in registry");
            }
            finally
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MockInstallers", true))
                {
                    key?.DeleteSubKeyTree("TestApp", false);
                }
            }
        }
        
        [TestMethod]
        public void Test_PostInstallHook()
        {
            var hookScript = Path.Combine(testDirectory, "post_install.ps1");
            var flagFile = Path.Combine(testDirectory, "hook_executed.flag");
            
            File.WriteAllText(hookScript, $@"
                New-Item -Path '{flagFile}' -ItemType File -Force
                Add-Content -Path '{flagFile}' -Value 'Hook executed at $(Get-Date)'
            ");
            
            ExecutePostInstallHook(hookScript);
            
            Assert.IsTrue(File.Exists(flagFile), "Post-install hook should create flag file");
        }
        
        // Helper methods
        private string GetSilentInstallCommand(string installer, string flag)
        {
            if (installer.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            {
                return $"msiexec /i \"{installer}\" {flag} /norestart";
            }
            return $"\"{installer}\" {flag}";
        }
        
        private InstallResult RunInstaller(string path, string args, int timeoutSeconds)
        {
            var result = new InstallResult();
            
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    process.Start();
                    
                    var completed = process.WaitForExit(timeoutSeconds * 1000);
                    
                    if (completed)
                    {
                        result.ExitCode = process.ExitCode;
                        result.Success = (process.ExitCode == 0);
                        result.Output = process.StandardOutput.ReadToEnd();
                        result.Error = process.StandardError.ReadToEnd();
                    }
                    else
                    {
                        process.Kill();
                        result.TimedOut = true;
                        result.Success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }
            
            return result;
        }
        
        private InstallResult RunInstallerWithLogging(string path, string args, string logPath)
        {
            var result = RunInstaller(path, args, 10);
            
            var logContent = $@"
Installation Log
================
Time: {DateTime.Now}
Installer: {path}
Arguments: {args}
Exit Code: {result.ExitCode}
Success: {result.Success}
Timed Out: {result.TimedOut}
Output: {result.Output}
Error: {result.Error}
";
            
            File.WriteAllText(logPath, logContent);
            
            return result;
        }
        
        private bool VerifyInstallation(string appName)
        {
            var paths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\MockInstallers"
            };
            
            foreach (var path in paths)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (var subKey = key.OpenSubKey(subKeyName))
                                {
                                    var displayName = subKey?.GetValue("DisplayName") as string;
                                    if (displayName?.Contains(appName) == true)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            
            return false;
        }
        
        private void ExecutePostInstallHook(string scriptPath)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                process.Start();
                process.WaitForExit(5000);
            }
        }
        
        private class InstallResult
        {
            public bool Success { get; set; }
            public int ExitCode { get; set; }
            public bool TimedOut { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }
    }
}
