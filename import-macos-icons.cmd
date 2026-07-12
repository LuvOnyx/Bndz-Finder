@echo off
setlocal
cd /d "%~dp0"
if "%MACOSICONS_API_KEY%"=="" (
  echo Set MACOSICONS_API_KEY first. Free key: https://docs.macosicons.com/api-management
  exit /b 1
)
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0BndzFinder\scripts\import-macos-icons.ps1"
exit /b %ERRORLEVEL%
