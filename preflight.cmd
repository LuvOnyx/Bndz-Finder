@echo off
setlocal EnableExtensions
cd /d "%~dp0"
where pwsh >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell 7+ ^(pwsh^) required.
    exit /b 1
)
if exist "%~dp0BndzFinder\scripts\preflight.ps1" (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0BndzFinder\scripts\preflight.ps1"
) else (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\preflight.ps1"
)
exit /b %ERRORLEVEL%
