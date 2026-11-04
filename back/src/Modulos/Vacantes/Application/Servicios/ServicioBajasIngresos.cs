using System.Data.Common;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioBajasIngresos : IModuloBajasIngresos
{
    private readonly IRepositorioOfrecimientos _repositorio;
    private readonly IAutorizacionOfrecimientos _autorizacion;
    private readonly IIntegracionSeccionesEspera _secciones;
    private readonly EjecucionEspera _ejecucion;
    private readonly IAutorizacionListaEspera _consulta;
    public ServicioBajasIngresos(
        IRepositorioOfrecimientos repositorio,
        IAutorizacionOfrecimientos autorizacion,
        IIntegracionSeccionesEspera secciones,
        EjecucionEspera ejecucion,
        IAutorizacionListaEspera consulta)
    {
        _repositorio = repositorio;
        _autorizacion = autorizacion;
        _secciones = secciones;
        _ejecucion = ejecucion;
        _consulta = consulta;
    }

    // Implementa IModuloBajasIngresos.ConsultarAsync: permite consultar la coordinación sin volver a ejecutarla.
    public async Task<RegistrarBajaIngresoRespuesta?> ConsultarAsync(Guid ingresoId, CancellationToken tokenCancelacion = default)
    {
        var ingreso = await _repositorio.ObtenerIngresoAsync(ingresoId, tokenCancelacion);
        if (ingreso is null) return null;
        await _consulta.AutorizarConsultaEsperaAsync(ingreso.PeriodoId, tokenCancelacion);
        return Mapear(ingreso);
    }

    // Implementa IModuloBajasIngresos.RegistrarAsync: la coordinación pendiente conserva ocupada la plaza.
    public async Task<RegistrarBajaIngresoRespuesta> RegistrarAsync(
        Guid ingresoId,
        RegistrarBajaIngresoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (peticion.RevisionIngreso < 1) throw new ArgumentException("Indicá la revisión vigente del ingreso.");
        var referencia = await _repositorio.ObtenerIngresoAsync(ingresoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el ingreso.");
        var autor = await _autorizacion.AutorizarBajaAsync(referencia.PeriodoId, tokenCancelacion);
        return await _ejecucion.EjecutarAsync(
            referencia.PeriodoId,
            async token =>
            {
                var ingreso = (await _repositorio.ObtenerIngresoAsync(ingresoId, token))!;
                if (ingreso.BajaEnProceso || !ingreso.Activo)
                {
                    if (ingreso.MotivoBaja != peticion.Motivo.Trim()) throw new InvalidOperationException("Ya existe una baja con otro motivo.");
                    if (!ingreso.Activo) return Mapear(ingreso);
                }
                else
                {
                    if (ingreso.Revision != peticion.RevisionIngreso) throw new InvalidOperationException("El ingreso cambió. Volvé a consultarlo.");
                    ingreso.SolicitarBaja(autor.UsuarioId, peticion.Motivo);
                    await _repositorio.GuardarSolicitudBajaAsync(ingreso, token);
                }
                try
                {
                    await _secciones.DarBajaAsignacionesAsync(
                        ingreso.PeriodoId,
                        ingreso.Id,
                        ingreso.UsuarioBajaId!.Value,
                        ingreso.MotivoBaja!,
                        token);
                }
                catch (Exception error) when (error is InvalidOperationException or DbException)
                {
                    // El paso durable permite reintentar la misma solicitud sin liberar una plaza inconsistente.
                    return Mapear(ingreso);
                }
                return Mapear(await _repositorio.CompletarBajaAsync(ingreso.Id, token));
            },
            tokenCancelacion);
    }

    private static RegistrarBajaIngresoRespuesta Mapear(IngresoConfirmado ingreso)
        => new(
            ingreso.Id,
            ingreso.Activo ? ingreso.BajaEnProceso ? "PendienteCoordinacion" : "SinBaja" : "Completada",
            !ingreso.Activo,
            ingreso.Revision,
            ingreso.UsuarioBajaId,
            ingreso.FechaSolicitudBaja,
            ingreso.FechaBaja,
            ingreso.MotivoBaja);
}
