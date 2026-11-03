namespace Jardin.Inscripciones.Domain.Entidades;

public enum EstadoVerificacionCondicion { PendienteRevision, Verificada, Observada }

// Una declaración y su verificación no asignan puntajes ni deciden un ingreso.
public sealed class CondicionPrioridad
{
    public Guid Id { get; private set; }
    public Guid SolicitudId { get; private set; }
    public string Tipo { get; private set; } = "";
    public string ValorDeclarado { get; private set; } = "";
    public EstadoVerificacionCondicion EstadoVerificacion { get; private set; }
    public string? EvidenciaReferencia { get; private set; }
    public string? Observacion { get; private set; }
    public Guid UsuarioRegistroId { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }
    public Guid? UsuarioRevisionId { get; private set; }
    public DateTimeOffset? FechaRevision { get; private set; }
    private CondicionPrioridad() { }

    public CondicionPrioridad(
        Guid solicitudId,
        string tipo,
        string valorDeclarado,
        string? evidenciaReferencia,
        Guid autor)
    {
        if (solicitudId == Guid.Empty || autor == Guid.Empty) throw new ArgumentException("La solicitud y el autor son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tipo);
        ArgumentException.ThrowIfNullOrWhiteSpace(valorDeclarado);
        Id = Guid.NewGuid();
        SolicitudId = solicitudId;
        Tipo = tipo.Trim();
        ValorDeclarado = valorDeclarado.Trim();
        EvidenciaReferencia = Normalizar(evidenciaReferencia);
        EstadoVerificacion = EstadoVerificacionCondicion.PendienteRevision;
        UsuarioRegistroId = autor;
        FechaRegistro = DateTimeOffset.UtcNow;
    }

    public void Revisar(
        EstadoVerificacionCondicion estado,
        string? evidenciaReferencia,
        string? observacion,
        Guid autor)
    {
        if (!Enum.IsDefined(estado)) throw new ArgumentException("El estado de verificación no es válido.");
        if (autor == Guid.Empty) throw new ArgumentException("El revisor es obligatorio.");
        EstadoVerificacion = estado;
        EvidenciaReferencia = Normalizar(evidenciaReferencia);
        Observacion = Normalizar(observacion);
        UsuarioRevisionId = autor;
        FechaRevision = DateTimeOffset.UtcNow;
    }

    // Una verificación anterior no se traslada silenciosamente a un contenido distinto.
    public void CorregirDeclaracion(
        string tipo,
        string valorDeclarado,
        string? evidenciaReferencia)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tipo);
        ArgumentException.ThrowIfNullOrWhiteSpace(valorDeclarado);
        var referencia = Normalizar(evidenciaReferencia);
        if (Tipo == tipo.Trim() && ValorDeclarado == valorDeclarado.Trim() && EvidenciaReferencia == referencia)
            throw new InvalidOperationException("No se detectaron cambios en la declaración.");
        Tipo = tipo.Trim();
        ValorDeclarado = valorDeclarado.Trim();
        EvidenciaReferencia = referencia;
        EstadoVerificacion = EstadoVerificacionCondicion.PendienteRevision;
        Observacion = null;
        UsuarioRevisionId = null;
        FechaRevision = null;
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
