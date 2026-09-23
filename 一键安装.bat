@echo off
setlocal
cd /d "%~dp0"
title SpeakerGrillePro v27.4 - 3Dconnexion Safe Installer

net session >nul 2>&1
if errorlevel 1 (
  echo Requesting administrator privileges...
  powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0one_click_install.ps1"
set EC=%ERRORLEVEL%

echo.
if not "%EC%"=="0" (
  echo Installation failed. Error code: %EC%
  echo Please send the full contents of install_log.txt to ChatGPT.
) else (
  echo SpeakerGrillePro installation finished successfully. Start SOLIDWORKS normally after closing this window.
)
echo.
pause
exit /b %EC%
