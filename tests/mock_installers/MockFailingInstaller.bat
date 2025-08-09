@echo off
echo [MOCK] Starting installation of MockFailingInstaller.bat
echo [MOCK] Simulating installation... (1 seconds)
timeout /t 1 /nobreak >nul 2>&1
echo [MOCK] Installation failed with exit code: 1
exit /b 1

