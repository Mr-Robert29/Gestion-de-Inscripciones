@echo off
setlocal
set "RAIZ=%~dp0"

where powershell.exe >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encontro Windows PowerShell.
    pause
    exit /b 1
)

if not "%~1"=="" goto argumentos

:menu
echo.
echo Cantidad de aspirantes ficticios para cargar:
echo   1) 50
echo   2) 100
echo   3) 200
echo   4) 300
echo   Q) Salir
choice /C 1234Q /N /M "Elegir una opcion: "
if errorlevel 5 exit /b 0
if errorlevel 4 (
    set "CANTIDAD=300"
    goto ejecutar
)
if errorlevel 3 (
    set "CANTIDAD=200"
    goto ejecutar
)
if errorlevel 2 (
    set "CANTIDAD=100"
    goto ejecutar
)
if errorlevel 1 (
    set "CANTIDAD=50"
    goto ejecutar
)

:ejecutar
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%RAIZ%scripts\cargar-solicitudes-simuladas.ps1" -Cantidad %CANTIDAD%
goto resultado

:argumentos
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%RAIZ%scripts\cargar-solicitudes-simuladas.ps1" %*

:resultado
set "CODIGO=%ERRORLEVEL%"
echo.
if not "%CODIGO%"=="0" echo El cargador termino con errores. Revisa el mensaje anterior.
pause
exit /b %CODIGO%
