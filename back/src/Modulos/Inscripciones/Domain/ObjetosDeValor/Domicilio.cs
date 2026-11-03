namespace Jardin.Inscripciones.Domain.ObjetosDeValor;

// Dirección propia de una solicitud; no modifica el domicilio de otras solicitudes.
public sealed record Domicilio
{
    public string Calle { get; }
    public string Numero { get; }
    public string Localidad { get; }
    public string Provincia { get; }
    public string? CodigoPostal { get; }
    public string? Complemento { get; }
    public string? Piso { get; }
    public string? Departamento { get; }
    public string? Barrio { get; }

    public Domicilio(
        string calle,
        string numero,
        string localidad,
        string provincia,
        string? codigoPostal = null,
        string? complemento = null,
        string? piso = null,
        string? departamento = null,
        string? barrio = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calle);
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(localidad);
        ArgumentException.ThrowIfNullOrWhiteSpace(provincia);
        Calle = calle.Trim();
        Numero = numero.Trim();
        Localidad = localidad.Trim();
        Provincia = provincia.Trim();
        CodigoPostal = Normalizar(codigoPostal);
        Complemento = Normalizar(complemento);
        Piso = Normalizar(piso);
        Departamento = Normalizar(departamento);
        Barrio = Normalizar(barrio);
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
