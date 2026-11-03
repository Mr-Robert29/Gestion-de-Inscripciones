using System.Text.Json;
using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Servicios;

// Separado del servicio de instituciones para mantener cada caso de uso fácil de localizar.
public sealed class ServicioPeriodos : IModuloPeriodos
{
    private readonly IRepositorioPeriodos _periodos;
    private readonly IRepositorioInstituciones _instituciones;
    private readonly IContextoUsuarioConfiguracion _usuario;

    public ServicioPeriodos(
        IRepositorioPeriodos periodos,
        IRepositorioInstituciones instituciones,
        IContextoUsuarioConfiguracion usuario)
    {
        _periodos = periodos;
        _instituciones = instituciones;
        _usuario = usuario;
    }

    // Implementa IModuloPeriodos.CrearPeriodoAsync.
    public async Task<CrearPeriodoRespuesta> CrearPeriodoAsync(
        Guid institucionId,
        CrearPeriodoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await AutorizarAsync(institucionId, true, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (await _instituciones.ObtenerAsync(institucionId, tokenCancelacion) is null)
            throw new KeyNotFoundException("No se encontró la institución.");
        var periodo = new PeriodoInscripcion(
            institucionId,
            peticion.Ciclo,
            peticion.Inicio,
            peticion.Fin);
        await _periodos.AgregarAsync(
            periodo,
            CrearCambio(periodo, autor.Id, "Periodo.Creacion", "Preparación del período", null),
            tokenCancelacion);
        return new CrearPeriodoRespuesta(periodo.Id);
    }

    // Implementa IModuloPeriodos.ConsultarPeriodoAsync.
    public async Task<ConsultarPeriodoRespuesta?> ConsultarPeriodoAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        var periodo = await _periodos.ObtenerAsync(periodoId, tokenCancelacion);
        if (periodo is null)
        {
            if (await _usuario.ObtenerAsync(tokenCancelacion) is null) throw new UnauthorizedAccessException();
            return null;
        }
        await AutorizarAsync(periodo.InstitucionId, false, tokenCancelacion);
        return Mapear(periodo);
    }

    // Implementa IModuloPeriodos.ListarPeriodosAsync.
    public async Task<IReadOnlyList<ConsultarPeriodoRespuesta>> ListarPeriodosAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(institucionId, false, tokenCancelacion);
        if (await _instituciones.ObtenerAsync(institucionId, tokenCancelacion) is null)
            throw new KeyNotFoundException("No se encontró la institución.");
        var periodos = await _periodos.ListarAsync(institucionId, tokenCancelacion);
        return periodos.Select(Mapear).ToArray();
    }

    // Implementa IModuloPeriodos.AbrirRecepcionAsync.
    public Task<CambiarRecepcionRespuesta> AbrirRecepcionAsync(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        return CambiarRecepcionAsync(
            periodoId,
            peticion,
            true,
            tokenCancelacion);
    }

    // Implementa IModuloPeriodos.CerrarRecepcionAsync.
    public Task<CambiarRecepcionRespuesta> CerrarRecepcionAsync(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        return CambiarRecepcionAsync(
            periodoId,
            peticion,
            false,
            tokenCancelacion);
    }

    private async Task<CambiarRecepcionRespuesta> CambiarRecepcionAsync(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        bool abrir,
        CancellationToken tokenCancelacion)
    {
        var periodo = await _periodos.ObtenerAsync(periodoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el período.");
        var autor = await AutorizarAsync(periodo.InstitucionId, true, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (periodo.Revision != peticion.Revision) throw new ConflictoInstitucionException();
        var anteriores = JsonSerializer.Serialize(Mapear(periodo));
        if (abrir) periodo.Abrir();
        else periodo.CerrarRecepcion();
        await _periodos.ActualizarAsync(
            periodo,
            CrearCambio(periodo, autor.Id, abrir ? "Periodo.Apertura" : "Periodo.CierreRecepcion", peticion.Motivo.Trim(), anteriores),
            tokenCancelacion);
        return new CambiarRecepcionRespuesta(
            periodo.Id,
            periodo.Estado.ToString(),
            periodo.Revision);
    }

    private async Task<UsuarioConfiguracion> AutorizarAsync(
        Guid institucionId,
        bool escritura,
        CancellationToken tokenCancelacion)
    {
        var usuario = await _usuario.ObtenerAsync(tokenCancelacion)
            ?? throw new UnauthorizedAccessException("Se requiere una cuenta activa.");
        if (!usuario.EsRoot &&
            (!usuario.Accesos.TryGetValue(institucionId, out var puedeEditar) || (escritura && !puedeEditar)))
            throw new UnauthorizedAccessException("No tenés permiso para esta institución.");
        return usuario;
    }

    private static CambioInstitucion CrearCambio(
        PeriodoInscripcion periodo,
        Guid autor,
        string accion,
        string motivo,
        string? anteriores)
    {
        return new CambioInstitucion(
            periodo.InstitucionId,
            autor,
            accion,
            motivo,
            anteriores,
            JsonSerializer.Serialize(Mapear(periodo)));
    }

    private static ConsultarPeriodoRespuesta Mapear(PeriodoInscripcion periodo)
    {
        return new ConsultarPeriodoRespuesta(
            periodo.Id,
            periodo.InstitucionId,
            periodo.Ciclo,
            periodo.Inicio,
            periodo.Fin,
            periodo.Estado.ToString(),
            periodo.Revision,
            periodo.VersionReglasId,
            periodo.VersionOfertaId);
    }
}
