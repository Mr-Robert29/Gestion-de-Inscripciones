namespace Jardin.Inscripciones.Domain.Entidades;

public sealed class Responsable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Apellido { get; private set; } = "";
    public string? Documento { get; private set; }
    public string? TipoDocumento { get; private set; }
    public string Telefono { get; private set; } = "";
    public string? CorreoElectronico { get; private set; }
    private Responsable() { }
    public Responsable(string nombre, string apellido, string? documento, string telefono,
        string? correoElectronico = null, string? tipoDocumento = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellido);
        Id = Guid.NewGuid(); Nombre = nombre.Trim(); Apellido = apellido.Trim();
        Documento = string.IsNullOrWhiteSpace(documento) ? null : documento.Trim();
        TipoDocumento = tipoDocumento;
        ActualizarContacto(telefono, correoElectronico);
    }
    public void ActualizarContacto(string telefono, string? correoElectronico)
    {
        Telefono = telefono?.Trim() ?? "";
        CorreoElectronico = string.IsNullOrWhiteSpace(correoElectronico) ? null : correoElectronico.Trim();
    }
}
