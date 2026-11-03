namespace Jardin.Inscripciones.Domain.Entidades;

public enum EstadoDocumentacionSolicitud { Pendiente, Presentada, Observada }

// Registra la presentación o pendencia, sin exigir un archivo digitalizado.
public sealed class DocumentacionSolicitud
{
    public Guid Id { get; private set; }
    public Guid SolicitudId { get; private set; }
    public string Tipo { get; private set; } = "";
    public EstadoDocumentacionSolicitud Estado { get; private set; }
    public string? Referencia { get; private set; }
    public string? Observacion { get; private set; }
    public Guid UsuarioRegistroId { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }
    public Guid? UsuarioRevisionId { get; private set; }
    public DateTimeOffset? FechaRevision { get; private set; }
    private DocumentacionSolicitud() { }

    public DocumentacionSolicitud(
        Guid solicitudId,
        string tipo,
        EstadoDocumentacionSolicitud estado,
        string? referencia,
        string? observacion,
        Guid autor)
    {
        if (solicitudId == Guid.Empty || autor == Guid.Empty) throw new ArgumentException("La solicitud y el autor son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tipo);
        if (!Enum.IsDefined(estado)) throw new ArgumentException("El estado documental no es válido.");
        Id = Guid.NewGuid();
        SolicitudId = solicitudId;
        Tipo = tipo.Trim();
        Estado = estado;
        Referencia = Normalizar(referencia);
        Observacion = Normalizar(observacion);
        UsuarioRegistroId = autor;
        FechaRegistro = DateTimeOffset.UtcNow;
    }

    public void Revisar(
        EstadoDocumentacionSolicitud estado,
        string? referencia,
        string? observacion,
        Guid autor)
    {
        if (!Enum.IsDefined(estado)) throw new ArgumentException("El estado documental no es válido.");
        if (autor == Guid.Empty) throw new ArgumentException("El revisor es obligatorio.");
        Estado = estado;
        Referencia = Normalizar(referencia);
        Observacion = Normalizar(observacion);
        UsuarioRevisionId = autor;
        FechaRevision = DateTimeOffset.UtcNow;
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
