using System.Text.Json;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

// HU-06 prepara personas y vínculos; no registra solicitudes ni asigna vacantes.
public sealed class ServicioPersonasInscripciones : IModuloPersonasInscripciones
{
    private readonly IRepositorioPersonasInscripciones _personas;
    private readonly IAccesoInstitucionesInscripciones _acceso;

    public ServicioPersonasInscripciones(IRepositorioPersonasInscripciones personas, IAccesoInstitucionesInscripciones acceso)
    {
        _personas = personas;
        _acceso = acceso;
    }

    // Implementa IModuloPersonasInscripciones.RegistrarAspiranteAsync.
    public async Task<RegistrarAspiranteRespuesta> RegistrarAspiranteAsync(
        Guid institucionId,
        RegistrarAspirantePeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarAsync(
            institucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        var aspirante = new Aspirante(
            peticion.Nombre,
            peticion.Apellido,
            peticion.Dni,
            peticion.FechaNacimiento,
            peticion.TipoDocumento,
            peticion.Sexo);
        await _personas.AgregarAspiranteAsync(
            aspirante,
            new RegistroAspiranteInstitucion(institucionId, aspirante.Id),
            Cambio(
                institucionId,
                autor,
                aspirante.Id,
                "Aspirante.Registro",
                Mapear(aspirante)),
            tokenCancelacion);
        return new RegistrarAspiranteRespuesta(aspirante.Id);
    }

    // Implementa IModuloPersonasInscripciones.ConsultarAspiranteAsync.
    public async Task<ConsultarAspiranteRespuesta?> ConsultarAspiranteAsync(
        Guid institucionId,
        Guid aspiranteId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        var aspirante = await _personas.ObtenerAspiranteAsync(
            institucionId,
            aspiranteId,
            tokenCancelacion);
        return aspirante is null ? null : Mapear(aspirante);
    }

    // Implementa IModuloPersonasInscripciones.RegistrarResponsableAsync.
    public async Task<RegistrarResponsableRespuesta> RegistrarResponsableAsync(
        Guid institucionId,
        RegistrarResponsablePeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarAsync(
            institucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        var responsable = new Responsable(
            peticion.Nombre,
            peticion.Apellido,
            peticion.Documento,
            peticion.Telefono,
            peticion.CorreoElectronico,
            peticion.TipoDocumento);
        await _personas.AgregarResponsableAsync(
            responsable,
            new RegistroResponsableInstitucion(institucionId, responsable.Id),
            Cambio(
                institucionId,
                autor,
                responsable.Id,
                "Responsable.Registro",
                Mapear(responsable)),
            tokenCancelacion);
        return new RegistrarResponsableRespuesta(responsable.Id);
    }

    // Implementa IModuloPersonasInscripciones.ConsultarResponsableAsync.
    public async Task<ConsultarResponsableRespuesta?> ConsultarResponsableAsync(
        Guid institucionId,
        Guid responsableId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        var responsable = await _personas.ObtenerResponsableAsync(
            institucionId,
            responsableId,
            tokenCancelacion);
        return responsable is null ? null : Mapear(responsable);
    }

    // Implementa IModuloPersonasInscripciones.BuscarResponsablesAsync.
    public async Task<BuscarResponsablesRespuesta> BuscarResponsablesAsync(
        Guid institucionId,
        BuscarResponsablesPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.Pagina < 1 || peticion.TamanoPagina is < 1 or > 100 ||
            (long)(peticion.Pagina - 1) * peticion.TamanoPagina > int.MaxValue)
            throw new ArgumentException("La página debe ser positiva y su tamaño debe estar entre 1 y 100.");
        var documento = string.IsNullOrWhiteSpace(peticion.Documento) ? null : peticion.Documento.Trim();
        var responsables = await _personas.BuscarResponsablesAsync(
            institucionId,
            documento,
            (peticion.Pagina - 1) * peticion.TamanoPagina,
            peticion.TamanoPagina + 1,
            tokenCancelacion);
        return new BuscarResponsablesRespuesta(
            responsables.Take(peticion.TamanoPagina).Select(Mapear).ToArray(),
            peticion.Pagina,
            peticion.TamanoPagina,
            responsables.Count > peticion.TamanoPagina);
    }

    // Implementa IModuloPersonasInscripciones.VincularResponsableAsync.
    public async Task<VincularResponsableRespuesta> VincularResponsableAsync(
        Guid institucionId,
        Guid aspiranteId,
        VincularResponsablePeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarAsync(
            institucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        var aspirante = await _personas.ObtenerAspiranteAsync(
            institucionId,
            aspiranteId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró el aspirante en esta institución.");
        var responsable = await _personas.ObtenerResponsableAsync(
            institucionId,
            peticion.ResponsableId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró el responsable en esta institución.");
        var vinculo = aspirante.VincularResponsable(
            responsable,
            peticion.TipoVinculo,
            peticion.ContactoPrincipal,
            peticion.EsTutor);
        await _personas.AgregarVinculoAsync(
            vinculo,
            Cambio(
                institucionId,
                autor,
                aspirante.Id,
                "Responsable.Vinculacion",
                new { vinculo.AspiranteId, vinculo.ResponsableId, vinculo.TipoVinculo, vinculo.ContactoPrincipal, vinculo.EsTutor }),
            tokenCancelacion);
        return new VincularResponsableRespuesta(aspirante.Id, responsable.Id);
    }

    // Implementa IModuloPersonasInscripciones.ConsultarHistorialAsync.
    public async Task<IReadOnlyList<DatosCambioInscripciones>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        return (await _personas.ConsultarHistorialAsync(institucionId, tokenCancelacion))
            .Select(c => new DatosCambioInscripciones(
                c.Id,
                c.UsuarioId,
                c.EntidadId,
                c.Fecha,
                c.Accion,
                c.Datos)).ToArray();
    }

    private static ConsultarResponsableRespuesta Mapear(Responsable responsable) => new(
        responsable.Id,
        responsable.Nombre,
        responsable.Apellido,
        responsable.Telefono,
        responsable.Documento,
        responsable.TipoDocumento,
        responsable.CorreoElectronico);

    private static ConsultarAspiranteRespuesta Mapear(Aspirante aspirante) => new(
        aspirante.Id,
        aspirante.Nombre,
        aspirante.Apellido,
        aspirante.FechaNacimiento,
        aspirante.Dni,
        aspirante.TipoDocumento,
        aspirante.Sexo,
        aspirante.Vinculos.OrderBy(v => v.ResponsableId).Select(v => new DatosVinculoResponsable(
            Mapear(v.Responsable),
            v.TipoVinculo,
            v.ContactoPrincipal,
            v.EsTutor)).ToArray());

    private static CambioInscripciones Cambio(
        Guid institucionId,
        Guid autor,
        Guid entidadId,
        string accion,
        object datos) => new(
            institucionId,
            autor,
            entidadId,
            accion,
            JsonSerializer.Serialize(datos));
}
