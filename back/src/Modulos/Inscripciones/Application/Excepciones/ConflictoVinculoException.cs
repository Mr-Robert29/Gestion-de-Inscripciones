namespace Jardin.Inscripciones.Application.Excepciones;

public sealed class ConflictoVinculoException : Exception
{
    public ConflictoVinculoException() : base("El responsable ya está vinculado con el aspirante.") { }
}
