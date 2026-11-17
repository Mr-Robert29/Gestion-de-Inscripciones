using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;

namespace Jardin.Vacantes.Application.Servicios;

public interface IProcesadorPropuestaIngresosEnSegundoPlano
{
    Task<ConsultarPropuestaIngresosRespuesta> GenerarAutorizadoAsync(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        AutorizacionVacantes autor,
        CancellationToken tokenCancelacion = default);
}
