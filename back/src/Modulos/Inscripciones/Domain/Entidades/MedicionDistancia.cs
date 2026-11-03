namespace Jardin.Inscripciones.Domain.Entidades;

// Historial inmutable: corregir una distancia crea otro registro y conserva el anterior.
public sealed class MedicionDistancia
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public int NumeroRegistro { get; private set; }
    public int RevisionDomicilio { get; private set; }
    public int RevisionUbicacionJardin { get; private set; }
    public decimal DistanciaMetros { get; private set; }
    public string Origen { get; private set; } = "ManualProvisional";
    public string Referencia { get; private set; } = "";
    public string Entradas { get; private set; } = "";
    public DateTimeOffset FechaRegistro { get; private set; }
    public Guid UsuarioId { get; private set; }
    private MedicionDistancia() { }

    public MedicionDistancia(SolicitudInscripcion solicitud, RegistroDistanciaManual registro)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(registro);
        if (registro.DistanciaMetros < 0) throw new ArgumentException("La distancia en metros no puede ser negativa.");
        if (registro.UsuarioId == Guid.Empty || registro.NumeroRegistro < 1 || registro.RevisionUbicacionJardin < 0)
            throw new ArgumentException("Autor, número de registro o revisión no válidos.");
        ArgumentException.ThrowIfNullOrWhiteSpace(registro.Referencia);
        ArgumentException.ThrowIfNullOrWhiteSpace(registro.Entradas);
        Id = Guid.NewGuid();
        SolicitudId = solicitud.Id;
        InstitucionId = solicitud.InstitucionId;
        NumeroRegistro = registro.NumeroRegistro;
        RevisionDomicilio = solicitud.RevisionDomicilio;
        RevisionUbicacionJardin = registro.RevisionUbicacionJardin;
        DistanciaMetros = registro.DistanciaMetros;
        Referencia = registro.Referencia.Trim();
        Entradas = registro.Entradas;
        UsuarioId = registro.UsuarioId;
        FechaRegistro = DateTimeOffset.UtcNow;
    }
}

public sealed record RegistroDistanciaManual(
    decimal DistanciaMetros,
    int RevisionUbicacionJardin,
    int NumeroRegistro,
    Guid UsuarioId,
    string Referencia,
    string Entradas);
