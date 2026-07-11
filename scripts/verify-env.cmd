@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-env.ps1" %*
exit /b %ERRORLEVEL%
