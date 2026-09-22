@echo off
setlocal
cd /d "%~dp0"
net session >nul 2>&1
if errorlevel 1 (
  echo ERROR: Please right-click this file and choose "Run as administrator".
  pause
  exit /b 1
)
if not exist "bin\SpeakerGrillePro.dll" (
  echo ERROR: bin\SpeakerGrillePro.dll not found. Run build.bat first.
  pause
  exit /b 2
)
set "REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
"%REGASM%" "bin\SpeakerGrillePro.dll" /codebase /tlb:"bin\SpeakerGrillePro.tlb"
if errorlevel 1 (
  echo Registration failed.
  pause
  exit /b 3
)
echo.
echo Installed successfully.
echo Start SOLIDWORKS 2025 ^> Tools ^> Add-Ins and enable "喇叭孔生成器" if it is not already enabled.
pause
