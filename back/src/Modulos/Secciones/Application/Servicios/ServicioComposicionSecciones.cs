using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Domain.Entidades;
using Jardin.Vacantes.Contracts;

namespace Jardin.Secciones.Application.Servicios;

public sealed class ServicioComposicionSecciones(
    IRepositorioSecciones secciones,
    IRepositorioComposiciones repositorio,
    IProveedorOfertaSecciones oferta,
    IModuloDisponibilidadComposicion vacantes,
    IAutorizacionComposicion autorizacion) : IModuloComposicionSecciones
{
    // Implementa IModuloComposicionSecciones.CambiarAsync: no registra ingresos ni ofrecimientos.
    public async Task<CambiarComposicionSeccionRespuesta> CambiarAsync(
        Guid periodoId,
        Guid seccionId,
        CambiarComposicionSeccionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentNullException.ThrowIfNull(peticion.NuevasEdades);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (peticion.Revision < 1) throw new ArgumentException("Indicá la revisión vigente de la sección.");
        var usuarioId = await autorizacion.AutorizarCambioAsync(periodoId, tokenCancelacion);
        return await vacantes.ConPeriodoProtegidoAsync(
            periodoId,
            async (datos, token) => await repositorio.ConSeccionProtegidaAsync(
                periodoId,
                seccionId,
                async (seccion, asignados, protegido) =>
                {
                    if (!seccion.Activa || seccion.Revision != peticion.Revision)
                        throw new InvalidOperationException("La sección está inactiva o cambió su revisión. Volvé a consultarla.");
                    var recurso = datos.Ofertas.SingleOrDefault(x => x.AulaId == seccion.AulaId && x.TurnoId == seccion.TurnoId && x.Disponible)
                        ?? throw new InvalidOperationException("El aula y turno ya no están disponibles en la oferta publicada.");
                    var configurada = await oferta.ObtenerAsync(
                        periodoId,
                        seccion.AulaId,
                        seccion.TurnoId,
                        protegido) ?? throw new InvalidOperationException("No existe oferta para la sección.");
                    var ocupaciones = datos.Ocupaciones.Where(x => x.OfertaId == recurso.Id).ToArray();
                    var capacidad = Math.Min(recurso.Capacidad, seccion.Capacidad);
                    if (recurso.CantidadInicial + ocupaciones.Length >= capacidad || asignados.Count >= capacidad)
                        throw new InvalidOperationException("No hay una vacante libre para cambiar la composición.");
                    var ingresos = datos.Ingresos.ToDictionary(x => x.IngresoId);
                    if (asignados.Any(x => !ingresos.ContainsKey(x)))
                        throw new InvalidOperationException("Hay asignaciones sin ingreso activo. Revisá las bajas antes de cambiar.");
                    if (asignados.Any(x => ingresos[x].TurnoId != seccion.TurnoId))
                        throw new InvalidOperationException("Una asignación tiene un turno incompatible.");
                    var anteriores = seccion.EdadesAdmitidas.ToArray();
                    var nuevas = peticion.NuevasEdades.Distinct().Order().ToArray();
                    if (anteriores.Order().SequenceEqual(nuevas))
                        throw new InvalidOperationException("La composición solicitada coincide con la actual.");
                    if (seccion.Tipo != Jardin.Secciones.Domain.TipoSeccion.Pura || nuevas.Length < 2 || !anteriores.All(nuevas.Contains))
                        throw new InvalidOperationException("HU-26 amplía una sección pura a multiedad conservando su edad original.");
                    // También las reservas e ingresos todavía sin asignación deben conservar su compatibilidad.
                    var edades = recurso.EdadesIniciales.Concat(ocupaciones.Select(x => x.Edad))
                        .Concat(asignados.Select(x => ingresos[x].Edad)).ToArray();
                    if (recurso.CantidadInicial != recurso.EdadesIniciales.Count)
                        throw new InvalidOperationException("Faltan las edades de las ocupaciones iniciales para comprobar compatibilidad.");
                    seccion.CambiarComposicion(
                        nuevas,
                        peticion.Motivo,
                        configurada.CombinacionesPermitidas,
                        edades);
                    var afectadas = await repositorio.GuardarAsync(new CambioComposicionSeccion(
                        seccion,
                        anteriores,
                        peticion.Revision,
                        usuarioId,
                        peticion.Motivo), protegido);
                    return new CambiarComposicionSeccionRespuesta(
                        seccion.Id,
                        seccion.Revision,
                        seccion.EdadesAdmitidas.ToArray(),
                        afectadas);
                },
                token),
            tokenCancelacion);
    }

    // Implementa IModuloComposicionSecciones.ConsultarHistorialAsync con control institucional.
    public async Task<IReadOnlyList<DatosCambioComposicion>> ConsultarHistorialAsync(
        Guid periodoId,
        Guid seccionId,
        CancellationToken tokenCancelacion = default)
    {
        await autorizacion.AutorizarCambioAsync(periodoId, tokenCancelacion);
        var seccion = await secciones.ObtenerPorIdAsync(seccionId, tokenCancelacion);
        if (seccion is null || seccion.PeriodoId != periodoId) throw new KeyNotFoundException("No se encontró la sección en el período.");
        return (await repositorio.ConsultarHistorialAsync(seccionId, tokenCancelacion)).Select(x => new DatosCambioComposicion(
            x.Id,
            x.SeccionId,
            x.EdadesAnteriores,
            x.EdadesNuevas,
            x.RevisionAnterior,
            x.RevisionNueva,
            x.UsuarioId,
            x.Fecha,
            x.Motivo)).ToArray();
    }
}
