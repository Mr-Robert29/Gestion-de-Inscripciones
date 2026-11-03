namespace Jardin.Inscripciones.Domain.Entidades;

// Índice de referencias externas: no contiene ni confirma mediciones o propuestas del módulo de origen.
public sealed class UsoDatosSolicitud
{
    public Guid Id { get; private set; }
    public Guid SolicitudId { get; private set; }
    public Guid ReferenciaId { get; private set; }
    public string Tipo { get; private set; } = "";
    public int RevisionDatos { get; private set; }
    public int RevisionDomicilio { get; private set; }
    private UsoDatosSolicitud() { }

    public UsoDatosSolicitud(
        Guid solicitudId,
        Guid referenciaId,
        string tipo,
        int revisionDatos,
        int revisionDomicilio)
    {
        if (solicitudId == Guid.Empty || referenciaId == Guid.Empty) throw new ArgumentException("Solicitud y referencia son obligatorias.");
        if (tipo is not ("MedicionDistancia" or "PropuestaIngreso" or "PropuestaDistribucion"))
            throw new ArgumentException("El tipo de referencia no es válido.");
        if (revisionDomicilio < 1 || revisionDatos < revisionDomicilio)
            throw new ArgumentException("Las revisiones indicadas no son válidas.");
        Id = Guid.NewGuid();
        SolicitudId = solicitudId;
        ReferenciaId = referenciaId;
        Tipo = tipo;
        RevisionDatos = revisionDatos;
        RevisionDomicilio = revisionDomicilio;
    }

    public bool EstaDesactualizado(SolicitudInscripcion solicitud)
    {
        if (SolicitudId != solicitud.Id) throw new ArgumentException("La referencia pertenece a otra solicitud.");
        return Tipo == "MedicionDistancia"
            ? RevisionDomicilio != solicitud.RevisionDomicilio
            : RevisionDatos != solicitud.RevisionDatos;
    }
}
