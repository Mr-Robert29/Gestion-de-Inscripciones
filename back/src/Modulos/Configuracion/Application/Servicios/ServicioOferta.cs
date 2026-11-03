using System.Text.Json;
using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Servicios;

// Prepara recursos y oferta; no crea secciones ni calcula ingresos.
public sealed class ServicioOferta : IModuloOferta
{
    private readonly IRepositorioOferta _oferta;
    private readonly IRepositorioPeriodos _periodos;
    private readonly IRepositorioInstituciones _instituciones;
    private readonly IContextoUsuarioConfiguracion _usuario;

    public ServicioOferta(
        IRepositorioOferta oferta,
        IRepositorioPeriodos periodos,
        IRepositorioInstituciones instituciones,
        IContextoUsuarioConfiguracion usuario)
    {
        _oferta = oferta;
        _periodos = periodos;
        _instituciones = instituciones;
        _usuario = usuario;
    }

    // Implementa IModuloOferta.RegistrarAulaAsync.
    public async Task<RegistrarAulaRespuesta> RegistrarAulaAsync(
        Guid institucionId,
        RegistrarAulaPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await AutorizarAsync(
            institucionId,
            true,
            tokenCancelacion);
        await ComprobarInstitucionAsync(institucionId, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        var aula = new Aula(institucionId, peticion.Nombre);
        await _oferta.AgregarAulaAsync(
            aula,
            Cambio(
                institucionId,
                autor.Id,
                "Aula.Creacion",
                "Registro de aula",
                null,
                aula),
            tokenCancelacion);
        return new RegistrarAulaRespuesta(aula.Id);
    }

    // Implementa IModuloOferta.ListarAulasAsync.
    public async Task<IReadOnlyList<ConsultarAulaRespuesta>> ListarAulasAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        await ComprobarInstitucionAsync(institucionId, tokenCancelacion);
        return (await _oferta.ListarAulasAsync(institucionId, tokenCancelacion))
            .Select(a => new ConsultarAulaRespuesta(
                a.Id,
                a.InstitucionId,
                a.Nombre)).ToArray();
    }

    // Implementa IModuloOferta.RegistrarTurnoAsync.
    public async Task<RegistrarTurnoRespuesta> RegistrarTurnoAsync(
        Guid institucionId,
        RegistrarTurnoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await AutorizarAsync(
            institucionId,
            true,
            tokenCancelacion);
        await ComprobarInstitucionAsync(institucionId, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        var turno = new Turno(institucionId, peticion.Nombre);
        await _oferta.AgregarTurnoAsync(
            turno,
            Cambio(
                institucionId,
                autor.Id,
                "Turno.Creacion",
                "Registro de turno",
                null,
                turno),
            tokenCancelacion);
        return new RegistrarTurnoRespuesta(turno.Id);
    }

    // Implementa IModuloOferta.ListarTurnosAsync.
    public async Task<IReadOnlyList<ConsultarTurnoRespuesta>> ListarTurnosAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        await ComprobarInstitucionAsync(institucionId, tokenCancelacion);
        return (await _oferta.ListarTurnosAsync(institucionId, tokenCancelacion))
            .Select(t => new ConsultarTurnoRespuesta(
                t.Id,
                t.InstitucionId,
                t.Nombre)).ToArray();
    }

