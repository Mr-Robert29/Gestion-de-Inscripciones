using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Contracts;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioDisponibilidadComposicion(
    EjecucionEspera ejecucion,
    IContextoPropuestaIngresos contexto,
    IRepositorioPropuestasIngresos repositorio) : IModuloDisponibilidadComposicion
{
    // Implementa IModuloDisponibilidadComposicion.ConPeriodoProtegidoAsync sin exponer entidades internas.
    public Task<T> ConPeriodoProtegidoAsync<T>(
        Guid periodoId,
        Func<DatosDisponibilidadComposicion, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => ejecucion.EjecutarAsync(
            periodoId,
            async token =>
            {
                var entradas = await contexto.LeerAsync(periodoId, token);
                var ingresos = await repositorio.ListarIngresosAsync(periodoId, token);
                var ocupaciones = await repositorio.ListarOcupacionesAsync(periodoId, token);
                return await operacion(new DatosDisponibilidadComposicion(
                    entradas.Ofertas.Select(x => new DatosOfertaComposicion(
                        x.Id,
                        x.AulaId,
                        x.TurnoId,
                        x.Capacidad,
                        x.Disponible,
                        x.CantidadOcupacionInicial ?? x.EdadesOcupacionesIniciales.Count,
                        x.EdadesOcupacionesIniciales)).ToArray(),
                    ocupaciones.Where(x => x.Activa).Select(x => new DatosOcupacionComposicion(x.OfertaId, x.Sala)).ToArray(),
                    ingresos.Where(x => x.Activo && !x.BajaEnProceso).Select(x => new DatosIngresoConfirmado(
                        x.Id,
                        x.AspiranteId,
                        x.Edad,
                        x.TurnoId,
                        true)).ToArray()), token);
            },
            tokenCancelacion);
}
