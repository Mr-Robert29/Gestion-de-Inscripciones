using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioNominaDistrito : IModuloNominaDistrito
{
    private readonly IAutorizacionNominaDistrito _autorizacion;
    private readonly IModuloListaEspera _listaEspera;

    public ServicioNominaDistrito(
        IAutorizacionNominaDistrito autorizacion,
        IModuloListaEspera listaEspera)
    {
        _autorizacion = autorizacion;
        _listaEspera = listaEspera;
    }

    public async Task<ConsultarNominaDistritoRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default)
    {
        if (institucionId == Guid.Empty) throw new ArgumentException("La institución no puede estar vacía.", nameof(institucionId));
        if (periodoId == Guid.Empty) throw new ArgumentException("El período no puede estar vacío.", nameof(periodoId));

        var acceso = await _autorizacion.AutorizarConsultaAsync(periodoId, tokenCancelacion);
        if (acceso.InstitucionId != institucionId)
            throw new KeyNotFoundException("El período no pertenece a la institución indicada.");

        var fuente = await LeerListaCompletaAsync(institucionId, periodoId, tokenCancelacion);
        return new ConsultarNominaDistritoRespuesta(
            fuente.InstitucionId,
            fuente.PeriodoId,
            DateTimeOffset.UtcNow,
            fuente.EstadoProceso,
            fuente.RequiereRevision,
            fuente.PropuestaConfirmadaId,
            fuente.FechaConfirmacion,
            fuente.TotalEnEspera,
            fuente.TotalPendientes,
            fuente.EnEspera.Select(Mapear).ToArray(),
            fuente.Pendientes.Select(Mapear).ToArray());
    }

    private async Task<ListaCompleta> LeerListaCompletaAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        var enEspera = new List<DatosIntegranteListaEspera>();
        var pendientes = new List<DatosIntegranteListaEspera>();
        ConsultarListaEsperaRespuesta? primera = null;
        Guid? propuesta = null;

        for (var pagina = 1; ; pagina++)
        {
            var respuesta = await _listaEspera.ConsultarAsync(
                institucionId,
                periodoId,
                new ConsultarListaEsperaPeticion(Pagina: pagina, TamanoPagina: 100),
                tokenCancelacion);
            primera ??= respuesta;
            if (propuesta is null) propuesta = respuesta.PropuestaConfirmadaId;
            else if (propuesta != respuesta.PropuestaConfirmadaId)
                throw new InvalidOperationException("La lista de espera cambió durante la generación de la nómina.");

            enEspera.AddRange(respuesta.EnEspera.Resultados);
            pendientes.AddRange(respuesta.Pendientes.Resultados);
            if (!respuesta.EnEspera.HayMas && !respuesta.Pendientes.HayMas) break;
        }

        var cabecera = primera ?? throw new InvalidOperationException("No se pudo consultar la lista de espera.");
        return new ListaCompleta(
            cabecera.InstitucionId,
            cabecera.PeriodoId,
            cabecera.EstadoProceso,
            cabecera.RequiereRevision,
            cabecera.PropuestaConfirmadaId,
            cabecera.FechaConfirmacion,
            enEspera.Count,
            pendientes.Count,
            enEspera,
            pendientes);
    }

    private static DatosNominaDistrito Mapear(DatosIntegranteListaEspera dato) => new(
        dato.SolicitudId,
        dato.AspiranteId,
        dato.Nombre,
        dato.Apellido,
        dato.DniUtilizado,
        dato.Sala,
        dato.EdadUtilizada,
        dato.PosicionGeneral,
        dato.PosicionEspera,
        dato.Estado,
        dato.Motivo,
        dato.TurnoPreferidoId,
        dato.DistanciaMetrosUtilizada,
        dato.DatosCoincidenConResultado,
        dato.CasosRevision);

    private sealed record ListaCompleta(
        Guid InstitucionId,
        Guid PeriodoId,
        string EstadoProceso,
        bool RequiereRevision,
        Guid? PropuestaConfirmadaId,
        DateTimeOffset? FechaConfirmacion,
        int TotalEnEspera,
        int TotalPendientes,
        IReadOnlyList<DatosIntegranteListaEspera> EnEspera,
        IReadOnlyList<DatosIntegranteListaEspera> Pendientes);
}
