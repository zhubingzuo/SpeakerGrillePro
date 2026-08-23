@echo off
setlocal
cd /d "%~dp0"
net session >nul 2>&1
if errorlevel 1 (
  echo ERROR: Please run as administrator.
  pause
  exit /b 1
)
set "REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
if exist "bin\SpeakerGrillePro.dll" "%REGASM%" "bin\SpeakerGrillePro.dll" /unregister
reg delete "HKLM\SOFTWARE\SolidWorks\Addins\{7A88B123-7C5D-4B8C-9E2B-7E7314B42650}" /f >nul 2>&1
reg delete "HKCU\Software\SolidWorks\AddInsStartup\{7A88B123-7C5D-4B8C-9E2B-7E7314B42650}" /f >nul 2>&1
echo Uninstalled.
pause
