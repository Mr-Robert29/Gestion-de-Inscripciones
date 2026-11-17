param(
    [string]$ApiUrl = "http://localhost:5167",
    [string]$CorreoRoot = "admin@jardin.gob.ar",
    [string]$CorreoDirectivo = "fercortez75@gmail.com"
)

$ErrorActionPreference = "Stop"

function Convertir-SecureString {
    param([Security.SecureString]$Valor)
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Valor)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
}

function Invocar-Api {
    param(
        [Parameter(Mandatory)][string]$Ruta,
        [string]$Metodo = "GET",
        $Cuerpo = $null,
        [hashtable]$Cabeceras = $script:Cabeceras
    )

    $parametros = @{
        Uri = "$script:BaseApi$Ruta"
        Method = $Metodo
        Headers = $Cabeceras
        TimeoutSec = 40
        ErrorAction = "Stop"
    }
    if ($null -ne $Cuerpo) {
        $parametros.ContentType = "application/json; charset=utf-8"
        $parametros.Body = ConvertTo-Json -InputObject $Cuerpo -Depth 16 -Compress
    }

    try { return Invoke-RestMethod @parametros }
    catch {
        $respuesta = $_.Exception.Response
        if ($null -ne $respuesta) {
            try {
                $lector = [IO.StreamReader]::new($respuesta.GetResponseStream())
                try { $detalle = $lector.ReadToEnd() }
                finally { $lector.Dispose() }
                if (-not [string]::IsNullOrWhiteSpace($detalle)) {
                    throw "Error al llamar $Metodo $Ruta. $detalle"
                }
            }
            catch { if ($_.Exception.Message -like "Error al llamar*") { throw } }
        }
        throw "Error al llamar $Metodo $Ruta. $($_.Exception.Message)"
    }
}

