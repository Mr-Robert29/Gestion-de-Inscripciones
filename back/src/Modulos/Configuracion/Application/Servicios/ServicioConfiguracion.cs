using System.Text.Json;
using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Domain.Entidades;
using Jardin.Configuracion.Domain.ObjetosDeValor;

namespace Jardin.Configuracion.Application.Servicios;

// Coordina HU-01, autoriza según el usuario vigente y conserva los cambios junto al guardado.
public sealed class ServicioConfiguracion : IModuloConfiguracion
{
    private readonly IRepositorioInstituciones _repositorio;
    private readonly IContextoUsuarioConfiguracion _contextoUsuario;

    public ServicioConfiguracion(IRepositorioInstituciones repositorio, IContextoUsuarioConfiguracion contextoUsuario)
    {
        ArgumentNullException.ThrowIfNull(repositorio);
        ArgumentNullException.ThrowIfNull(contextoUsuario);
        _repositorio = repositorio;
        _contextoUsuario = contextoUsuario;
    }

    // Implementa IModuloConfiguracion.RegistrarInstitucionAsync.
    public async Task<RegistrarInstitucionRespuesta> RegistrarInstitucionAsync(RegistrarInstitucionPeticion peticion, CancellationToken tokenCancelacion = default)
    {
        var usuario = await ObtenerUsuarioAsync(tokenCancelacion);
        if (!usuario.EsRoot && !usuario.PuedeRegistrar) throw new UnauthorizedAccessException("No tenés permiso para registrar instituciones.");
        ArgumentNullException.ThrowIfNull(peticion);
        var institucion = new Institucion(
            peticion.Nombre,
            CrearDomicilio(peticion.Domicilio),
            peticion.TelefonoContacto,
            peticion.Codigo,
            peticion.CorreoElectronicoContacto,
            CrearCoordenadas(peticion.Coordenadas));
        var cambio = new CambioInstitucion(
            institucion.Id,
            usuario.Id,
            "Registro",
            "Registro de la institución",
            null,
            JsonSerializer.Serialize(Mapear(institucion)));
        await _repositorio.AgregarAsync(institucion, cambio, tokenCancelacion);
        return new RegistrarInstitucionRespuesta(institucion.Id);
    }

