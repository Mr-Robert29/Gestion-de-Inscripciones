using System.Text.Json;
using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Servicios;

public sealed class ServicioVersionesConfiguracion : IModuloVersionesConfiguracion
{
    private readonly IRepositorioVersionesConfiguracion _versiones;
    private readonly IRepositorioPeriodos _periodos;
    private readonly IRepositorioOferta _oferta;
    private readonly IContextoUsuarioConfiguracion _usuario;

    public ServicioVersionesConfiguracion(
        IRepositorioVersionesConfiguracion versiones,
        IRepositorioPeriodos periodos,
        IRepositorioOferta oferta,
        IContextoUsuarioConfiguracion usuario)
    {
        _versiones = versiones;
        _periodos = periodos;
        _oferta = oferta;
        _usuario = usuario;
    }

    // Implementa IModuloVersionesConfiguracion.PublicarReglasAsync.
    public async Task<PublicarVersionRespuesta> PublicarReglasAsync(
        Guid periodoId,
        PublicarReglasPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        var autor = await AutorizarAsync(
            periodo.InstitucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.FechaCorte == default) throw new ArgumentException("La fecha de corte es obligatoria.");
        var reglas = CrearPerfilPrototipo(peticion.FechaCorte);
        var version = new VersionConfiguracion(
            periodo.Id,
            TipoVersionConfiguracion.Reglas,
            JsonSerializer.Serialize(reglas),
            autor.Id,
            peticion.Motivo,
            peticion.Referencia);
        await GuardarVersionAsync(
            periodo,
            version,
            tokenCancelacion);
        return new PublicarVersionRespuesta(version.Id);
    }

    // Implementa IModuloVersionesConfiguracion.PublicarOfertaAsync.
    public async Task<PublicarVersionRespuesta> PublicarOfertaAsync(
        Guid periodoId,
        PublicarOfertaPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        var autor = await AutorizarAsync(
            periodo.InstitucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        // Una consulta con sus ocupaciones obtiene una copia coherente de la oferta en PostgreSQL.
        var oferta = await _oferta.ListarOfertasAsync(periodoId, tokenCancelacion);
        if (oferta.Count == 0) throw new InvalidOperationException("El período no tiene oferta para publicar.");
        var instantanea = oferta.OrderBy(o => o.Id).Select(o => new ConsultarOfertaRespuesta(
            o.Id,
            o.PeriodoId,
            o.AulaId,
            o.TurnoId,
            o.Capacidad,
            o.Disponible,
            o.ReferenciaCapacidad,
            o.OrigenOcupaciones,
            o.Ocupaciones.OrderBy(c => c.Sala).Select(c => new DatosOcupacionInicial(c.Sala, c.Cantidad)).ToArray(),
            o.CapacidadNetaInicial,
            o.Revision)).ToArray();
        var version = new VersionConfiguracion(
            periodo.Id,
            TipoVersionConfiguracion.Oferta,
            JsonSerializer.Serialize(instantanea),
            autor.Id,
            peticion.Motivo,
            peticion.Referencia);
        await GuardarVersionAsync(
            periodo,
            version,
            tokenCancelacion);
        return new PublicarVersionRespuesta(version.Id);
    }

    // Implementa IModuloVersionesConfiguracion.ListarAsync.
    public async Task<IReadOnlyList<ConsultarVersionRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        await AutorizarAsync(
            periodo.InstitucionId,
            false,
            tokenCancelacion);
        return (await _versiones.ListarAsync(periodoId, tokenCancelacion)).Select(Mapear).ToArray();
    }

    // Implementa IModuloVersionesConfiguracion.ConsultarAsync.
    public async Task<ConsultarVersionRespuesta?> ConsultarAsync(Guid versionId, CancellationToken tokenCancelacion = default)
    {
        var version = await _versiones.ObtenerAsync(versionId, tokenCancelacion);
        if (version is null)
        {
            if (await _usuario.ObtenerAsync(tokenCancelacion) is null) throw new UnauthorizedAccessException();
            return null;
        }
        var periodo = await ObtenerPeriodoAsync(version.PeriodoId, tokenCancelacion);
        await AutorizarAsync(
            periodo.InstitucionId,
            false,
            tokenCancelacion);
        return Mapear(version);
    }

