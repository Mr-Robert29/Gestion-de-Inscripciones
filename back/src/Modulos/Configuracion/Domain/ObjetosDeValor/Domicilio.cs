namespace Jardin.Configuracion.Domain.ObjetosDeValor;

// Valor local de Configuración para la futura Institucion.
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
    public Domicilio(string calle, string numero, string localidad, string provincia,
        string? codigoPostal = null, string? complemento = null,
        string? piso = null, string? departamento = null, string? barrio = null)
    {
        Calle = calle?.Trim() ?? ""; Numero = numero?.Trim() ?? "";
        Localidad = localidad?.Trim() ?? ""; Provincia = provincia?.Trim() ?? "";
        CodigoPostal = codigoPostal; Complemento = complemento;
        Piso = piso; Departamento = departamento; Barrio = barrio;
        // La obligatoriedad por tipo de dirección se completa con P-14.
    }
}
