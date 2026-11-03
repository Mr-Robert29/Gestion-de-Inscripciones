namespace Jardin.Inscripciones.Application.Excepciones;

public sealed class ConflictoDuplicadoException : Exception
{
    public ConflictoDuplicadoException() : base("La alerta o sus datos cambiaron. Volvé a detectar y consultá la revisión actual antes de resolver.") { }
}
