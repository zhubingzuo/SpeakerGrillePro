@echo off
setlocal
cd /d "%~dp0"

echo Running the geometric pattern regression harness...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify_patterns.ps1"
set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
  echo Pattern verification FAILED. Exit code: %RC%
) else (
  echo Pattern verification passed.
)
pause
exit /b %RC%
