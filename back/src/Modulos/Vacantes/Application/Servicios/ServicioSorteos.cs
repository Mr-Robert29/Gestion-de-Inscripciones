using System.Globalization;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;
using Jardin.Inscripciones.Contracts;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioSorteos : IModuloSorteos
{
    private readonly IRepositorioSorteos _repositorio;
    private readonly IAutorizacionVacantes _autorizacion;
    private readonly IProveedorEstadoPeriodo _periodos;
    private readonly IModuloParticipantesSorteo _participantes;

    public ServicioSorteos(
        IRepositorioSorteos repositorio,
        IAutorizacionVacantes autorizacion,
        IProveedorEstadoPeriodo periodos,
        IModuloParticipantesSorteo participantes)
    {
        _repositorio = repositorio;
        _autorizacion = autorizacion;
        _periodos = periodos;
        _participantes = participantes;
    }

    public async Task<DatosSorteo> RegistrarAsync(
        Guid periodoId,
        RegistrarSorteoPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        await ExigirRecepcionCerradaAsync(periodoId, cancellationToken);

        Sorteo.ValidarNumero(peticion.Numero);
        Sorteo.ValidarSala(peticion.Sala);
        var anterior = await _repositorio.ObtenerVigenteAsync(
            periodoId,
            peticion.Sala,
            cancellationToken);
        var version = await _repositorio.ObtenerUltimaVersionAsync(
            periodoId,
            peticion.Sala,
            cancellationToken) + 1;

        var nuevo = new Sorteo(
            periodoId,
            peticion.Sala,
            peticion.Numero,
            peticion.Fecha,
            peticion.Referencia,
            version,
            autorizacion.UsuarioId,
            DateTimeOffset.UtcNow);

        await _repositorio.GuardarNuevaVersionAsync(nuevo, anterior, cancellationToken);
        return Mapear(nuevo);
    }

    public async Task<IReadOnlyList<DatosSorteo>> ListarVigentesAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        var sorteos = await _repositorio.ListarVigentesAsync(periodoId, cancellationToken);
        return sorteos.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<DatosOrdenSorteo>> CalcularOrdenAsync(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        await ExigirRecepcionCerradaAsync(periodoId, cancellationToken);
        Sorteo.ValidarSala(sala);

        var sorteo = await _repositorio.ObtenerVigenteAsync(periodoId, sala, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró un sorteo vigente para la sala indicada.");
        var participantes = await _participantes.ListarAsync(
            autorizacion.InstitucionId,
            periodoId,
            sala,
            cancellationToken);
        var orden = OrdenadorSorteoCircular.Ordenar(
            sorteo.Numero,
            participantes.Select(x => new ParticipanteSorteo(
                x.SolicitudId,
                x.AspiranteId,
                x.Sala,
                x.Dni)));

        return orden.Select(x => new DatosOrdenSorteo(
            sorteo.Id,
            sorteo.Version,
            x.SolicitudId,
            x.AspiranteId,
            x.Sala,
            x.TerminacionDni,
            x.ClaveCircular,
            x.Posicion,
            x.RequiereRevision,
            x.MotivoRevision)).ToArray();
    }

    private async Task ExigirRecepcionCerradaAsync(
        Guid periodoId,
        CancellationToken cancellationToken)
    {
        var estado = await _periodos.ConsultarAsync(periodoId, cancellationToken);
        if (!estado.RecepcionCerrada)
            throw new InvalidOperationException("El sorteo solo puede utilizarse con la recepción cerrada.");
    }

    private static DatosSorteo Mapear(Sorteo sorteo) => new(
        sorteo.Id,
        sorteo.PeriodoId,
        sorteo.Sala,
        sorteo.Numero.ToString("D3", CultureInfo.InvariantCulture),
        sorteo.Fecha,
        sorteo.Referencia,
        sorteo.Version,
        sorteo.UsuarioId,
        sorteo.FechaRegistro);
}