    // Implementa IModuloOferta.RegistrarOfertaAsync.
    public async Task<RegistrarOfertaRespuesta> RegistrarOfertaAsync(
        Guid periodoId,
        RegistrarOfertaPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        var autor = await AutorizarAsync(
            periodo.InstitucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var aula = await _oferta.ObtenerAulaAsync(peticion.AulaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el aula.");
        var turno = await _oferta.ObtenerTurnoAsync(peticion.TurnoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el turno.");
        if (aula.InstitucionId != periodo.InstitucionId || turno.InstitucionId != periodo.InstitucionId)
            throw new ArgumentException("El aula y el turno deben pertenecer a la institución del período.");
        if (await _oferta.ExisteCombinacionAsync(
            periodo.Id,
            aula.Id,
            turno.Id,
            tokenCancelacion))
            throw new InvalidOperationException("Ya existe una oferta para ese período, aula y turno.");
        var oferta = new OfertaAulaTurno(
            periodo.Id,
            aula.Id,
            turno.Id,
            peticion.Capacidad,
            peticion.Disponible,
            peticion.ReferenciaCapacidad,
            peticion.OrigenOcupaciones,
            CrearOcupaciones(peticion.Ocupaciones));
        await _oferta.AgregarOfertaAsync(
            oferta,
            Cambio(
                periodo.InstitucionId,
                autor.Id,
                "Oferta.Creacion",
                peticion.Motivo.Trim(),
                null,
                Mapear(oferta)),
            tokenCancelacion);
        return new RegistrarOfertaRespuesta(oferta.Id);
    }

    // Implementa IModuloOferta.ConsultarOfertaAsync.
    public async Task<ConsultarOfertaRespuesta?> ConsultarOfertaAsync(Guid ofertaId, CancellationToken tokenCancelacion = default)
    {
        var oferta = await _oferta.ObtenerOfertaAsync(ofertaId, tokenCancelacion);
        if (oferta is null)
        {
            if (await _usuario.ObtenerAsync(tokenCancelacion) is null) throw new UnauthorizedAccessException();
            return null;
        }
        var periodo = await ObtenerPeriodoAsync(oferta.PeriodoId, tokenCancelacion);
        await AutorizarAsync(
            periodo.InstitucionId,
            false,
            tokenCancelacion);
        return Mapear(oferta);
    }

    // Implementa IModuloOferta.ListarOfertasAsync.
    public async Task<IReadOnlyList<ConsultarOfertaRespuesta>> ListarOfertasAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        var periodo = await ObtenerPeriodoAsync(periodoId, tokenCancelacion);
        await AutorizarAsync(
            periodo.InstitucionId,
            false,
            tokenCancelacion);
        return (await _oferta.ListarOfertasAsync(periodoId, tokenCancelacion)).Select(Mapear).ToArray();
    }

    // Implementa IModuloOferta.ActualizarOfertaAsync.
    public async Task<ActualizarOfertaRespuesta> ActualizarOfertaAsync(
        Guid ofertaId,
        ActualizarOfertaPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var oferta = await _oferta.ObtenerOfertaAsync(ofertaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la oferta.");
        var periodo = await ObtenerPeriodoAsync(oferta.PeriodoId, tokenCancelacion);
        var autor = await AutorizarAsync(
            periodo.InstitucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (oferta.Revision != peticion.Revision) throw new ConflictoInstitucionException();
        var anteriores = JsonSerializer.Serialize(Mapear(oferta));
        oferta.Actualizar(
            peticion.Capacidad,
            peticion.Disponible,
            peticion.ReferenciaCapacidad,
            peticion.OrigenOcupaciones,
            CrearOcupaciones(peticion.Ocupaciones));
        await _oferta.ActualizarOfertaAsync(
            oferta,
            Cambio(
                periodo.InstitucionId,
                autor.Id,
                "Oferta.Actualizacion",
                peticion.Motivo.Trim(),
                anteriores,
                Mapear(oferta)),
            tokenCancelacion);
        return new ActualizarOfertaRespuesta(oferta.Id, oferta.Revision);
    }

    private async Task<UsuarioConfiguracion> AutorizarAsync(
        Guid institucionId,
        bool escritura,
        CancellationToken tokenCancelacion)
    {
        tokenCancelacion.ThrowIfCancellationRequested();
        var usuario = await _usuario.ObtenerAsync(tokenCancelacion)
            ?? throw new UnauthorizedAccessException("Se requiere una cuenta activa.");
        if (!usuario.EsRoot &&
            (!usuario.Accesos.TryGetValue(institucionId, out var puedeEditar) || (escritura && !puedeEditar)))
            throw new UnauthorizedAccessException("No tenés permiso para esta institución.");
        return usuario;
    }

    private async Task ComprobarInstitucionAsync(Guid institucionId, CancellationToken tokenCancelacion)
    {
        if (await _instituciones.ObtenerAsync(institucionId, tokenCancelacion) is null)
            throw new KeyNotFoundException("No se encontró la institución.");
    }

    private async Task<PeriodoInscripcion> ObtenerPeriodoAsync(Guid periodoId, CancellationToken tokenCancelacion)
    {
        return await _periodos.ObtenerAsync(periodoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el período.");
    }

    private static IReadOnlyList<OcupacionInicial> CrearOcupaciones(IReadOnlyList<DatosOcupacionInicial> datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (datos.Any(d => d is null)) throw new ArgumentException("La lista contiene una ocupación nula.");
        return datos.Select(d => new OcupacionInicial(d.Sala, d.Cantidad)).ToArray();
    }

    private static CambioInstitucion Cambio(
        Guid institucionId,
        Guid autor,
        string accion,
        string motivo,
        string? anteriores,
        object nuevos)
    {
        return new CambioInstitucion(
            institucionId,
            autor,
            accion,
            motivo,
            anteriores,
            JsonSerializer.Serialize(nuevos));
    }

    private static ConsultarOfertaRespuesta Mapear(OfertaAulaTurno oferta)
    {
        return new ConsultarOfertaRespuesta(
            oferta.Id,
            oferta.PeriodoId,
            oferta.AulaId,
            oferta.TurnoId,
            oferta.Capacidad,
            oferta.Disponible,
            oferta.ReferenciaCapacidad,
            oferta.OrigenOcupaciones,
            oferta.Ocupaciones.OrderBy(o => o.Sala).Select(o => new DatosOcupacionInicial(o.Sala, o.Cantidad)).ToArray(),
            oferta.CapacidadNetaInicial,
            oferta.Revision);
    }
}
