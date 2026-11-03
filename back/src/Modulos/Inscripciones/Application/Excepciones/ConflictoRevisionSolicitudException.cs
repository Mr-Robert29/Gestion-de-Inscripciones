namespace Jardin.Inscripciones.Application.Excepciones;

public sealed class ConflictoRevisionSolicitudException : Exception
{
    public ConflictoRevisionSolicitudException() : base("La solicitud cambió. Consultá la revisión actual antes de guardar.") { }
}
