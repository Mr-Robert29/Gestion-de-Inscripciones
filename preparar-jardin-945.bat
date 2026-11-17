@echo off
setlocal
set "RAIZ=%~dp0"

where powershell.exe >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro Windows PowerShell.
    pause
    exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%RAIZ%scripts\preparar-jardin-945.ps1" %*
set "CODIGO=%ERRORLEVEL%"

echo.
if not "%CODIGO%"=="0" echo La preparacion termino con errores. Revisa el mensaje anterior.
pause
exit /b %CODIGO%