    // Implementa IModuloVersionesConfiguracion.AsignarAsync.
    public async Task<AsignarConfiguracionRespuesta> AsignarAsync(
        Guid periodoId,
        AsignarConfiguracionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        var autor = await AutorizarAsync(
            periodo.InstitucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentNullException.ThrowIfNull(peticion.Versiones);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (peticion.Revision != periodo.Revision) throw new ConflictoInstitucionException();
        var reglas = await _versiones.ObtenerAsync(peticion.Versiones.VersionReglasId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la versión de reglas.");
        var oferta = await _versiones.ObtenerAsync(peticion.Versiones.VersionOfertaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la versión de oferta.");
        var anteriores = JsonSerializer.Serialize(new { periodo.Id, periodo.VersionReglasId, periodo.VersionOfertaId, periodo.Revision });
        periodo.AsignarConfiguracion(reglas, oferta);
        var cambio = new CambioInstitucion(
            periodo.InstitucionId,
            autor.Id,
            "Configuracion.Asignacion",
            peticion.Motivo.Trim(),
            anteriores,
            JsonSerializer.Serialize(new { periodo.Id, periodo.VersionReglasId, periodo.VersionOfertaId, periodo.Revision }));
        await _periodos.ActualizarAsync(
            periodo,
            cambio,
            tokenCancelacion);
        return new AsignarConfiguracionRespuesta(
            periodo.Id,
            new DatosVersionesConfiguracion(reglas.Id, oferta.Id),
            periodo.Revision);
    }

    // Implementa IModuloVersionesConfiguracion.EstaVigenteAsync.
    public async Task<bool> EstaVigenteAsync(
        Guid periodoId,
        DatosVersionesConfiguracion versiones,
        CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        await AutorizarAsync(
            periodo.InstitucionId,
            false,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(versiones);
        return periodo.VersionReglasId == versiones.VersionReglasId && periodo.VersionOfertaId == versiones.VersionOfertaId;
    }

    private async Task GuardarVersionAsync(
        PeriodoInscripcion periodo,
        VersionConfiguracion version,
        CancellationToken tokenCancelacion)
    {
        var cambio = new CambioInstitucion(
            periodo.InstitucionId,
            version.UsuarioId,
            "Configuracion.Publicacion" + version.Tipo,
            version.Motivo,
            null,
            JsonSerializer.Serialize(Mapear(version)));
        await _versiones.AgregarAsync(
            version,
            cambio,
            tokenCancelacion);
    }

    private async Task<PeriodoInscripcion> ObtenerPeriodoAsync(Guid id, CancellationToken tokenCancelacion)
        => await _periodos.ObtenerAsync(id, tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró el período.");

    private async Task<UsuarioConfiguracion> AutorizarAsync(
        Guid institucionId,
        bool escritura,
        CancellationToken tokenCancelacion)
    {
        tokenCancelacion.ThrowIfCancellationRequested();
        var usuario = await _usuario.ObtenerAsync(tokenCancelacion) ?? throw new UnauthorizedAccessException();
        if (!usuario.EsRoot && (!usuario.Accesos.TryGetValue(institucionId, out var editar) || (escritura && !editar)))
            throw new UnauthorizedAccessException("No tenés permiso para esta institución.");
        return usuario;
    }

    private static ConsultarVersionRespuesta Mapear(VersionConfiguracion version)
    {
        using var datos = JsonDocument.Parse(version.Datos);
        return new ConsultarVersionRespuesta(
            version.Id,
            version.PeriodoId,
            version.Tipo.ToString(),
            version.Formato,
            version.UsuarioId,
            version.Fecha,
            version.Referencia,
            version.Motivo,
            datos.RootElement.Clone());
    }

    private static DatosReglasPeriodo CrearPerfilPrototipo(DateOnly fechaCorte)
    {
        // Perfil explícito y acotado. Ampliar el editor requiere acordar reglas, no recibir código ejecutable.
        return new DatosReglasPeriodo(
            "Prototipo-2026-1",
            fechaCorte,
            [5, 4, 3],
            ["ArticulacionPedagogica", "HermanosYGruposConvivientes", "Cercania", "HijosDelPersonal", "SorteoUltimaInstancia", "FueraDeTermino"],
            "Últimos tres dígitos de DNI: desde el número público hasta 999 y desde 000 al anterior; solo en última instancia. Terminaciones repetidas y casos sin DNI requieren revisión manual.",
            "Distancia directa en metros devuelta por Geoapify, sin redondear para comparar ni sustituir valores desconocidos por cero. Sin Haversine local.",
            [[3], [4], [5], [3, 4], [4, 5]],
            25);
    }
}
