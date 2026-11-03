using Jardin.Configuracion.Contracts;
using Jardin.Vacantes.Application.Seguridad;

namespace Jardin.Api.Seguridad;

public sealed class ProveedorEstadoPeriodo : IProveedorEstadoPeriodo
{
    private readonly IModuloPeriodos _periodos;

    public ProveedorEstadoPeriodo(IModuloPeriodos periodos)
    {
        _periodos = periodos;
    }

    public async Task<EstadoPeriodoVacantes> ConsultarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var periodo = await _periodos.ConsultarPeriodoAsync(periodoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el período.");
        return new EstadoPeriodoVacantes(periodo.InstitucionId, periodo.Estado, periodo.Revision);
    }
}
