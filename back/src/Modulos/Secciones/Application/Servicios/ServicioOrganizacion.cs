using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Servicios;

public sealed class ServicioOrganizacion : IModuloSecciones
{
    private readonly IRepositorioSecciones _repositorio;
    private readonly IProveedorOfertaSecciones _oferta;

    public ServicioOrganizacion(
        IRepositorioSecciones repositorio,
        IProveedorOfertaSecciones oferta)
    {
        _repositorio = repositorio;
        _oferta = oferta;
    }

    public async Task<Guid> DefinirSeccionAsync(
        DefinirSeccionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var oferta = await _oferta.ObtenerAsync(
            peticion.PeriodoId,
            peticion.AulaId,
            peticion.TurnoId,
            cancellationToken);

        if (oferta is null)
        {
            throw new InvalidOperationException(
                "El aula y el turno no pertenecen a la oferta configurada para el período.");
        }

        var seccion = new Seccion(
            oferta.PeriodoId,
            oferta.AulaId,
            oferta.TurnoId,
            peticion.Nombre,
            peticion.EdadesAdmitidas,
            oferta.Capacidad,
            oferta.CombinacionesPermitidas);

        await _repositorio.AgregarAsync(seccion, cancellationToken);
        return seccion.Id;
    }

    public async Task<DatosSeccion?> ObtenerSeccionPorIdAsync(
        Guid seccionId,
        CancellationToken cancellationToken = default)
    {
        var seccion = await _repositorio.ObtenerPorIdAsync(seccionId, cancellationToken);
        return seccion is null ? null : Mapear(seccion);
    }

    public async Task<IReadOnlyList<DatosSeccion>> ListarSeccionesPorPeriodoAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var secciones = await _repositorio.ListarPorPeriodoAsync(periodoId, cancellationToken);
        return secciones.Select(Mapear).ToList();
    }

    public async Task<bool> CambiarComposicionSeccionAsync(
        Guid seccionId,
        CambiarComposicionSeccionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var seccion = await _repositorio.ObtenerPorIdAsync(seccionId, cancellationToken);
        if (seccion is null)
            return false;

        var oferta = await _oferta.ObtenerAsync(
            seccion.PeriodoId,
            seccion.AulaId,
            seccion.TurnoId,
            cancellationToken);

        if (oferta is null)
            throw new InvalidOperationException("La oferta del período ya no está disponible.");
        seccion.CambiarComposicion(
            peticion.NuevasEdades,
            peticion.Motivo,
            oferta.CombinacionesPermitidas,
            edadesAlumnosAsignados: null);

        await _repositorio.ActualizarAsync(seccion, cancellationToken);
        return true;
    }

    private static DatosSeccion Mapear(Seccion seccion) => new(
        seccion.Id,
        seccion.PeriodoId,
        seccion.AulaId,
        seccion.TurnoId,
        seccion.Nombre,
        seccion.Tipo.ToString(),
        seccion.EdadesAdmitidas.ToList(),
        seccion.Capacidad,
        seccion.Revision,
        seccion.Activa);
}
