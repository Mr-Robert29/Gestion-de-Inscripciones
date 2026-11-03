namespace Jardin.Secciones.Domain.Entidades;

public sealed class AsignacionPropuesta
{
    public Guid Id { get; private set; }
    public Guid PropuestaDistribucionId { get; private set; }
    public Guid IngresoId { get; private set; }
    public Guid? SeccionId { get; private set; }
    public int Edad { get; private set; }
    public Guid TurnoId { get; private set; }
    public string? MotivoSinSeccion { get; private set; }

    private AsignacionPropuesta() { }

    public AsignacionPropuesta(
        Guid ingresoId,
        int edad,
        Guid turnoId,
        Guid? seccionId,
        string? motivoSinSeccion,
        Guid? id = null)
    {
        if (ingresoId == Guid.Empty)
            throw new ArgumentException("El ingreso no puede estar vacío.", nameof(ingresoId));
        if (edad is < 3 or > 5)
            throw new ArgumentOutOfRangeException(nameof(edad), "La edad debe estar entre 3 y 5 años.");
        if (turnoId == Guid.Empty)
            throw new ArgumentException("El turno no puede estar vacío.", nameof(turnoId));
        if (seccionId is null && string.IsNullOrWhiteSpace(motivoSinSeccion))
            throw new ArgumentException("Una asignación sin sección debe informar el motivo.", nameof(motivoSinSeccion));
        if (seccionId is not null && !string.IsNullOrWhiteSpace(motivoSinSeccion))
            throw new ArgumentException("Una asignación con sección no debe informar un motivo de ausencia.", nameof(motivoSinSeccion));

        Id = id ?? Guid.NewGuid();
        IngresoId = ingresoId;
        Edad = edad;
        TurnoId = turnoId;
        SeccionId = seccionId;
        MotivoSinSeccion = string.IsNullOrWhiteSpace(motivoSinSeccion) ? null : motivoSinSeccion.Trim();
    }

    public void AsignarASeccion(Guid seccionId)
    {
        if (seccionId == Guid.Empty)
            throw new ArgumentException("La sección no puede estar vacía.", nameof(seccionId));

        SeccionId = seccionId;
        MotivoSinSeccion = null;
    }

    public void QuitarAsignacion(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        SeccionId = null;
        MotivoSinSeccion = motivo.Trim();
    }
}
