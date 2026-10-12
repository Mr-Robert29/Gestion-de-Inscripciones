namespace Jardin.Inscripciones.Domain.Entidades;

public sealed class Aspirante
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Apellido { get; private set; } = "";
    public string? Dni { get; private set; }
    public DateOnly FechaNacimiento { get; private set; }
    public string? TipoDocumento { get; private set; }
    public string? Sexo { get; private set; }
    private readonly List<VinculoResponsable> _vinculos = [];
    public IReadOnlyCollection<VinculoResponsable> Vinculos => _vinculos.AsReadOnly();
    private Aspirante() { }
    public Aspirante(string nombre, string apellido, string? dni, DateOnly fechaNacimiento,
        string? tipoDocumento = null, string? sexo = null)
    {
        Id = Guid.NewGuid();
        CorregirDatos(nombre, apellido, dni, fechaNacimiento, tipoDocumento, sexo);
    }
    public void CorregirDatos(string nombre, string apellido, string? dni, DateOnly fechaNacimiento,
        string? tipoDocumento = null, string? sexo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellido);
        Nombre = nombre.Trim(); Apellido = apellido.Trim();
        Dni = string.IsNullOrWhiteSpace(dni) ? null : dni.Trim();
        FechaNacimiento = fechaNacimiento;
        TipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? null : tipoDocumento.Trim();
        Sexo = string.IsNullOrWhiteSpace(sexo) ? null : sexo.Trim();
    }
    public int CalcularEdad(DateOnly fechaCorte)
    {
        if (fechaCorte < FechaNacimiento) throw new ArgumentException("La fecha de corte es anterior al nacimiento.");
        var edad = fechaCorte.Year - FechaNacimiento.Year;
        if (fechaCorte.Month < FechaNacimiento.Month ||
            (fechaCorte.Month == FechaNacimiento.Month && fechaCorte.Day < FechaNacimiento.Day)) edad--;
        return edad;
    }
    public VinculoResponsable VincularResponsable(Responsable responsable, string tipoVinculo,
        bool contactoPrincipal, bool esTutor)
    {
        ArgumentNullException.ThrowIfNull(responsable);
        if (_vinculos.Any(v => v.ResponsableId == responsable.Id))
            throw new InvalidOperationException("El responsable ya está vinculado con el aspirante.");
        var vinculo = new VinculoResponsable(Id, responsable, tipoVinculo, contactoPrincipal, esTutor);
        _vinculos.Add(vinculo);
        return vinculo;
    }
}
