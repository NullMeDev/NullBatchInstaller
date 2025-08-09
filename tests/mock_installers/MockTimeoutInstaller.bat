@echo off
echo [MOCK] Starting installation of MockTimeoutInstaller.bat
echo [MOCK] Simulating installation... (150 seconds)
timeout /t 150 /nobreak >nul 2>&1
echo [MOCK] Installation completed successfully!
exit /b 0

