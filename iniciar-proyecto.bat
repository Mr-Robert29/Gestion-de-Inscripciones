@echo off
setlocal

set "RAIZ=%~dp0"
set "PYTHON=python"

if not exist "%RAIZ%back\src\Jardin.Api\Jardin.Api.csproj" goto :sinCarpetas
if not exist "%RAIZ%front\index.html" goto :sinCarpetas
where dotnet >nul 2>&1
if errorlevel 1 goto :sinDotnet
dotnet --list-sdks | findstr /B /C:"10." >nul
if errorlevel 1 goto :sinDotnet
python --version >nul 2>&1
if errorlevel 1 (
    py -3 --version >nul 2>&1
    if errorlevel 1 goto :sinPython
    set "PYTHON=py -3"
)

if /I "%~1"=="--comprobar" (
    echo Correcto: carpetas relativas, SDK .NET 10 y Python disponibles.
    exit /b 0
)

echo Iniciando backend y frontend de Jardin...
echo PostgreSQL debe estar encendido y los secretos de usuario configurados.

rem Evita iniciar una segunda instancia si el puerto ya esta ocupado.
call :puertoEnUso 5167
if errorlevel 1 (
    start "Jardin Backend" /D "%RAIZ%back" cmd /k "dotnet run --project src\Jardin.Api --launch-profile http"
) else (
    echo El puerto 5167 ya esta ocupado. No se inicia otro backend.
)

call :puertoEnUso 4173
if errorlevel 1 (
    start "Jardin Frontend" /D "%RAIZ%front" cmd /k "%PYTHON% -m http.server 4173 --bind 127.0.0.1"
) else (
    echo El puerto 4173 ya esta ocupado. No se inicia otro frontend.
)

echo Esperando los servidores, hasta 60 segundos...
rem La API responde 401 sin sesion: esa respuesta tambien confirma disponibilidad.
powershell -NoProfile -Command "$limite = (Get-Date).AddSeconds(60); $api = $false; $front = $false; do { if (-not $api) { try { $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5167/api/v1/Autenticacion/mi-acceso' -TimeoutSec 2; $api = ($r.StatusCode -eq 200) } catch { $api = ($null -ne $_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 401) } }; if (-not $front) { try { $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:4173/' -TimeoutSec 2; $front = ($r.StatusCode -eq 200) } catch { $front = $false } }; if ($api -and $front) { exit 0 }; Start-Sleep -Seconds 1 } while ((Get-Date) -lt $limite); exit 1"
if errorlevel 1 (
    echo No respondieron ambos servidores. Revisa sus ventanas, PostgreSQL y los secretos.
    goto :error
)
start "" "http://127.0.0.1:4173"
echo Listo. Abri http://127.0.0.1:4173
echo Para detenerlos, presiona Ctrl+C en cada ventana de servidor.
exit /b 0

:puertoEnUso
powershell -NoProfile -Command "if (Get-NetTCPConnection -State Listen -LocalPort %~1 -ErrorAction SilentlyContinue) { exit 0 }; exit 1"
exit /b %errorlevel%

:sinCarpetas
echo ERROR: Coloca este archivo junto a las carpetas back y front.
goto :error
:sinDotnet
echo ERROR: Instala el SDK de .NET 10.
goto :error
:sinPython
echo ERROR: Instala Python 3.
:error
if /I not "%~1"=="--comprobar" pause
exit /b 1