    // Implementa IModuloConfiguracion.ConsultarInstitucionAsync.
    public async Task<ConsultarInstitucionRespuesta?> ConsultarInstitucionAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(institucionId, false, tokenCancelacion);
        var institucion = await _repositorio.ObtenerAsync(institucionId, tokenCancelacion);
        return institucion is null ? null : Mapear(institucion);
    }

    // Implementa IModuloConfiguracion.ListarInstitucionesAsync.
    public async Task<IReadOnlyList<ConsultarInstitucionRespuesta>> ListarInstitucionesAsync(CancellationToken tokenCancelacion = default)
    {
        var usuario = await ObtenerUsuarioAsync(tokenCancelacion);
        var instituciones = await _repositorio.ListarAsync(tokenCancelacion);
        return instituciones.Where(i => usuario.EsRoot || usuario.Accesos.ContainsKey(i.Id)).Select(Mapear).ToArray();
    }

    // Implementa IModuloConfiguracion.ActualizarInstitucionAsync.
    public async Task<ActualizarInstitucionRespuesta> ActualizarInstitucionAsync(
        Guid institucionId,
        ActualizarInstitucionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var usuario = await AutorizarAsync(institucionId, true, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var institucion = await ObtenerExistenteAsync(institucionId, tokenCancelacion);
        if (institucion.RevisionDatos != peticion.RevisionDatos) throw new ConflictoInstitucionException();
        var anteriores = JsonSerializer.Serialize(Mapear(institucion));
        institucion.ActualizarInformacion(
            peticion.Nombre,
            CrearDomicilio(peticion.Domicilio),
            peticion.TelefonoContacto,
            peticion.Codigo,
            peticion.CorreoElectronicoContacto,
            CrearCoordenadas(peticion.Coordenadas));
        var cambio = new CambioInstitucion(
            institucion.Id,
            usuario.Id,
            "Actualizacion",
            peticion.Motivo.Trim(),
            anteriores,
            JsonSerializer.Serialize(Mapear(institucion)));
        await _repositorio.ActualizarAsync(institucion, cambio, tokenCancelacion);
        return new ActualizarInstitucionRespuesta(
            institucion.Id,
            institucion.RevisionDatos,
            institucion.RevisionUbicacion);
    }

    // Implementa IModuloConfiguracion.ConsultarHistorialAsync.
    public async Task<IReadOnlyList<DatosCambioInstitucion>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(institucionId, false, tokenCancelacion);
        await ObtenerExistenteAsync(institucionId, tokenCancelacion);
        var cambios = await _repositorio.ConsultarHistorialAsync(institucionId, tokenCancelacion);
        return cambios.Select(c => new DatosCambioInstitucion(
            c.Id,
            c.UsuarioId,
            c.Fecha,
            c.Accion,
            c.Motivo,
            c.DatosAnteriores,
            c.DatosNuevos)).ToArray();
    }

    // Implementa IModuloConfiguracion.ConsultarImpactoUbicacionAsync.
    public async Task<ConsultarImpactoUbicacionRespuesta> ConsultarImpactoUbicacionAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(institucionId, false, tokenCancelacion);
        var institucion = await ObtenerExistenteAsync(institucionId, tokenCancelacion);
        var usos = await _repositorio.ConsultarUsosAsync(institucionId, tokenCancelacion);
        var afectados = usos.Where(u => institucion.Coordenadas is null || u.RevisionUbicacion != institucion.RevisionUbicacion)
            .Select(u => new DatosUsoUbicacion(
                u.ReferenciaId,
                u.Tipo,
                u.RevisionUbicacion)).ToArray();
        return new ConsultarImpactoUbicacionRespuesta(
            institucion.Id,
            institucion.RevisionUbicacion,
            institucion.Coordenadas is null,
            afectados);
    }

    // Implementa IModuloConfiguracion.RegistrarUsoUbicacionAsync; se utiliza desde otros módulos, no desde HTTP.
    public async Task RegistrarUsoUbicacionAsync(
        Guid institucionId,
        DatosUsoUbicacion uso,
        CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(institucionId, false, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(uso);
        var institucion = await ObtenerExistenteAsync(institucionId, tokenCancelacion);
        if (institucion.Coordenadas is null || institucion.RevisionUbicacion != uso.RevisionUbicacion)
            throw new ConflictoInstitucionException();
        await _repositorio.RegistrarUsoAsync(new UsoUbicacion(
            institucionId,
            uso.ReferenciaId,
            uso.Tipo,
            uso.RevisionUbicacion), tokenCancelacion);
    }

    // Implementa IModuloConfiguracion.ComprobarVigenciaUsoAsync; una propuesta obsoleta debe rechazarse al confirmar.
    public async Task<bool> ComprobarVigenciaUsoAsync(
        Guid institucionId,
        DatosUsoUbicacion uso,
        CancellationToken tokenCancelacion = default)
    {
        await AutorizarAsync(institucionId, false, tokenCancelacion);
        ArgumentNullException.ThrowIfNull(uso);
        var institucion = await ObtenerExistenteAsync(institucionId, tokenCancelacion);
        var usos = await _repositorio.ConsultarUsosAsync(institucionId, tokenCancelacion);
        return institucion.Coordenadas is not null && institucion.RevisionUbicacion == uso.RevisionUbicacion
            && usos.Any(u => u.ReferenciaId == uso.ReferenciaId && u.Tipo == uso.Tipo && u.RevisionUbicacion == uso.RevisionUbicacion);
    }

    private async Task<UsuarioConfiguracion> ObtenerUsuarioAsync(CancellationToken tokenCancelacion)
    {
        tokenCancelacion.ThrowIfCancellationRequested();
        var usuario = await _contextoUsuario.ObtenerAsync(tokenCancelacion);
        return usuario ?? throw new UnauthorizedAccessException("Se requiere una cuenta activa.");
    }

    private async Task<UsuarioConfiguracion> AutorizarAsync(
        Guid institucionId,
        bool escritura,
        CancellationToken tokenCancelacion)
    {
        var usuario = await ObtenerUsuarioAsync(tokenCancelacion);
        if (usuario.EsRoot) return usuario;
        if (!usuario.Accesos.TryGetValue(institucionId, out var puedeEditar) || (escritura && !puedeEditar))
            throw new UnauthorizedAccessException("No tenés permiso para esta institución.");
        return usuario;
    }

    private async Task<Institucion> ObtenerExistenteAsync(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return await _repositorio.ObtenerAsync(institucionId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la institución.");
    }

    private static Domicilio CrearDomicilio(DatosDomicilio datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return new Domicilio(
            datos.Calle,
            datos.Numero,
            datos.Localidad,
            datos.Provincia,
            datos.CodigoPostal,
            datos.Complemento,
            datos.Piso,
            datos.Departamento,
            datos.Barrio);
    }

    private static Coordenadas? CrearCoordenadas(DatosCoordenadas? datos)
    {
        return datos is null ? null : new Coordenadas(datos.Latitud, datos.Longitud);
    }

    private static ConsultarInstitucionRespuesta Mapear(Institucion institucion)
    {
        var d = institucion.Domicilio;
        return new ConsultarInstitucionRespuesta(
            institucion.Id,
            institucion.Nombre,
            new DatosDomicilio(
                d.Calle,
                d.Numero,
                d.Localidad,
                d.Provincia,
                d.CodigoPostal,
                d.Complemento,
                d.Piso,
                d.Departamento,
                d.Barrio),
            institucion.TelefonoContacto,
            institucion.Codigo,
            institucion.CorreoElectronicoContacto,
            institucion.Coordenadas is null ? null : new DatosCoordenadas(institucion.Coordenadas.Latitud, institucion.Coordenadas.Longitud),
            institucion.Activa,
            institucion.RevisionUbicacion,
            institucion.RevisionDatos);
    }
}
