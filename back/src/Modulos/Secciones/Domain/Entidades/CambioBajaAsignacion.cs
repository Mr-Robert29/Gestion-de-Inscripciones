namespace Jardin.Secciones.Domain.Entidades;

// Preserva los datos anteriores a DarDeBaja, cuyo registro principal muestra el estado actual.
public sealed class CambioBajaAsignacion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AsignacionId { get; private set; }
    public Guid IngresoId { get; private set; }
    public Guid SeccionId { get; private set; }
    public DateTimeOffset FechaAnterior { get; private set; }
    public Guid UsuarioAnteriorId { get; private set; }
    public string? MotivoAnterior { get; private set; }
    public string EstadoAnterior { get; private set; } = "";
    public Guid UsuarioBajaId { get; private set; }
    public DateTimeOffset FechaBaja { get; private set; }
    public string MotivoBaja { get; private set; } = "";
    private CambioBajaAsignacion() { }
    public CambioBajaAsignacion(AsignacionSeccion asignacion, IngresoRetiradoSecciones retiro)
    {
        AsignacionId = asignacion.Id;
        IngresoId = asignacion.IngresoId;
        SeccionId = asignacion.SeccionId;
        FechaAnterior = asignacion.Fecha;
        UsuarioAnteriorId = asignacion.UsuarioId;
        MotivoAnterior = asignacion.Motivo;
        EstadoAnterior = asignacion.Estado.ToString();
        UsuarioBajaId = retiro.UsuarioId;
        FechaBaja = retiro.Fecha;
        MotivoBaja = retiro.Motivo;
    }
}
