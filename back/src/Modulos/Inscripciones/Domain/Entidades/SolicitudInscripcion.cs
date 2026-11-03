using Jardin.Inscripciones.Domain.ObjetosDeValor;

namespace Jardin.Inscripciones.Domain.Entidades;

public enum EstadoRecepcionSolicitud
{
    Ordinaria,
    FueraDeTerminoPendienteAutorizacion
}

// La recepción no representa una decisión de ingreso ni una asignación a sección.
public sealed class SolicitudInscripcion
{
    private readonly List<TurnoAlternativoSolicitud> _turnosAlternativos = [];
    private readonly List<DocumentacionSolicitud> _documentos = [];
    private readonly List<CondicionPrioridad> _condiciones = [];
    public IReadOnlyCollection<DocumentacionSolicitud> Documentos => _documentos.AsReadOnly();
    public IReadOnlyCollection<CondicionPrioridad> Condiciones => _condiciones.AsReadOnly();
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid AspiranteId { get; private set; }
    public Domicilio DomicilioDeclarado { get; private set; } = null!;
    public CalculoEdadSolicitud CalculoEdad { get; private set; } = null!;
    public Guid TurnoPreferidoId { get; private set; }
    public IReadOnlyCollection<TurnoAlternativoSolicitud> TurnosAlternativos => _turnosAlternativos.AsReadOnly();
    public Guid VersionReglasId { get; private set; }
    public Guid VersionOfertaId { get; private set; }
    public int RevisionPeriodoUtilizada { get; private set; }
    public EstadoRecepcionSolicitud EstadoRecepcion { get; private set; }
    public bool FueraDeTermino => EstadoRecepcion == EstadoRecepcionSolicitud.FueraDeTerminoPendienteAutorizacion;
    public bool RequiereRevisionEdad => CalculoEdad.Sala is null;
    public Guid UsuarioRegistroId { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }
    public string? ReferenciaConsentimiento { get; private set; }
    public int RevisionDatos { get; private set; }
    public int RevisionDomicilio { get; private set; } = 1;

    private SolicitudInscripcion() { }

    public SolicitudInscripcion(
        Guid institucionId,
        Guid periodoId,
        Aspirante aspirante,
        Domicilio domicilio,
        DateOnly fechaCorte,
        Guid turnoPreferidoId,
        IReadOnlyList<Guid> alternativasAceptadas,
        Guid versionReglasId,
        Guid versionOfertaId,
        int revisionPeriodo,
        bool fueraDeTermino,
        Guid usuarioRegistroId,
        string? referenciaConsentimiento = null)
    {
        ArgumentNullException.ThrowIfNull(aspirante);
        ArgumentNullException.ThrowIfNull(domicilio);
        ArgumentNullException.ThrowIfNull(alternativasAceptadas);
        if (institucionId == Guid.Empty || periodoId == Guid.Empty || turnoPreferidoId == Guid.Empty ||
            versionReglasId == Guid.Empty || versionOfertaId == Guid.Empty || usuarioRegistroId == Guid.Empty)
            throw new ArgumentException("Institución, período, turno, versiones y autor son obligatorios.");
        if (revisionPeriodo < 1) throw new ArgumentException("La revisión del período debe ser positiva.");
        if (alternativasAceptadas.Any(id => id == Guid.Empty || id == turnoPreferidoId) ||
            alternativasAceptadas.Distinct().Count() != alternativasAceptadas.Count)
            throw new ArgumentException("Las alternativas no pueden repetirse, estar vacías ni incluir el turno preferido.");
        var calculoEdad = new CalculoEdadSolicitud(aspirante, fechaCorte);
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        PeriodoId = periodoId;
        AspiranteId = aspirante.Id;
        DomicilioDeclarado = domicilio;
        CalculoEdad = calculoEdad;
        TurnoPreferidoId = turnoPreferidoId;
        _turnosAlternativos.AddRange(alternativasAceptadas.Select(id => new TurnoAlternativoSolicitud(id)));
        VersionReglasId = versionReglasId;
        VersionOfertaId = versionOfertaId;
        RevisionPeriodoUtilizada = revisionPeriodo;
        EstadoRecepcion = fueraDeTermino ? EstadoRecepcionSolicitud.FueraDeTerminoPendienteAutorizacion : EstadoRecepcionSolicitud.Ordinaria;
        UsuarioRegistroId = usuarioRegistroId;
        FechaRegistro = DateTimeOffset.UtcNow;
        ReferenciaConsentimiento = string.IsNullOrWhiteSpace(referenciaConsentimiento) ? null : referenciaConsentimiento.Trim();
        RevisionDatos = 1;
    }
    // Corrige datos de esta solicitud sin cambiar la persona, recepción ni versiones históricas.
    public bool CorregirDatos(
        Domicilio domicilio,
        Guid turnoPreferidoId,
        IReadOnlyList<Guid> alternativasAceptadas,
        string? referenciaConsentimiento)
    {
        ArgumentNullException.ThrowIfNull(domicilio);
        ArgumentNullException.ThrowIfNull(alternativasAceptadas);
        if (turnoPreferidoId == Guid.Empty) throw new ArgumentException("El turno preferido es obligatorio.");
        if (alternativasAceptadas.Any(id => id == Guid.Empty || id == turnoPreferidoId) ||
            alternativasAceptadas.Distinct().Count() != alternativasAceptadas.Count)
            throw new ArgumentException("Las alternativas no pueden repetirse, estar vacías ni incluir el turno preferido.");
        var referencia = string.IsNullOrWhiteSpace(referenciaConsentimiento) ? null : referenciaConsentimiento.Trim();
        var cambioDomicilio = DomicilioDeclarado != domicilio;
        var cambioTurnos = TurnoPreferidoId != turnoPreferidoId ||
            !_turnosAlternativos.Select(t => t.TurnoId).Order().SequenceEqual(alternativasAceptadas.Order());
        if (!cambioDomicilio && !cambioTurnos && ReferenciaConsentimiento == referencia)
            throw new InvalidOperationException("No se detectaron cambios en los datos de la solicitud.");
        DomicilioDeclarado = domicilio;
        TurnoPreferidoId = turnoPreferidoId;
        // Conservamos las instancias que siguen vigentes para el seguimiento de la colección de EF.
        var aceptadas = alternativasAceptadas.ToHashSet();
        _turnosAlternativos.RemoveAll(t => !aceptadas.Contains(t.TurnoId));
        foreach (var turnoId in alternativasAceptadas)
            if (_turnosAlternativos.All(t => t.TurnoId != turnoId)) _turnosAlternativos.Add(new TurnoAlternativoSolicitud(turnoId));
        ReferenciaConsentimiento = referencia;
        if (cambioDomicilio) RevisionDomicilio++;
        RevisionDatos++;
        return cambioDomicilio;
    }

    // Invalida la revisión anterior cuando cambian antecedentes usados para evaluar la solicitud.
    public void RegistrarRevisionAntecedentes() => RevisionDatos++;
}
