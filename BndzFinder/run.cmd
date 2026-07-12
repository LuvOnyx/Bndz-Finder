@echo off
setlocal EnableExtensions
cd /d "%~dp0"
if exist "%~dp0..\run.cmd" (
    call "%~dp0..\run.cmd" %*
) else (
    where pwsh >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo ERROR: PowerShell 7+ ^(pwsh^) not found.
        exit /b 1
    )
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\run-inner.ps1" %*
)
exit /b %ERRORLEVEL%
