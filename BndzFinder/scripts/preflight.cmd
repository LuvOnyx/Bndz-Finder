@echo off
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0preflight.ps1" %*
