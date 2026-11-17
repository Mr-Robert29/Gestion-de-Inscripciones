param(
    [string]$ApiUrl = "http://localhost:5167",
    [ValidateRange(1, 500)][int]$Cantidad = 50
)

$ErrorActionPreference = "Stop"

function Get-Propiedad {
    param($Objeto, [string]$Nombre, $Predeterminado = $null)
    if ($null -eq $Objeto) { return $Predeterminado }
    if ($Objeto -is [System.Collections.IDictionary]) {
        foreach ($clave in $Objeto.Keys) {
            if ([string]$clave -ieq $Nombre) { return $Objeto[$clave] }
        }
    }
    $propiedad = $Objeto.PSObject.Properties[$Nombre]
    if ($null -eq $propiedad) { return $Predeterminado }
    return $propiedad.Value
}

function Get-Campo {
    param($Objeto, [string[]]$Nombres, $Predeterminado = $null)
    foreach ($nombre in $Nombres) {
        $valor = Get-Propiedad $Objeto $nombre
        if ($null -ne $valor -and -not [string]::IsNullOrWhiteSpace([string]$valor)) { return $valor }
    }
    return $Predeterminado
}

function Get-Coleccion {
    param($Respuesta, [string[]]$Contenedores)
    if ($null -eq $Respuesta) { return @() }
    if ($Respuesta -is [array] -or $Respuesta -is [System.Collections.IList]) { return @($Respuesta) }
    foreach ($contenedor in $Contenedores) {
        $valor = Get-Propiedad $Respuesta $contenedor
        if ($null -ne $valor) {
            if ($valor -is [array] -or $valor -is [System.Collections.IList]) { return @($valor) }
            return @($valor)
        }
    }
    return @($Respuesta)
}

function Invoke-JardinApi {
    param([string]$Ruta, [string]$Metodo = "GET", $Cuerpo = $null)
    $parametros = @{
        Uri = "$script:ApiBase$Ruta"
        Method = $Metodo
        Headers = $script:Headers
        TimeoutSec = 40
        ErrorAction = "Stop"
    }
    if ($null -ne $Cuerpo) {
        $parametros.ContentType = "application/json; charset=utf-8"
        $parametros.Body = ConvertTo-Json -InputObject $Cuerpo -Depth 12 -Compress
    }
    return Invoke-RestMethod @parametros
}

function Exportar-Avance {
    if ($script:Resultados.Count -eq 0) { return }
    $script:Resultados | Export-Csv -LiteralPath $script:ArchivoSalida -NoTypeInformation -Encoding UTF8
}

function Obtener-Edad {
    param([datetime]$Nacimiento, [datetime]$Corte)
    $edad = $Corte.Year - $Nacimiento.Year
    if ($Corte.Month -lt $Nacimiento.Month -or
        ($Corte.Month -eq $Nacimiento.Month -and $Corte.Day -lt $Nacimiento.Day)) { $edad-- }
    return $edad
}

function Nuevo-NumeroUnico {
    param([int]$Minimo, [int]$Maximo, [System.Collections.Generic.HashSet[string]]$Usados)
    for ($intento = 0; $intento -lt 1000; $intento++) {
        $numero = [string](Get-Random -Minimum $Minimo -Maximum $Maximo)
        if ($Usados.Add($numero)) { return $numero }
    }
    throw "No pude generar un número de prueba único después de varios intentos."
}

function Elegir-Elemento {
    param([string]$Titulo, [array]$Elementos, [scriptblock]$Etiqueta)
    if ($Elementos.Count -eq 0) { throw "No hay opciones disponibles para: $Titulo." }
    Write-Host "`n$Titulo" -ForegroundColor Cyan
    for ($i = 0; $i -lt $Elementos.Count; $i++) {
        Write-Host ("  {0}) {1}" -f ($i + 1), (& $Etiqueta $Elementos[$i]))
    }
    while ($true) {
        $entrada = Read-Host "Elegí un número"
        $numero = 0
        if ([int]::TryParse($entrada, [ref]$numero) -and $numero -ge 1 -and $numero -le $Elementos.Count) {
            return $Elementos[$numero - 1]
        }
        Write-Host "Ingresá un número de la lista." -ForegroundColor Yellow
    }
}

