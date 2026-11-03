using System.Globalization;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioSorteos : IModuloSorteos
{
    private readonly IRepositorioSorteos _repositorio;
    private readonly IAutorizacionVacantes _autorizacion;

    public ServicioSorteos(
        IRepositorioSorteos repositorio,
        IAutorizacionVacantes autorizacion)
    {
        _repositorio = repositorio;
        _autorizacion = autorizacion;
    }

    public async Task<DatosSorteo> RegistrarAsync(
        Guid periodoId,
        RegistrarSorteoPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);

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
