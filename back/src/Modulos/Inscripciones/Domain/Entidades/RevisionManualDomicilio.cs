using Jardin.Inscripciones.Domain.ObjetosDeValor;

namespace Jardin.Inscripciones.Domain.Entidades;

public enum EstadoRevisionDomicilio { PendienteRevision, Observado, RevisadoManualmente }

// Cada decisión corresponde a una versión concreta de la dirección y conserva su copia histórica.
public sealed class RevisionManualDomicilio
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public int NumeroRegistro { get; private set; }
    public int RevisionDomicilio { get; private set; }
    public int RevisionSolicitudUtilizada { get; private set; }
    public EstadoRevisionDomicilio Estado { get; private set; }
    public Domicilio Domicilio { get; private set; } = null!;
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Motivo { get; private set; } = "";
    public string? Referencia { get; private set; }
    private RevisionManualDomicilio() { }

    public RevisionManualDomicilio(SolicitudInscripcion solicitud, DatosDecisionRevisionDomicilio decision)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(decision);
        if (!Enum.IsDefined(decision.Estado)) throw new ArgumentException("El estado de revisión no es válido.");
        if (decision.UsuarioId == Guid.Empty || decision.NumeroRegistro < 1) throw new ArgumentException("Autor y número de registro son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(decision.Motivo);
        if (decision.Estado == EstadoRevisionDomicilio.RevisadoManualmente) ArgumentException.ThrowIfNullOrWhiteSpace(decision.Referencia);
        Id = Guid.NewGuid();
        InstitucionId = solicitud.InstitucionId;
        SolicitudId = solicitud.Id;
        RevisionDomicilio = solicitud.RevisionDomicilio;
        RevisionSolicitudUtilizada = solicitud.RevisionDatos;
        NumeroRegistro = decision.NumeroRegistro;
        Estado = decision.Estado;
        Domicilio = solicitud.DomicilioDeclarado with { };
        UsuarioId = decision.UsuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Motivo = decision.Motivo.Trim();
        Referencia = string.IsNullOrWhiteSpace(decision.Referencia) ? null : decision.Referencia.Trim();
    }
}

public sealed record DatosDecisionRevisionDomicilio(
    EstadoRevisionDomicilio Estado,
    int NumeroRegistro,
    Guid UsuarioId,
    string Motivo,
    string? Referencia);
