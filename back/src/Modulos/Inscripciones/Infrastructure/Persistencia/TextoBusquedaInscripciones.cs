namespace Jardin.Inscripciones.Infrastructure.Persistencia;

// EF traduce esta llamada a SQL upper en ambos lados; nunca debe ejecutarse como código de .NET.
public static class TextoBusquedaInscripciones
{
    public static string Mayusculas(string valor) => throw new NotSupportedException("Esta función se usa dentro de consultas SQL.");
}
