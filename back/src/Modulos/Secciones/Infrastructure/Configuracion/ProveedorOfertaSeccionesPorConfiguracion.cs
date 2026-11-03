using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Domain;
using Microsoft.Extensions.Configuration;

namespace Jardin.Secciones.Infrastructure.Configuracion;

public sealed class ProveedorOfertaSeccionesPorConfiguracion : IProveedorOfertaSecciones
{
    private readonly IConfiguration _configuracion;

    public ProveedorOfertaSeccionesPorConfiguracion(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    public Task<OfertaSeccionConfigurada?> ObtenerAsync(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"Secciones:Oferta:{periodoId}:{aulaId}:{turnoId}";
        var seccion = _configuracion.GetSection(ruta);
        if (!seccion.Exists())
            return Task.FromResult<OfertaSeccionConfigurada?>(null);

        var capacidadTexto = seccion["Capacidad"];
        if (!int.TryParse(capacidadTexto, out var capacidad) || capacidad <= 0)
            throw new InvalidOperationException("La capacidad de la oferta debe estar configurada y ser mayor a cero.");

        var combinaciones = seccion.GetSection("CombinacionesPermitidas")
            .GetChildren()
            .Select(c => (c.Value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList())
            .Cast<IReadOnlyList<int>>()
            .ToList();

        if (combinaciones.Count == 0)
            combinaciones = ReglasSeccion.CombinacionesAprobadasPorDefecto.ToList();

        return Task.FromResult<OfertaSeccionConfigurada?>(new OfertaSeccionConfigurada(
            periodoId,
            aulaId,
            turnoId,
            capacidad,
            combinaciones));
    }
}
