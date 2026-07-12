@echo off
setlocal EnableExtensions
cd /d "%~dp0"
where pwsh >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell 7+ ^(pwsh^) not found.
    exit /b 1
)
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build.ps1" -Publish %*
exit /b %ERRORLEVEL%
