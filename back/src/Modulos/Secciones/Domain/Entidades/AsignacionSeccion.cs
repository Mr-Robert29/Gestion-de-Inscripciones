namespace Jardin.Secciones.Domain.Entidades;

public sealed class AsignacionSeccion
{
    public Guid Id { get; private set; }
    public Guid IngresoId { get; private set; }
    public Guid SeccionId { get; private set; }
    public EstadoAsignacionSeccion Estado { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string? Motivo { get; private set; }

    private AsignacionSeccion() { }

    public AsignacionSeccion(
        Guid ingresoId,
        Guid seccionId,
        Guid usuarioId,
        DateTimeOffset fecha,
        string? motivo = null,
        Guid? id = null)
    {
        if (ingresoId == Guid.Empty)
            throw new ArgumentException("El ingreso no puede estar vacío.", nameof(ingresoId));
        if (seccionId == Guid.Empty)
            throw new ArgumentException("La sección no puede estar vacía.", nameof(seccionId));
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario no puede estar vacío.", nameof(usuarioId));

        Id = id ?? Guid.NewGuid();
        IngresoId = ingresoId;
        SeccionId = seccionId;
        UsuarioId = usuarioId;
        Fecha = fecha;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        Estado = EstadoAsignacionSeccion.Activa;
    }

    public void MoverASeccion(Guid nuevaSeccionId, Guid usuarioId, DateTimeOffset fecha, string motivo)
    {
        if (nuevaSeccionId == Guid.Empty)
            throw new ArgumentException("La nueva sección no puede estar vacía.", nameof(nuevaSeccionId));
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario no puede estar vacío.", nameof(usuarioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        SeccionId = nuevaSeccionId;
        UsuarioId = usuarioId;
        Fecha = fecha;
        Motivo = motivo.Trim();
    }

    public void DarDeBaja(Guid usuarioId, DateTimeOffset fecha, string motivo)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario no puede estar vacío.", nameof(usuarioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        Estado = EstadoAsignacionSeccion.Baja;
        UsuarioId = usuarioId;
        Fecha = fecha;
        Motivo = motivo.Trim();
    }
}