try {
    $uri = [Uri]$ApiUrl
    if (-not $uri.IsAbsoluteUri -or $uri.Scheme -notin @("http", "https")) {
        throw "La URL de la API debe ser una dirección http o https completa."
    }
    if ($uri.Host -notin @("localhost", "127.0.0.1", "::1")) {
        throw "Por seguridad, este archivo solo trabaja con una API local (localhost)."
    }
    $script:BaseApi = $ApiUrl.TrimEnd("/") + "/api/v1"

    Write-Host "Preparación local de Jardín 945" -ForegroundColor Green
    Write-Host "API: $ApiUrl"
    Write-Host "Root: $CorreoRoot"
    Write-Host "Directivo: $CorreoDirectivo"
    Write-Host "Período: ciclo 2027 · 17/11/2026 al 26/11/2026 · corte 30/06/2027"
    Write-Host "Ofertas: Sala Roja/Mañana y Sala Verde/Tarde · 25 lugares cada una"
    Write-Host "La recepción quedará en preparación; no se abrirá automáticamente."

    $nombreDirectivo = Read-Host "Nombre y apellido del Directivo"
    if ([string]::IsNullOrWhiteSpace($nombreDirectivo)) { throw "El nombre del Directivo es obligatorio." }
    $dniDirectivo = Read-Host "DNI del Directivo (7 u 8 números)"
    if ($dniDirectivo -notmatch '^\d{7,8}$') { throw "El DNI debe tener 7 u 8 números, sin puntos ni espacios." }

    $claveRootSegura = Read-Host "Contraseña de la cuenta Root" -AsSecureString
    $claveRoot = Convertir-SecureString $claveRootSegura
    $claveRootSegura.Dispose()
    $login = Invoke-RestMethod -Uri "$script:BaseApi/Autenticacion/iniciar-sesion" -Method Post `
        -ContentType "application/json; charset=utf-8" `
        -Body (ConvertTo-Json -Compress @{ correoElectronico = $CorreoRoot; clave = $claveRoot }) `
        -TimeoutSec 40 -ErrorAction Stop
    $claveRoot = $null

    if ([string]::IsNullOrWhiteSpace([string]$login.token)) {
        throw "La API no devolvió un token de sesión para Root."
    }
    if ($login.debeCambiarClave -eq $true) {
        throw "La cuenta Root requiere cambiar la contraseña. Iniciá sesión en la aplicación, actualizala y volvé a ejecutar el archivo."
    }
    $script:Cabeceras = @{ Authorization = "Bearer $($login.token)" }

    # Evita crear una institución parcial si la cuenta o el código ya existen.
    $instituciones = @(Invocar-Api -Ruta "/instituciones")
    if ($instituciones | Where-Object { $_.codigo -eq "JI-945" }) {
        throw "Ya existe una institución con código JI-945. No se creó nada; revisá la institución existente o usá otro código."
    }
    $usuarios = @(Invocar-Api -Ruta "/Usuarios")
    if ($usuarios | Where-Object { $_.correoElectronico -ieq $CorreoDirectivo }) {
        throw "Ya existe una cuenta con correo $CorreoDirectivo. No se creó nada. Revisá esa cuenta antes de volver a ejecutar el archivo."
    }

    Write-Host "`nSe solicitarán las contraseñas de forma oculta y no se guardarán en el archivo." -ForegroundColor Yellow
    $confirmacion = Read-Host "Escribí CREAR JARDIN 945 para confirmar la creación"
    if ($confirmacion -cne "CREAR JARDIN 945") {
        Write-Host "Cancelado; no se creó ningún registro." -ForegroundColor Yellow
        exit 0
    }
    $claveDirectivoSegura = Read-Host "Contraseña inicial del Directivo" -AsSecureString
    $claveDirectivo = Convertir-SecureString $claveDirectivoSegura
    $claveDirectivoSegura.Dispose()

    Write-Host "`nCreando institución..." -ForegroundColor Cyan
    $institucion = Invocar-Api -Ruta "/instituciones" -Metodo "POST" -Cuerpo @{
        nombre = "Jardin 945"
        codigo = "JI-945"
        telefonoContacto = "01122983451"
        correoElectronicoContacto = "wilson@jardincoloresnuevos.gob.ar"
        domicilio = @{
            calle = "123"
            numero = "5123"
            localidad = "Florencio Varela"
            provincia = "Buenos Aires"
            codigoPostal = "1888"
            complemento = $null
        }
    }
    $institucionId = [string]$institucion.id
    if ([string]::IsNullOrWhiteSpace($institucionId)) { throw "La API no devolvió el ID de la institución creada." }

    Write-Host "Creando cuenta Directivo..." -ForegroundColor Cyan
    $null = Invocar-Api -Ruta "/Usuarios/personal-institucion" -Metodo "POST" -Cuerpo @{
        institucionId = $institucionId
        rol = "Directivo"
        nombre = $nombreDirectivo.Trim()
        correoElectronico = $CorreoDirectivo
        claveInicial = $claveDirectivo
        dni = $dniDirectivo
    }
    $claveDirectivo = $null

    Write-Host "Creando período 2027..." -ForegroundColor Cyan
    $periodo = Invocar-Api -Ruta "/instituciones/$institucionId/periodos" -Metodo "POST" -Cuerpo @{
        ciclo = 2027
        inicio = "2026-11-17"
        fin = "2026-11-26"
    }
    $periodoId = [string]$periodo.id
    if ([string]::IsNullOrWhiteSpace($periodoId)) { throw "La API no devolvió el ID del período." }

    Write-Host "Creando aulas y turnos..." -ForegroundColor Cyan
    $aulaRoja = Invocar-Api -Ruta "/instituciones/$institucionId/aulas" -Metodo "POST" -Cuerpo @{ nombre = "Sala Roja" }
    $aulaVerde = Invocar-Api -Ruta "/instituciones/$institucionId/aulas" -Metodo "POST" -Cuerpo @{ nombre = "Sala Verde" }
    $turnoManana = Invocar-Api -Ruta "/instituciones/$institucionId/turnos" -Metodo "POST" -Cuerpo @{ nombre = "Mañana" }
    $turnoTarde = Invocar-Api -Ruta "/instituciones/$institucionId/turnos" -Metodo "POST" -Cuerpo @{ nombre = "Tarde" }

    $motivo = "Configuración inicial de prueba del ciclo 2027."
    Write-Host "Creando ofertas reutilizables de la institución..." -ForegroundColor Cyan
    $null = Invocar-Api -Ruta "/instituciones/$institucionId/ofertas-reutilizables" -Metodo "POST" -Cuerpo @{
        aulaId = $aulaRoja.id
        turnoId = $turnoManana.id
        capacidad = 25
        referenciaCapacidad = "Capacidad de referencia: 25 lugares"
        motivo = $motivo
    }
    $null = Invocar-Api -Ruta "/instituciones/$institucionId/ofertas-reutilizables" -Metodo "POST" -Cuerpo @{
        aulaId = $aulaVerde.id
        turnoId = $turnoTarde.id
        capacidad = 25
        referenciaCapacidad = "Capacidad de referencia: 25 lugares"
        motivo = $motivo
    }

    Write-Host "Agregando las dos ofertas al período..." -ForegroundColor Cyan
    $null = Invocar-Api -Ruta "/periodos/$periodoId/ofertas" -Metodo "POST" -Cuerpo @{
        aulaId = $aulaRoja.id
        turnoId = $turnoManana.id
        capacidad = 25
        disponible = $true
        referenciaCapacidad = "Capacidad de referencia: 25 lugares"
        origenOcupaciones = "Sin ocupaciones iniciales"
        ocupaciones = @()
        motivo = $motivo
    }
    $null = Invocar-Api -Ruta "/periodos/$periodoId/ofertas" -Metodo "POST" -Cuerpo @{
        aulaId = $aulaVerde.id
        turnoId = $turnoTarde.id
        capacidad = 25
        disponible = $true
        referenciaCapacidad = "Capacidad de referencia: 25 lugares"
        origenOcupaciones = "Sin ocupaciones iniciales"
        ocupaciones = @()
        motivo = $motivo
    }

    Write-Host "Preparando reglas y oferta del ciclo con corte 30/06/2027..." -ForegroundColor Cyan
    $referencia = "Configuración inicial del ciclo 2027"
    $versionReglas = Invocar-Api -Ruta "/periodos/$periodoId/versiones-reglas" -Metodo "POST" -Cuerpo @{
        fechaCorte = "2027-06-30"
        referencia = $referencia
        motivo = $motivo
    }
    $versionOferta = Invocar-Api -Ruta "/periodos/$periodoId/versiones-oferta" -Metodo "POST" -Cuerpo @{
        referencia = $referencia
        motivo = $motivo
    }
    $periodoActual = Invocar-Api -Ruta "/periodos/$periodoId"
    $null = Invocar-Api -Ruta "/periodos/$periodoId/configuracion" -Metodo "PUT" -Cuerpo @{
        versiones = @{
            versionReglasId = $versionReglas.id
            versionOfertaId = $versionOferta.id
        }
        revision = [int]$periodoActual.revision
        motivo = $motivo
    }

    Write-Host "`nListo. Se crearon la institución, la cuenta Directivo, el período y las dos ofertas." -ForegroundColor Green
    Write-Host "Institución: Jardín 945 (JI-945) · ciclo 2027 · fecha de corte 30/06/2027"
    Write-Host "Sala Roja · Mañana · 25 lugares"
    Write-Host "Sala Verde · Tarde · 25 lugares"
    Write-Host "La recepción sigue cerrada/en preparación. La cuenta Directivo deberá cambiar su contraseña al ingresar por primera vez."
    Write-Host "IDs: institución $institucionId · período $periodoId"
}
catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
