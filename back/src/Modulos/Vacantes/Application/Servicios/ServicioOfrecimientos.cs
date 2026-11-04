using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioOfrecimientos : IModuloOfrecimientos
{
    private readonly IRepositorioOfrecimientos _repositorio;
    private readonly IRepositorioPropuestasIngresos _propuestas;
    private readonly IModuloListaEspera _espera;
    private readonly IContextoPropuestaIngresos _contexto;
    private readonly IIntegracionSeccionesEspera _secciones;
    private readonly IAutorizacionOfrecimientos _autorizacion;
    private readonly IAutorizacionListaEspera _consulta;
    private readonly EjecucionEspera _ejecucion;
    public ServicioOfrecimientos(
        IRepositorioOfrecimientos repositorio,
        IRepositorioPropuestasIngresos propuestas,
        IModuloListaEspera espera,
        IContextoPropuestaIngresos contexto,
        IIntegracionSeccionesEspera secciones,
        IAutorizacionOfrecimientos autorizacion,
        IAutorizacionListaEspera consulta,
        EjecucionEspera ejecucion)
    {
        _repositorio = repositorio;
        _propuestas = propuestas;
        _espera = espera;
        _contexto = contexto;
        _secciones = secciones;
        _autorizacion = autorizacion;
        _consulta = consulta;
        _ejecucion = ejecucion;
    }

    // Implementa IModuloOfrecimientos.ConsultarCandidatosAsync: muestra compatibles en el orden de la lista global.
    public async Task<ConsultarCandidatosVacanteRespuesta> ConsultarCandidatosAsync(
        Guid periodoId,
        Guid ofertaId,
        CancellationToken tokenCancelacion = default)
    {
        await _consulta.AutorizarConsultaEsperaAsync(periodoId, tokenCancelacion);
        return await ConOfertaAsync(
            periodoId,
            ofertaId,
            async (datos, oferta, secciones, token) =>
            {
                var lista = await LeerEsperaAsync(datos.InstitucionId, periodoId, token);
                var ocupaciones = await _propuestas.ListarOcupacionesAsync(periodoId, token);
                var solicitudes = datos.Solicitudes.ToDictionary(x => x.Id);
                var compatibles = lista.Integrantes.Where(x => x.Estado == "EnEspera" && x.DatosCoincidenConResultado &&
                    solicitudes.TryGetValue(x.SolicitudId, out var solicitud) && CompatibilidadVacante.EsCompatible(
                        datos, oferta, solicitud, ocupaciones, secciones)).ToArray();
                return new ConsultarCandidatosVacanteRespuesta(periodoId, ofertaId, lista.PropuestaId, compatibles);
            },
            tokenCancelacion);
    }

    // Implementa IModuloOfrecimientos.OfrecerAsync: el operador elige un candidato compatible y registra su justificación.
    public async Task<ConsultarOfrecimientoRespuesta> OfrecerAsync(
        Guid periodoId,
        OfrecerVacantePeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.OperacionId == Guid.Empty) throw new ArgumentException("Indicá un identificador de operación para los reintentos.");
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var autor = await _autorizacion.AutorizarOfertaAsync(periodoId, tokenCancelacion);
        return await _ejecucion.EjecutarAsync(
            periodoId,
            async token =>
            {
                var existente = await _repositorio.BuscarOperacionAsync(periodoId, peticion.OperacionId, token);
                if (existente is not null)
                {
                    if (existente.SolicitudId != peticion.SolicitudId || existente.OfertaId != peticion.OfertaId ||
                        existente.PropuestaListaId != peticion.PropuestaListaId || existente.Motivo != peticion.Motivo.Trim())
                        throw new InvalidOperationException("El identificador de operación ya se usó con otros datos.");
                    return await MapearAsync(existente, token);
                }
                return await ConOfertaAsync(
                    periodoId,
                    peticion.OfertaId,
                    async (datos, oferta, secciones, protegido) =>
                    {
                        var solicitud = await ExigirCandidatoAsync(datos, peticion.SolicitudId, peticion.PropuestaListaId, "EnEspera", protegido);
                        var ocupaciones = await _propuestas.ListarOcupacionesAsync(periodoId, protegido);
                        if (!CompatibilidadVacante.EsCompatible(datos, oferta, solicitud, ocupaciones, secciones))
                            throw new InvalidOperationException("Ya no hay una plaza compatible disponible para ese candidato.");
                        var reserva = new OcupacionPlaza(periodoId, solicitud.AspiranteId, oferta.Id, solicitud.Sala!.Value, "Reserva");
                        var ofrecimiento = new OfrecimientoVacante(
                            peticion.OperacionId,
                            periodoId,
                            autor.InstitucionId,
                            solicitud.Id,
                            solicitud.AspiranteId,
                            oferta.Id,
                            oferta.TurnoId,
                            peticion.PropuestaListaId,
                            reserva.Id,
                            autor.UsuarioId,
                            peticion.Motivo);
                        await _repositorio.CrearAsync(ofrecimiento, reserva, protegido);
                        return await MapearAsync(ofrecimiento, protegido);
                    },
                    token);
            },
            tokenCancelacion);
    }

    // Implementa IModuloOfrecimientos.ConsultarAsync con autorización institucional.
    public async Task<ConsultarOfrecimientoRespuesta?> ConsultarAsync(Guid ofrecimientoId, CancellationToken tokenCancelacion = default)
    {
        var ofrecimiento = await _repositorio.ObtenerAsync(ofrecimientoId, tokenCancelacion);
        if (ofrecimiento is null) return null;
        await _consulta.AutorizarConsultaEsperaAsync(ofrecimiento.PeriodoId, tokenCancelacion);
        return await MapearAsync(ofrecimiento, tokenCancelacion);
    }

    // Implementa IModuloOfrecimientos.ListarAsync.
    public async Task<IReadOnlyList<ConsultarOfrecimientoRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        await _consulta.AutorizarConsultaEsperaAsync(periodoId, tokenCancelacion);
        var resultados = new List<ConsultarOfrecimientoRespuesta>();
        foreach (var ofrecimiento in await _repositorio.ListarAsync(periodoId, tokenCancelacion))
            resultados.Add(await MapearAsync(ofrecimiento, tokenCancelacion));
        return resultados;
    }

    // Implementa IModuloOfrecimientos.ResponderAsync: conserva aceptación o libera por rechazo/cancelación, sin retirar de espera.
    public async Task<ConsultarOfrecimientoRespuesta> ResponderAsync(
        Guid ofrecimientoId,
        ResponderOfrecimientoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.Revision < 1) throw new ArgumentException("Indicá la revisión vigente del ofrecimiento.");
        var referencia = await ExigirOfrecimientoAsync(ofrecimientoId, tokenCancelacion);
        var autor = await _autorizacion.AutorizarRespuestaAsync(referencia.PeriodoId, tokenCancelacion);
        return await _ejecucion.EjecutarAsync(
            referencia.PeriodoId,
            async token =>
            {
                var ofrecimiento = await ExigirOfrecimientoAsync(ofrecimientoId, token);
                var estadoAnterior = ofrecimiento.Estado;
                var revision = ofrecimiento.Revision;
                ofrecimiento.Responder(peticion.Respuesta, peticion.Revision, autor.UsuarioId, peticion.Motivo, peticion.Referencia);
                if (revision != ofrecimiento.Revision)
                    await _repositorio.GuardarRespuestaAsync(
                        ofrecimiento,
                        new CambioOfrecimiento(ofrecimiento.Id, autor.UsuarioId, estadoAnterior, ofrecimiento.Estado, peticion.Motivo.Trim(), peticion.Referencia?.Trim()),
                        token);
                return await MapearAsync(ofrecimiento, token);
            },
            tokenCancelacion);
    }

    // Implementa IModuloOfrecimientos.ConfirmarAsync: revalida la espera y convierte una única reserva en ingreso.
    public async Task<ConsultarOfrecimientoRespuesta> ConfirmarAsync(
        Guid ofrecimientoId,
        ConfirmarOfrecimientoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (peticion.Revision < 1) throw new ArgumentException("Indicá la revisión vigente del ofrecimiento.");
        var referencia = await ExigirOfrecimientoAsync(ofrecimientoId, tokenCancelacion);
        var autor = await _autorizacion.AutorizarConfirmacionEsperaAsync(referencia.PeriodoId, tokenCancelacion);
        return await _ejecucion.EjecutarAsync(
            referencia.PeriodoId,
            async token =>
            {
                var ofrecimiento = await ExigirOfrecimientoAsync(ofrecimientoId, token);
                if (ofrecimiento.Estado == "Confirmado")
                {
                    var confirmacion = (await _repositorio.HistorialAsync(ofrecimiento.Id, token))
                        .Single(x => x.EstadoNuevo == "Confirmado");
                    if (confirmacion.Motivo != peticion.Motivo.Trim())
                        throw new InvalidOperationException("El ofrecimiento ya se confirmó con otro motivo.");
                    return await MapearAsync(ofrecimiento, token);
                }
                return await ConOfertaAsync(
                    ofrecimiento.PeriodoId,
                    ofrecimiento.OfertaId,
                    async (datos, oferta, secciones, protegido) =>
                    {
                        if (ofrecimiento.Estado != "Aceptado") throw new InvalidOperationException("Falta registrar aceptación familiar.");
                        var solicitud = await ExigirCandidatoAsync(datos, ofrecimiento.SolicitudId, ofrecimiento.PropuestaListaId, "ConReserva", protegido);
                        var ocupaciones = await _propuestas.ListarOcupacionesAsync(datos.PeriodoId, protegido);
                        if (!CompatibilidadVacante.EsCompatible(datos, oferta, solicitud, ocupaciones, secciones, ofrecimiento.ReservaId))
                            throw new InvalidOperationException("La plaza o composición ya no es compatible. Revisá el ofrecimiento.");
                        var ingreso = new IngresoConfirmado(
                            datos.PeriodoId,
                            solicitud.Id,
                            solicitud.AspiranteId,
                            solicitud.Edad,
                            oferta.Id,
                            oferta.TurnoId,
                            solicitud.Sala!.Value,
                            ofrecimiento.PropuestaListaId,
                            autor.UsuarioId,
                            ofrecimiento.Id);
                        ofrecimiento.Confirmar(ingreso.Id, peticion.Revision);
                        await _repositorio.ConfirmarAsync(
                            ofrecimiento,
                            ingreso,
                            new CambioOfrecimiento(ofrecimiento.Id, autor.UsuarioId, "Aceptado", "Confirmado", peticion.Motivo.Trim()),
                            protegido);
                        return await MapearAsync(ofrecimiento, protegido);
                    },
                    token);
            },
            tokenCancelacion);
    }

    private async Task<SolicitudPropuesta> ExigirCandidatoAsync(
        EntradasPropuestaIngresos datos,
        Guid solicitudId,
        Guid propuestaListaId,
        string estado,
        CancellationToken tokenCancelacion)
    {
        var lista = await LeerEsperaAsync(datos.InstitucionId, datos.PeriodoId, tokenCancelacion);
        var candidato = lista.Integrantes.SingleOrDefault(x => x.SolicitudId == solicitudId);
        if (lista.PropuestaId != propuestaListaId || candidato is null || candidato.Estado != estado || !candidato.DatosCoincidenConResultado)
            throw new InvalidOperationException("El candidato ya no está en la misma espera elegible. Volvé a consultar.");
        return datos.Solicitudes.Single(x => x.Id == solicitudId);
    }

    private async Task<(Guid? PropuestaId, IReadOnlyList<DatosIntegranteListaEspera> Integrantes)> LeerEsperaAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        Guid? propuesta = null;
        var integrantes = new List<DatosIntegranteListaEspera>();
        for (var pagina = 1; ; pagina++)
        {
            var respuesta = await _espera.ConsultarAsync(institucionId, periodoId,
                new ConsultarListaEsperaPeticion(Pagina: pagina, TamanoPagina: 100), tokenCancelacion);
            if (pagina > 1 && propuesta != respuesta.PropuestaConfirmadaId)
                throw new InvalidOperationException("La lista cambió durante la consulta.");
            propuesta = respuesta.PropuestaConfirmadaId;
            integrantes.AddRange(respuesta.EnEspera.Resultados);
            if (!respuesta.EnEspera.HayMas) break;
        }
        return (propuesta, integrantes);
    }

    private async Task<T> ConOfertaAsync<T>(
        Guid periodoId,
        Guid ofertaId,
        Func<EntradasPropuestaIngresos, OfertaPropuesta, IReadOnlyList<ComposicionVacante>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion)
    {
        var datos = await _contexto.LeerAsync(periodoId, tokenCancelacion);
        if (datos.EstadoPeriodo != "RecepcionCerrada") throw new InvalidOperationException("La recepción debe estar cerrada.");
        var oferta = datos.Ofertas.SingleOrDefault(x => x.Id == ofertaId)
            ?? throw new KeyNotFoundException("No se encontró la oferta publicada para el período.");
        return await _secciones.ConOfertaProtegidaAsync(
            periodoId,
            oferta.AulaId,
            oferta.TurnoId,
            (secciones, token) => operacion(datos, oferta, secciones, token),
            tokenCancelacion);
    }

    private async Task<OfrecimientoVacante> ExigirOfrecimientoAsync(Guid id, CancellationToken tokenCancelacion)
        => await _repositorio.ObtenerAsync(id, tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró el ofrecimiento.");

    private async Task<ConsultarOfrecimientoRespuesta> MapearAsync(OfrecimientoVacante ofrecimiento, CancellationToken tokenCancelacion)
        => new(
            ofrecimiento.Id,
            ofrecimiento.OperacionId,
            ofrecimiento.PeriodoId,
            ofrecimiento.InstitucionId,
            ofrecimiento.SolicitudId,
            ofrecimiento.AspiranteId,
            ofrecimiento.OfertaId,
            ofrecimiento.TurnoId,
            ofrecimiento.PropuestaListaId,
            ofrecimiento.ReservaId,
            ofrecimiento.Estado,
            ofrecimiento.Revision,
            ofrecimiento.UsuarioId,
            ofrecimiento.Fecha,
            ofrecimiento.Motivo,
            ofrecimiento.ReferenciaRespuesta,
            ofrecimiento.IngresoId,
            (await _repositorio.HistorialAsync(ofrecimiento.Id, tokenCancelacion)).Select(x =>
                new DatosCambioOfrecimiento(x.UsuarioId, x.Fecha, x.EstadoAnterior, x.EstadoNuevo, x.Motivo, x.Referencia)).ToArray());
}
