using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Application.Geografia;
using Jardin.Inscripciones.Domain.ObjetosDeValor;

namespace Jardin.Api.Integraciones;

public sealed class ConsultaUbicacionJardin : IUbicacionJardin
{
    private readonly IModuloConfiguracion _configuracion;
    public ConsultaUbicacionJardin(IModuloConfiguracion configuracion) { _configuracion = configuracion; }

    // Implementa IUbicacionJardin.ConsultarAsync: traduce el contrato público al dato requerido por Inscripciones.
    public async Task<UbicacionJardin> ConsultarAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        var institucion = await _configuracion.ConsultarInstitucionAsync(institucionId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el jardín.");
        var d = institucion.Domicilio;
        return new UbicacionJardin(
            institucion.RevisionUbicacion,
            new Domicilio(
                d.Calle,
                d.Numero,
                d.Localidad,
                d.Provincia,
                d.CodigoPostal,
                d.Complemento,
                d.Piso,
                d.Departamento,
                d.Barrio),
            institucion.Coordenadas is null ? null : new Coordenadas(institucion.Coordenadas.Latitud, institucion.Coordenadas.Longitud));
    }
}
