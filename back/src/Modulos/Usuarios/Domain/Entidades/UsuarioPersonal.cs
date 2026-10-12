namespace Jardin.Usuarios.Domain.Entidades;

public sealed class UsuarioPersonal
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string CorreoElectronico { get; private set; } = "";
    public bool Activo { get; private set; }
    private UsuarioPersonal() { }
    public UsuarioPersonal(Guid id, string nombre, string correoElectronico)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(correoElectronico);
        Id = id; Nombre = nombre.Trim(); CorreoElectronico = correoElectronico.Trim(); Activo = true;
    }
    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
    public void CambiarNombre(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre); Nombre = nombre.Trim();
    }
}
