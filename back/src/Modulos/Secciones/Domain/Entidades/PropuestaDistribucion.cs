using Jardin.Secciones.Domain;

namespace Jardin.Secciones.Domain.Entidades;

public sealed class PropuestaDistribucion
{
    private readonly List<AsignacionPropuesta> _asignaciones = [];

    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public int Version { get; private set; }
    public int RevisionIngresos { get; private set; }
    public int RevisionSecciones { get; private set; }
    public EstadoPropuestaDistribucion Estado { get; private set; }
    public DateTimeOffset FechaCreacion { get; private set; }
    public Guid CreadaPor { get; private set; }
    public Guid? ConfirmadaPor { get; private set; }
    public DateTimeOffset? FechaConfirmacion { get; private set; }
    public IReadOnlyCollection<AsignacionPropuesta> Asignaciones => _asignaciones.AsReadOnly();

    private PropuestaDistribucion() { }

    public PropuestaDistribucion(
        Guid periodoId,
        int version,
        int revisionIngresos,
        int revisionSecciones,
        Guid creadaPor,
        DateTimeOffset fechaCreacion,
        Guid? id = null)
    {
        if (periodoId == Guid.Empty)
            throw new ArgumentException("El período no puede estar vacío.", nameof(periodoId));
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version));
        if (revisionIngresos < 0)
            throw new ArgumentOutOfRangeException(nameof(revisionIngresos));
        if (revisionSecciones < 0)
            throw new ArgumentOutOfRangeException(nameof(revisionSecciones));
        if (creadaPor == Guid.Empty)
            throw new ArgumentException("El usuario creador no puede estar vacío.", nameof(creadaPor));

        Id = id ?? Guid.NewGuid();
        PeriodoId = periodoId;
        Version = version;
        RevisionIngresos = revisionIngresos;
        RevisionSecciones = revisionSecciones;
        CreadaPor = creadaPor;
        FechaCreacion = fechaCreacion;
        Estado = EstadoPropuestaDistribucion.Borrador;
    }

    public void AgregarAsignacion(AsignacionPropuesta asignacion)
    {
        ArgumentNullException.ThrowIfNull(asignacion);
        if (_asignaciones.Any(x => x.IngresoId == asignacion.IngresoId))
            throw new InvalidOperationException("El ingreso ya tiene una asignación dentro de la propuesta.");

        _asignaciones.Add(asignacion);
    }

    public void ModificarAsignacion(
        Guid ingresoId,
        Guid? seccionId,
        string? motivoSinSeccion)
    {
        var asignacion = _asignaciones.FirstOrDefault(x => x.IngresoId == ingresoId)
            ?? throw new InvalidOperationException("El ingreso no forma parte de la propuesta.");

        if (seccionId is null)
            asignacion.QuitarAsignacion(motivoSinSeccion ?? string.Empty);
        else
            asignacion.AsignarASeccion(seccionId.Value);
    }

    public void MarcarDesactualizada()
    {
        if (Estado == EstadoPropuestaDistribucion.Confirmada)
            throw new InvalidOperationException("Una propuesta confirmada no puede quedar desactualizada.");

        Estado = EstadoPropuestaDistribucion.Desactualizada;
    }

    public void Confirmar(Guid usuarioId, DateTimeOffset fecha)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario confirmador no puede estar vacío.", nameof(usuarioId));
        if (Estado != EstadoPropuestaDistribucion.Borrador)
            throw new InvalidOperationException("Solo una propuesta vigente puede confirmarse.");

        Estado = EstadoPropuestaDistribucion.Confirmada;
        ConfirmadaPor = usuarioId;
        FechaConfirmacion = fecha;
    }
}