try {
    $uriApi = [Uri]$ApiUrl
    if (-not $uriApi.IsAbsoluteUri -or $uriApi.Scheme -notin @("http", "https")) {
        throw "La dirección de la API debe ser una URL http o https completa."
    }
    if ($uriApi.Host -notin @("localhost", "127.0.0.1", "::1")) {
        throw "Por seguridad, este cargador solo admite una API local (localhost). No lo uses contra producción."
    }
    $script:ApiBase = $ApiUrl.TrimEnd('/') + "/api/v1"

    Write-Host "Cargador de solicitudes ficticias para pruebas locales" -ForegroundColor Green
    Write-Host "Se crearán hasta $Cantidad aspirantes, solicitudes, responsables y distancias manuales." 
    Write-Host "Las distancias serán ficticias/provisorias y podrán influir en prioridades y reportes."

    $correo = Read-Host "Correo de una cuenta con acceso a la institución"
    $claveSegura = Read-Host "Contraseña" -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($claveSegura)
    try { $clave = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }

    $login = Invoke-RestMethod -Uri "$script:ApiBase/Autenticacion/iniciar-sesion" -Method Post `
        -ContentType "application/json; charset=utf-8" `
        -Body (ConvertTo-Json -Compress @{ correoElectronico = $correo; clave = $clave }) `
        -TimeoutSec 40 -ErrorAction Stop
    $clave = $null
    $token = Get-Propiedad $login "token"
    if ([string]::IsNullOrWhiteSpace([string]$token)) { throw "La API no devolvió un token de sesión." }
    if ((Get-Propiedad $login "debeCambiarClave" $false) -eq $true) {
        throw "Esta cuenta debe cambiar la contraseña antes de usar la API. Iniciá sesión en la aplicación, cambiá la clave y volvé a ejecutar el cargador."
    }
    $script:Headers = @{ Authorization = "Bearer $token" }

    $institucionesRespuesta = Invoke-JardinApi "/instituciones"
    $instituciones = @(Get-Coleccion $institucionesRespuesta @("items", "value", "values", "data", "instituciones"))
    if ($instituciones.Count -eq 0) { throw "La API no devolvió instituciones para esta cuenta. Verificá que tenga acceso a un jardín." }
    $institucion = Elegir-Elemento "Institución" $instituciones { param($x) "$(Get-Campo $x @('nombre', 'nombreInstitucion', 'name') 'Nombre no disponible') · $(Get-Campo $x @('id', 'institucionId', 'idInstitucion') 'ID no disponible')" }
    $institucionId = [string](Get-Campo $institucion @("id", "institucionId", "idInstitucion"))
    if ([string]::IsNullOrWhiteSpace($institucionId)) {
        $campos = ($institucion.PSObject.Properties.Name -join ", ")
        throw "No pude leer el identificador de la institución que devolvió la API. Campos recibidos: $campos. Confirmá que el backend esté actualizado y reinicialo si ya estaba levantado."
    }
    $periodosRespuesta = Invoke-JardinApi "/instituciones/$institucionId/periodos"
    $periodos = @(Get-Coleccion $periodosRespuesta @("items", "value", "values", "data", "periodos"))
    if ($periodos.Count -eq 0) { throw "La institución elegida no tiene períodos de inscripción." }
    $periodo = Elegir-Elemento "Período de inscripción" $periodos { param($x) "Ciclo $(Get-Campo $x @('ciclo', 'cicloLectivo', 'anioLectivo') 'no disponible') · $(Get-Campo $x @('estado', 'estadoRecepcion') 'estado no disponible') · $(Get-Campo $x @('id', 'periodoId', 'idPeriodo') 'ID no disponible')" }
    $periodoId = [string](Get-Campo $periodo @("id", "periodoId", "idPeriodo"))
    $cicloValor = Get-Campo $periodo @("ciclo", "cicloLectivo", "anioLectivo")
    if ([string]::IsNullOrWhiteSpace($periodoId) -or $null -eq $cicloValor) {
        $campos = ($periodo.PSObject.Properties.Name -join ", ")
        throw "No pude leer ciclo e identificador del período. Campos recibidos: $campos. Confirmá que el backend esté actualizado y reinicialo si ya estaba levantado."
    }
    $ciclo = [int]$cicloValor
    $estado = [string](Get-Campo $periodo @("estado", "estadoRecepcion"))
    $idReglas = [string](Get-Propiedad $periodo "versionReglasId")
    $idOferta = [string](Get-Propiedad $periodo "versionOfertaId")
    if ([string]::IsNullOrWhiteSpace($idReglas) -or [string]::IsNullOrWhiteSpace($idOferta)) {
        throw "El período no tiene reglas y oferta publicadas. Prepará su configuración antes de ejecutar el cargador."
    }
    if ($estado -notin @("RecepcionAbierta", "RecepcionCerrada")) {
        throw "El período está en estado '$estado' y todavía no admite solicitudes. No se creó ningún registro."
    }

    $versionReglas = Invoke-JardinApi "/versiones-configuracion/$idReglas"
    $versionOferta = Invoke-JardinApi "/versiones-configuracion/$idOferta"
    $datosReglas = Get-Propiedad $versionReglas "datos"
    $datosOferta = @(Get-Propiedad $versionOferta "datos")
    $corteTexto = [string](Get-Propiedad $datosReglas "fechaCorte")
    $corte = [datetime]::Parse($corteTexto, [Globalization.CultureInfo]::InvariantCulture).Date
    $edades = @(
        foreach ($combinacion in @(Get-Propiedad $datosReglas "combinacionesEdades")) {
            foreach ($edad in @($combinacion)) { [int]$edad }
        }
    ) | Sort-Object -Unique
    if ($edades.Count -eq 0) { throw "Las reglas publicadas no indican edades admitidas." }

    $ofertasActivas = @($datosOferta | Where-Object {
        (Get-Propiedad $_ "disponible" $false) -eq $true -and [int](Get-Propiedad $_ "capacidad" 0) -gt 0
    })
    $turnos = @($ofertasActivas | ForEach-Object { [string](Get-Propiedad $_ "turnoId") } | Sort-Object -Unique)
    if ($turnos.Count -eq 0) { throw "La oferta publicada no tiene turnos habilitados con capacidad. No se creó ningún registro." }

    $institucionActual = Invoke-JardinApi "/instituciones/$institucionId"
    $revisionJardin = [int](Get-Propiedad $institucionActual "revisionUbicacion" 0)
    $runId = "SIM-$ciclo-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $script:Resultados = [System.Collections.Generic.List[object]]::new()
    $script:ArchivoSalida = Join-Path $env:TEMP "jardin-$runId.csv"

    Write-Host "`nResumen previo" -ForegroundColor Cyan
    Write-Host "Institución: $(Get-Propiedad $institucion 'nombre')"
    Write-Host "Ciclo: $ciclo · estado: $estado · fecha de corte: $($corte.ToString('dd/MM/yyyy'))"
    Write-Host "Edades permitidas: $($edades -join ', ') · turnos habilitados: $($turnos.Count)"
    Write-Host "Cantidad: $Cantidad · datos con apellido '$runId'"
    Write-Host "Salida: $script:ArchivoSalida"
    if ($estado -eq "RecepcionCerrada") {
        Write-Host "ATENCIÓN: la recepción está cerrada; la app registrará las solicitudes fuera de término y pendientes de autorización." -ForegroundColor Yellow
        $aceptaFueraTermino = Read-Host "Para continuar y crear solicitudes fuera de término, escribí ACEPTO FUERA DE TERMINO"
        if ($aceptaFueraTermino -cne "ACEPTO FUERA DE TERMINO") { Write-Host "Cancelado. No se creó ningún registro."; exit 0 }
    }
    $confirmacion = Read-Host "¿Crear $Cantidad registros de prueba? (S/N)"
    if ($confirmacion -notmatch '^(?i:s|si|sí)$') { Write-Host "Cancelado. No se creó ningún registro."; exit 0 }

    $nombresAspirantes = @(
        "Sofía", "Mateo", "Valentina", "Benjamín", "Martina", "Thiago", "Camila", "Joaquín", "Emilia", "Santiago",
        "Lucía", "Tomás", "Catalina", "Bautista", "Julieta", "Felipe", "Mía", "Lautaro", "Delfina", "Bruno",
        "Emma", "Franco", "Olivia", "Lorenzo", "Abril"
    )
    $apellidos = @(
        "García", "González", "Rodríguez", "Fernández", "López", "Martínez", "Pérez", "Gómez", "Sánchez", "Romero",
        "Díaz", "Álvarez", "Torres", "Ruiz", "Ramírez", "Flores", "Acosta", "Benítez", "Medina", "Herrera"
    )
    $callesSimuladas = @(
        "Los Aromos", "Las Acacias", "Los Ceibos", "Los Tilos", "El Ombú", "Las Tipas", "Los Lapachos", "La Arboleda",
        "Los Álamos", "Las Magnolias", "El Jacarandá", "Los Paraísos", "La Colina", "Los Olmos", "Las Violetas", "El Hornero"
    )
    $responsables = @(
        @{ Nombre = "María"; Vinculo = "Madre" }, @{ Nombre = "Carlos"; Vinculo = "Padre" },
        @{ Nombre = "Laura"; Vinculo = "Madre" }, @{ Nombre = "Diego"; Vinculo = "Padre" },
        @{ Nombre = "Patricia"; Vinculo = "Madre" }, @{ Nombre = "Gabriel"; Vinculo = "Padre" },
        @{ Nombre = "Andrea"; Vinculo = "Madre" }, @{ Nombre = "Jorge"; Vinculo = "Padre" },
        @{ Nombre = "Carolina"; Vinculo = "Madre" }, @{ Nombre = "Pablo"; Vinculo = "Padre" },
        @{ Nombre = "Claudia"; Vinculo = "Madre" }, @{ Nombre = "Fernando"; Vinculo = "Padre" },
        @{ Nombre = "Verónica"; Vinculo = "Madre" }, @{ Nombre = "Hernán"; Vinculo = "Padre" },
        @{ Nombre = "Natalia"; Vinculo = "Madre" }, @{ Nombre = "Martín"; Vinculo = "Padre" },
        @{ Nombre = "Silvia"; Vinculo = "Madre" }, @{ Nombre = "Ricardo"; Vinculo = "Padre" },
        @{ Nombre = "Daniela"; Vinculo = "Madre" }, @{ Nombre = "Gustavo"; Vinculo = "Padre" }
    )
    $dnisAspirantesUsados = [System.Collections.Generic.HashSet[string]]::new()
    $documentosResponsablesUsados = [System.Collections.Generic.HashSet[string]]::new()
    $referenciasConsentimientoUsadas = [System.Collections.Generic.HashSet[string]]::new()
    for ($i = 1; $i -le $Cantidad; $i++) {
        $sufijo = "{0:D3}" -f $i
        $sufijoConsentimiento = Nuevo-NumeroUnico 10000 100000 $referenciasConsentimientoUsadas
        $nombreAspirante = $nombresAspirantes[($i - 1) % $nombresAspirantes.Count]
        $apellidoAspirante = $apellidos[(($i - 1) * 7) % $apellidos.Count]
        $responsableElegido = $responsables[($i - 1) % $responsables.Count]
        $nombreResponsable = $responsableElegido.Nombre
        $nombreCompletoAspirante = "$nombreAspirante $apellidoAspirante"
        $nombreCompletoResponsable = "$nombreResponsable $apellidoAspirante"
        $dniAspirante = Nuevo-NumeroUnico 70000000 100000000 $dnisAspirantesUsados
        $dniResponsable = Nuevo-NumeroUnico 20000000 60000000 $documentosResponsablesUsados
        $resultado = [ordered]@{
            Lote = $runId; Ciclo = $ciclo; Numero = $i; Aspirante = $nombreCompletoAspirante; DNI = $dniAspirante; Responsable = $nombreCompletoResponsable
            AspiranteId = ""; SolicitudId = ""; ResponsableId = ""; Estado = "En proceso"; Error = ""
        }
        $script:Resultados.Add([pscustomobject]$resultado)
        try {
            $edadObjetivo = [int]$edades[($i - 1) % $edades.Count]
            $nacimiento = $corte.AddYears(-$edadObjetivo).AddDays(-(($i * 7) % 75))
            while ((Obtener-Edad $nacimiento $corte) -ne $edadObjetivo) { $nacimiento = $nacimiento.AddDays(1) }
            $aspirante = Invoke-JardinApi "/instituciones/$institucionId/aspirantes" "POST" @{
                nombre = $nombreAspirante; apellido = $apellidoAspirante; fechaNacimiento = $nacimiento.ToString("yyyy-MM-dd")
                dni = $dniAspirante; tipoDocumento = "DNI"; sexo = if ($i % 2 -eq 0) { "Femenino" } else { "Masculino" }; periodoId = $periodoId
            }
            $aspiranteId = [string](Get-Propiedad $aspirante "id")
            $script:Resultados[$i - 1].AspiranteId = $aspiranteId

            $turnoPreferido = $turnos[($i - 1) % $turnos.Count]
            $alternativos = @()
            if ($turnos.Count -gt 1) { $alternativos = @($turnos | Where-Object { $_ -ne $turnoPreferido } | Select-Object -First 1) }
            $calle = $callesSimuladas[($i - 1) % $callesSimuladas.Count]
            $domicilio = @{
                calle = $calle; numero = "$($i + 100)"; localidad = "Florencio Varela"; provincia = "Buenos Aires"
                codigoPostal = "1888"; complemento = "Domicilio de prueba - simulación $sufijo"; piso = "Planta baja"
                departamento = "Unidad $sufijo"; barrio = "Barrio de prueba $runId"
            }
            $solicitud = Invoke-JardinApi "/instituciones/$institucionId/periodos/$periodoId/solicitudes" "POST" @{
                aspiranteId = $aspiranteId; domicilio = $domicilio; turnoPreferidoId = $turnoPreferido
                turnosAlternativosAceptados = $alternativos; referenciaConsentimiento = ("CONS-{0}-{1}" -f $ciclo, $sufijoConsentimiento)
            }
            $solicitudId = [string](Get-Propiedad $solicitud "id")
            $script:Resultados[$i - 1].SolicitudId = $solicitudId

            $responsable = Invoke-JardinApi "/instituciones/$institucionId/responsables" "POST" @{
                nombre = $nombreResponsable; apellido = $apellidoAspirante; telefono = ("+54 11 0000-{0:D4}" -f $i)
                documento = $dniResponsable; tipoDocumento = "DNI"
                correoElectronico = "responsable.$($runId.ToLower()).$sufijo@example.invalid"
            }
            $responsableId = [string](Get-Propiedad $responsable "id")
            $script:Resultados[$i - 1].ResponsableId = $responsableId
            $null = Invoke-JardinApi "/instituciones/$institucionId/aspirantes/$aspiranteId/responsables" "POST" @{
                responsableId = $responsableId; tipoVinculo = $responsableElegido.Vinculo; contactoPrincipal = $true; esTutor = $true
            }

            $solicitudActual = Invoke-JardinApi "/instituciones/$institucionId/solicitudes/$solicitudId"
            $null = Invoke-JardinApi "/instituciones/$institucionId/solicitudes/$solicitudId/distancias/manual" "POST" @{
                distanciaMetros = [decimal](180 + (($i * 137) % 4800))
                referencia = "SIMULACION FICTICIA: distancia generada para prueba; no verificada en mapa. Lote $runId."
                revisionDomicilio = [int](Get-Propiedad $solicitudActual "revisionDomicilio" 1)
                revisionUbicacionJardin = $revisionJardin
                ultimoNumeroRegistro = 0
            }
            $script:Resultados[$i - 1].Estado = [string](Get-Propiedad $solicitud "estadoRecepcion" "Registrada")
            Exportar-Avance
            Write-Progress -Activity "Cargando solicitudes sintéticas" -Status "$i de $Cantidad" -PercentComplete (($i / $Cantidad) * 100)
            Write-Host "[$i/$Cantidad] Creado $nombreCompletoAspirante" -ForegroundColor Green
        }
        catch {
            $script:Resultados[$i - 1].Estado = "Incompleto"
            $script:Resultados[$i - 1].Error = $_.Exception.Message
            Exportar-Avance
            throw "Falló el registro $i ($nombreCompletoAspirante). Se detuvo la carga; revisá el CSV para identificar lo que alcanzó a crearse. Detalle: $($_.Exception.Message)"
        }
    }
    Write-Progress -Activity "Cargando solicitudes sintéticas" -Completed
    Exportar-Avance
    Write-Host "`nCarga finalizada: $Cantidad solicitudes." -ForegroundColor Green
    Write-Host "El detalle con los identificadores quedó en: $script:ArchivoSalida"
    Write-Host "Las distancias son simuladas y no deben usarse para decisiones reales."
}
catch {
    Write-Progress -Activity "Cargando solicitudes sintéticas" -Completed -ErrorAction SilentlyContinue
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($script:ArchivoSalida) { Write-Host "Si ya habían empezado a crearse registros, revisá: $script:ArchivoSalida" -ForegroundColor Yellow }
    exit 1
}
finally {
    $clave = $null
    $token = $null
    $script:Headers = $null
}
