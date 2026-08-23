@echo off
setlocal
cd /d "%~dp0"
echo Starting PowerShell build helper...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
set "RC=%ERRORLEVEL%"
echo.
if not "%RC%"=="0" (
  echo Build helper exited with code %RC%.
) else (
  echo Build helper finished successfully.
)
pause
exit /b %RC%
