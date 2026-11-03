namespace Jardin.Configuracion.Application.Excepciones;

// Conflicto de edición: el cliente debe volver a consultar antes de reintentar.
public sealed class ConflictoInstitucionException : Exception
{
    public ConflictoInstitucionException() : base("La institución cambió. Consultá sus datos actuales antes de volver a editar.") { }
}
