using Jardin.Vacantes.Contracts;
using Microsoft.Extensions.Configuration;

namespace Jardin.Secciones.Infrastructure.Configuracion;

public sealed class ProveedorIngresosConfirmadosPorConfiguracion : IModuloVacantes
{
    private readonly IConfiguration _configuracion;

    public ProveedorIngresosConfirmadosPorConfiguracion(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    public Task<RevisionIngresosActivos> ObtenerIngresosActivosAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"Vacantes:IngresosConfirmados:{periodoId}";
        var seccion = _configuracion.GetSection(ruta);
        var revision = int.TryParse(seccion["Revision"], out var valorRevision)
            ? valorRevision
            : 0;
        var ingresos = seccion.GetSection("Ingresos")
            .GetChildren()
            .Select(x => new DatosIngresoConfirmado(
                Guid.Parse(x["IngresoId"] ?? Guid.Empty.ToString()),
                Guid.Parse(x["AspiranteId"] ?? Guid.Empty.ToString()),
                int.Parse(x["Edad"] ?? "0"),
                Guid.Parse(x["TurnoId"] ?? Guid.Empty.ToString()),
                bool.Parse(x["Activo"] ?? "true")))
            .ToList();

        return Task.FromResult(new RevisionIngresosActivos(revision, ingresos));
    }
}
