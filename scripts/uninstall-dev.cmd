@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall-dev.ps1" %*
exit /b %ERRORLEVEL%
