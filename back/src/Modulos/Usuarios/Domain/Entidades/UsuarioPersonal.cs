namespace Jardin.Usuarios.Domain.Entidades;

public sealed class UsuarioPersonal
{
    private readonly List<AccesoInstitucion> _accesos = new();

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string CorreoElectronico { get; private set; } = "";
    public bool Activo { get; private set; }

    public IReadOnlyCollection<AccesoInstitucion> Accesos => _accesos.AsReadOnly();

    private UsuarioPersonal() { }

    public UsuarioPersonal(Guid id, string nombre, string correoElectronico)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador no puede ser vacío.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(correoElectronico);

        Id = id;
        Nombre = nombre.Trim();
        CorreoElectronico = correoElectronico.Trim();
        Activo = true;
    }

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    public void CambiarNombre(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre.Trim();
    }

    public AccesoInstitucion AsignarAcceso(Guid institucionId, string rol)
    {
        var accesoExistente = _accesos.FirstOrDefault(a => a.InstitucionId == institucionId);
        if (accesoExistente is not null)
        {
            accesoExistente.CambiarRol(rol);
            return accesoExistente;
        }

        var nuevoAcceso = new AccesoInstitucion(Guid.NewGuid(), Id, institucionId, rol);
        _accesos.Add(nuevoAcceso);
        return nuevoAcceso;
    }

    public bool RevocarAcceso(Guid institucionId)
    {
        var accesoExistente = _accesos.FirstOrDefault(a => a.InstitucionId == institucionId);
        if (accesoExistente is null)
            return false;

        _accesos.Remove(accesoExistente);
        return true;
    }
}
