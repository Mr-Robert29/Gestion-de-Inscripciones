using System.Text.Json;

namespace Jardin.Vacantes.Domain.Entidades;

public enum TipoCasoRevision
{
    EmpateTerminacionDni,
    SinDni,
    EmpateDistancia
}

public enum EstadoCasoRevision
{
    Pendiente,
    Resuelto,
    Reabierto
}

public sealed class CasoRevision
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public int Sala { get; private set; }
    public TipoCasoRevision Tipo { get; private set; }
    public EstadoCasoRevision Estado { get; private set; }
    public string SolicitudesAfectadas { get; private set; } = "[]";
    public string OrdenDecidido { get; private set; } = "[]";
    public string HuellaOrigen { get; private set; } = "";
    public Guid? SorteoId { get; private set; }
    public int? VersionSorteo { get; private set; }
    public Guid? VersionReglasId { get; private set; }
    public Guid? UsuarioResolucion { get; private set; }
    public DateTimeOffset? FechaResolucion { get; private set; }
    public string? MotivoResolucion { get; private set; }
    public DateTimeOffset FechaCreacion { get; private set; }
    public int Revision { get; private set; }

    private CasoRevision() { }

    public CasoRevision(
        Guid periodoId,
        int sala,
        TipoCasoRevision tipo,
        IReadOnlyCollection<Guid> solicitudesAfectadas,
        string huellaOrigen,
        Guid? sorteoId,
        int? versionSorteo,
        Guid? versionReglasId)
    {
        if (periodoId == Guid.Empty) throw new ArgumentException("El período es obligatorio.", nameof(periodoId));
        if (sala < 1) throw new ArgumentOutOfRangeException(nameof(sala));
        if (!Enum.IsDefined(tipo)) throw new ArgumentException("El tipo de caso no es válido.", nameof(tipo));
        ValidarSolicitudes(solicitudesAfectadas);
        ArgumentException.ThrowIfNullOrWhiteSpace(huellaOrigen);

        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        Sala = sala;
        Tipo = tipo;
        Estado = EstadoCasoRevision.Pendiente;
        SolicitudesAfectadas = Serializar(solicitudesAfectadas);
        HuellaOrigen = huellaOrigen.Trim();
        SorteoId = sorteoId;
        VersionSorteo = versionSorteo;
        VersionReglasId = versionReglasId;
        FechaCreacion = DateTimeOffset.UtcNow;
        Revision = 1;
    }

    public IReadOnlyList<Guid> ObtenerSolicitudesAfectadas()
        => Deserializar(SolicitudesAfectadas);

    public IReadOnlyList<Guid> ObtenerOrdenDecidido()
        => Deserializar(OrdenDecidido);

    public void Resolver(
        Guid usuarioId,
        IReadOnlyList<Guid> ordenDecidido,
        string motivo,
        int revisionEsperada)
    {
        ValidarRevision(revisionEsperada);
        if (Estado == EstadoCasoRevision.Resuelto)
            throw new InvalidOperationException("El caso ya fue resuelto y debe reabrirse antes de modificarlo.");
        if (usuarioId == Guid.Empty) throw new ArgumentException("El usuario es obligatorio.", nameof(usuarioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        ValidarOrden(ordenDecidido);

        Estado = EstadoCasoRevision.Resuelto;
        OrdenDecidido = Serializar(ordenDecidido);
        UsuarioResolucion = usuarioId;
        FechaResolucion = DateTimeOffset.UtcNow;
        MotivoResolucion = motivo.Trim();
        Revision++;
    }

    public void Reabrir(string motivo, int revisionEsperada)
    {
        ValidarRevision(revisionEsperada);
        if (Estado != EstadoCasoRevision.Resuelto)
            throw new InvalidOperationException("Solo se puede reabrir un caso resuelto.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        Estado = EstadoCasoRevision.Reabierto;
        MotivoResolucion = motivo.Trim();
        Revision++;
    }

    private void ValidarRevision(int revisionEsperada)
    {
        if (revisionEsperada != Revision)
            throw new InvalidOperationException("El caso cambió mientras se lo estaba editando.");
    }

    private void ValidarOrden(IReadOnlyList<Guid> ordenDecidido)
    {
        ValidarSolicitudes(ordenDecidido);
        var afectadas = ObtenerSolicitudesAfectadas().OrderBy(x => x).ToArray();
        var orden = ordenDecidido.OrderBy(x => x).ToArray();
        if (!afectadas.SequenceEqual(orden))
            throw new ArgumentException("El orden decidido debe contener exactamente las solicitudes afectadas.", nameof(ordenDecidido));
    }

    private static void ValidarSolicitudes(IEnumerable<Guid> solicitudes)
    {
        var ids = solicitudes?.ToArray() ?? throw new ArgumentNullException(nameof(solicitudes));
        if (ids.Length < 1 || ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("El caso debe afectar al menos una solicitud válida.", nameof(solicitudes));
    }

    private static string Serializar(IEnumerable<Guid> ids) => JsonSerializer.Serialize(ids.ToArray());

    private static IReadOnlyList<Guid> Deserializar(string json)
        => JsonSerializer.Deserialize<Guid[]>(json) ?? [];
}
