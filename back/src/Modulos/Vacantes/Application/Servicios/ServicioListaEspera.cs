using System.Text.Json;
using Jardin.Inscripciones.Contracts;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioListaEspera : IModuloListaEspera
{
    private readonly IAutorizacionListaEspera _autorizacion;
    private readonly IRepositorioPropuestasIngresos _repositorio;
    private readonly IContextoPropuestaIngresos _contexto;
    private readonly IModuloPersonasInscripciones _personas;
    private readonly IRepositorioSorteos _sorteos;
    private readonly IRepositorioCasosRevision _casos;

    public ServicioListaEspera(
        IAutorizacionListaEspera autorizacion,
        IRepositorioPropuestasIngresos repositorio,
        IContextoPropuestaIngresos contexto,
        IModuloPersonasInscripciones personas,
        IRepositorioSorteos sorteos,
        IRepositorioCasosRevision casos)
    {
        _autorizacion = autorizacion;
        _repositorio = repositorio;
        _contexto = contexto;
        _personas = personas;
        _sorteos = sorteos;
        _casos = casos;
    }

    // Implementa IModuloListaEspera.ConsultarAsync: lectura de una lista institucional; no guarda ni confirma resultados.
    public async Task<ConsultarListaEsperaRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid periodoId,
        ConsultarListaEsperaPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var acceso = await _autorizacion.AutorizarConsultaEsperaAsync(periodoId, tokenCancelacion);
        if (acceso.InstitucionId != institucionId) throw new KeyNotFoundException("El período no pertenece a la institución indicada.");
        ValidarFiltros(peticion);
        var propuestas = await _repositorio.ListarAsync(periodoId, tokenCancelacion);
        var ingresos = await _repositorio.ListarIngresosActivosAsync(periodoId, tokenCancelacion);
        var confirmadas = propuestas.Where(x => x.Estado == EstadoPropuestaIngreso.Confirmada).ToArray();
        if (confirmadas.Length == 0)
            return RespuestaVacia(institucionId, periodoId, peticion, ingresos.Count > 0);
        if (confirmadas.Any(x => ingresos.All(i => i.PropuestaId != x.Id)))
            throw new InvalidOperationException("Hay una propuesta confirmada sin ingresos registrados. Revisá su confirmación antes de consultar la espera.");
        var ordenadas = confirmadas.Select(x => new { Propuesta = x, Fecha = ingresos.Where(i => i.PropuestaId == x.Id).Max(i => i.Fecha) })
            .OrderByDescending(x => x.Fecha).ToArray();
        if (ordenadas.Length > 1 && ordenadas[0].Fecha == ordenadas[1].Fecha)
            throw new InvalidOperationException("Dos resultados tienen la misma fecha de confirmación. Se requiere identificar el resultado aplicable.");
        var propuesta = ordenadas[0].Propuesta;
        var fuente = FuenteListaEspera.Leer(propuesta);
        var origen = await ObtenerOrigenOrdenAsync(propuesta, tokenCancelacion);
        var posiciones = PosicionesOriginales(FuenteListaEspera.Leer(origen).Resultados);
        var avisos = new List<string>();
        EntradasPropuestaIngresos? actuales;
        try { actuales = await _contexto.LeerAsync(periodoId, tokenCancelacion); }
        catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException)
        {
            actuales = null;
            avisos.Add("La configuración actual no permite comprobar la vigencia del orden histórico.");
        }
        var cambianEntradas = actuales is null || CambioOrden(fuente.Entradas, actuales);
        var casos = await _casos.ListarAsync(periodoId, tokenCancelacion);
        var sorteos = await _sorteos.ListarVigentesAsync(periodoId, tokenCancelacion);
        if (!MismasRevisiones(fuente.Casos, casos.Select(x => (x.Id, x.Revision))) ||
            !MismasRevisiones(fuente.Sorteos, sorteos.Select(x => (x.Id, x.Version)), "Version"))
            cambianEntradas = true;
        if (cambianEntradas) avisos.Add("Los datos actuales pueden afectar el orden. Se conserva el resultado confirmado y la espera afectada se presenta pendiente de revisión.");
        var ocupaciones = await _repositorio.ListarOcupacionesAsync(periodoId, tokenCancelacion);
        var conIngreso = ingresos.Select(x => x.AspiranteId)
            .Concat(ocupaciones.Where(x => x.Tipo == "Ingreso").Select(x => x.AspiranteId)).ToHashSet();
        var reservas = ocupaciones.Where(x => x.Tipo == "Reserva").Select(x => x.AspiranteId).ToHashSet();
        var nuevas = actuales?.Solicitudes.Where(x => fuente.Entradas.Solicitudes.All(s => s.Id != x.Id)).ToArray() ?? [];
        var registros = ConstructorListaEspera.Construir(
            fuente.Entradas.Solicitudes,
            fuente.Resultados,
            posiciones,
            conIngreso,
            reservas,
            cambianEntradas,
            nuevas);
        var filtrados = registros.Where(x => Coincide(x, peticion)).ToArray();
        var espera = filtrados.Where(x => x.Estado != "PendienteRevision").ToArray();
        var pendientes = filtrados.Where(x => x.Estado == "PendienteRevision").ToArray();
        var paginaEspera = await PaginarAsync(espera, fuente, actuales, peticion, tokenCancelacion);
        var paginaPendientes = await PaginarAsync(pendientes, fuente, actuales, peticion, tokenCancelacion);
        return new ConsultarListaEsperaRespuesta(
            institucionId,
            periodoId,
            DateTimeOffset.UtcNow,
            propuesta.Id,
            origen.Id,
            ordenadas[0].Fecha,
            fuente.Entradas.VersionReglasId,
            fuente.Entradas.VersionOfertaId,
            "ConResultadosConfirmados",
            cambianEntradas || registros.Any(x => x.Estado == "PendienteRevision"),
            avisos,
            fuente.Entradas.OrdenProcedimiento,
            registros.Count(x => x.Estado != "PendienteRevision"),
            registros.Count(x => x.Estado == "PendienteRevision"),
            paginaEspera,
            paginaPendientes);
    }

    private async Task<PropuestaIngresos> ObtenerOrigenOrdenAsync(PropuestaIngresos propuesta, CancellationToken tokenCancelacion)
    {
        var visitadas = new HashSet<Guid> { propuesta.Id };
        var actual = propuesta;
        while (actual.PropuestaAnteriorId is Guid anteriorId)
        {
            if (!visitadas.Add(anteriorId)) throw new InvalidOperationException("La cadena histórica de propuestas contiene un ciclo.");
            var anterior = await _repositorio.ObtenerAsync(anteriorId, tokenCancelacion)
                ?? throw new InvalidOperationException("No se conserva el origen del orden de la propuesta.");
            if (anterior.PeriodoId != propuesta.PeriodoId || anterior.InstitucionId != propuesta.InstitucionId || anterior.HuellaEntradas != propuesta.HuellaEntradas)
                throw new InvalidOperationException("El origen del orden no coincide con las entradas confirmadas.");
            actual = anterior;
        }
        return actual;
    }

    private static IReadOnlyDictionary<Guid, int> PosicionesOriginales(IReadOnlyList<ResultadoPropuestaIngreso> resultados)
    {
        var posicion = 0;
        return resultados.Where(x => x.Estado is "IngresoPropuesto" or "EsperaPropuesta")
            .ToDictionary(x => x.SolicitudId, _ => ++posicion);
    }

    private static bool CambioOrden(EntradasPropuestaIngresos anteriores, EntradasPropuestaIngresos actuales)
        => anteriores.VersionReglasId != actuales.VersionReglasId || anteriores.VersionOfertaId != actuales.VersionOfertaId ||
            actuales.EstadoPeriodo != "RecepcionCerrada" ||
            JsonSerializer.Serialize(anteriores.Solicitudes.Where(x => !x.FueraDeTermino).OrderBy(x => x.Id)) !=
            JsonSerializer.Serialize(actuales.Solicitudes.Where(x => !x.FueraDeTermino).OrderBy(x => x.Id));

    private static bool MismasRevisiones(
        IReadOnlyList<JsonElement> historicas,
        IEnumerable<(Guid Id, int Revision)> actuales,
        string propiedad = "Revision")
        => historicas.Select(x => (Id: x.GetProperty("Id").GetGuid(), Revision: x.GetProperty(propiedad).GetInt32())).OrderBy(x => x.Id)
            .SequenceEqual(actuales.OrderBy(x => x.Id));

    private async Task<DatosPaginaListaEspera> PaginarAsync(
        IReadOnlyList<RegistroListaEspera> registros,
        FuenteListaEspera fuente,
        EntradasPropuestaIngresos? actuales,
        ConsultarListaEsperaPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        var resultados = new List<DatosIntegranteListaEspera>();
        var omitidos = ((long)peticion.Pagina - 1) * peticion.TamanoPagina;
        var pagina = omitidos >= registros.Count ? [] : registros.Skip((int)omitidos).Take(peticion.TamanoPagina).ToArray();
        foreach (var registro in pagina)
        {
            var solicitud = registro.Solicitud;
            var persona = await _personas.ConsultarAspiranteAsync(fuente.Entradas.InstitucionId, solicitud.AspiranteId, tokenCancelacion);
            var actual = actuales?.Solicitudes.SingleOrDefault(x => x.Id == solicitud.Id);
            var incluidaEnResultado = fuente.Entradas.Solicitudes.Any(x => x.Id == solicitud.Id);
            resultados.Add(new DatosIntegranteListaEspera(
                solicitud.Id,
                solicitud.AspiranteId,
                persona?.Nombre ?? "Identidad no disponible",
                persona?.Apellido ?? "",
                solicitud.Dni,
                solicitud.Sala,
                solicitud.Edad,
                registro.PosicionGeneral,
                registro.PosicionEspera,
                registro.Estado,
                registro.Motivo,
                solicitud.TurnoPreferidoId,
                solicitud.Alternativas,
                solicitud.Condiciones,
                solicitud.DistanciaMetros,
                solicitud.DistanciaMetros is decimal distancia ? Math.Round(distancia, fuente.Entradas.PrecisionDistanciaDecimales, MidpointRounding.AwayFromZero) : null,
                solicitud.MedicionId,
                incluidaEnResultado && actual is not null && JsonSerializer.Serialize(actual) == JsonSerializer.Serialize(solicitud),
                incluidaEnResultado ? fuente.ConsultarSorteo(solicitud) : null,
                fuente.ConsultarCasos(solicitud.Id)));
        }
        return new DatosPaginaListaEspera(resultados, registros.Count, peticion.Pagina, peticion.TamanoPagina, omitidos + resultados.Count < registros.Count);
    }

    private static bool Coincide(RegistroListaEspera registro, ConsultarListaEsperaPeticion peticion)
        => (peticion.Sala is null || registro.Solicitud.Sala == peticion.Sala) &&
            (peticion.TurnoId is null || registro.Solicitud.TurnoPreferidoId == peticion.TurnoId || registro.Solicitud.Alternativas.Contains(peticion.TurnoId.Value)) &&
            (peticion.Estado is null || registro.Estado == peticion.Estado);

    private static void ValidarFiltros(ConsultarListaEsperaPeticion peticion)
    {
        if (peticion.Sala is < 3 or > 5) throw new ArgumentException("La sala debe ser 3, 4 o 5.");
        if (peticion.TurnoId == Guid.Empty) throw new ArgumentException("El identificador de turno no es válido.");
        if (peticion.Estado is not (null or "EnEspera" or "ConReserva" or "PendienteRevision"))
            throw new ArgumentException("El estado debe ser EnEspera, ConReserva o PendienteRevision.");
        if (peticion.Pagina < 1 || peticion.TamanoPagina is < 1 or > 100)
            throw new ArgumentException("La página debe ser positiva y el tamaño debe estar entre 1 y 100.");
    }

    private static ConsultarListaEsperaRespuesta RespuestaVacia(
        Guid institucionId,
        Guid periodoId,
        ConsultarListaEsperaPeticion peticion,
        bool hayIngresos)
    {
        var pagina = new DatosPaginaListaEspera([], 0, peticion.Pagina, peticion.TamanoPagina, false);
        return new ConsultarListaEsperaRespuesta(
            institucionId, periodoId, DateTimeOffset.UtcNow, null, null, null, null, null,
            "SinResultadosConfirmados", hayIngresos,
            hayIngresos ? ["Existen ingresos sin una propuesta confirmada aplicable. Revisá la consistencia del proceso."] : [],
            [], 0, 0, pagina, pagina);
    }
}
