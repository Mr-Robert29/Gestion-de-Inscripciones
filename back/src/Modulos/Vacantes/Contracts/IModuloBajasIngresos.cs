namespace Jardin.Vacantes.Contracts;

public interface IModuloBajasIngresos
{
    Task<RegistrarBajaIngresoRespuesta?> ConsultarAsync(Guid ingresoId, CancellationToken tokenCancelacion = default);
    Task<RegistrarBajaIngresoRespuesta> RegistrarAsync(
        Guid ingresoId,
        RegistrarBajaIngresoPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
public sealed record RegistrarBajaIngresoPeticion(int RevisionIngreso, string Motivo);
public sealed record RegistrarBajaIngresoRespuesta(
    Guid IngresoId,
    string Estado,
    bool PlazaLiberada,
    int RevisionIngreso,
    Guid? UsuarioId,
    DateTimeOffset? FechaSolicitud,
    DateTimeOffset? FechaBaja,
    string? Motivo);
