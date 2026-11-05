@echo off
setlocal

rem Funciona aunque se abra desde otra carpeta o la ruta del repositorio cambie.
set "RAIZ=%~dp0"

if not exist "%RAIZ%back\pruebas\Jardin.Configuracion.Pruebas\Jardin.Configuracion.Pruebas.csproj" (
    echo ERROR: Coloca este archivo junto a la carpeta back del repositorio.
    goto :error
)
where dotnet >nul 2>&1
if errorlevel 1 goto :sinDotnet
dotnet --list-sdks | findstr /B /C:"10." >nul
if errorlevel 1 goto :sinDotnet

if /I "%~1"=="--comprobar" (
    echo Correcto: ruta del proyecto y SDK .NET 10 disponibles. No se modifico la base.
    exit /b 0
)

echo Se aplicaran las migraciones pendientes de los cinco modulos.
echo PostgreSQL debe estar encendido y la base debe existir.
echo La conexion se obtiene de los secretos de usuario de Jardin.Api.
echo Detene el backend antes de ejecutar este archivo para evitar bloqueos de compilacion.
echo.

pushd "%RAIZ%"
if errorlevel 1 (
    echo ERROR: No se pudo acceder a la carpeta del proyecto.
    goto :error
)
dotnet run --project "back\pruebas\Jardin.Configuracion.Pruebas" -- --actualizar-base
set "RESULTADO=%errorlevel%"
popd

if "%RESULTADO%"=="0" (
    echo.
    echo Base actualizada. Las migraciones ya aplicadas no se repiten.
) else (
    echo.
    echo No se completo la actualizacion. Revisa el mensaje anterior.
)
pause
exit /b %RESULTADO%

:sinDotnet
echo ERROR: Se necesita el SDK de .NET 10.
:error
if /I not "%~1"=="--comprobar" pause
exit /b 1
