@echo off
setlocal EnableExtensions
cd /d "%~dp0"
where pwsh >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell 7+ ^(pwsh^) not found. Install from https://aka.ms/powershell
    exit /b 1
)
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
