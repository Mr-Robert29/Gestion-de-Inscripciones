namespace Jardin.Usuarios.Domain.Entidades;

public sealed class AccesoInstitucion
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid InstitucionId { get; private set; }
    public string Rol { get; private set; } = "";

    private AccesoInstitucion() { }

    public AccesoInstitucion(Guid id, Guid usuarioId, Guid institucionId, string rol)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador del acceso no puede ser vacío.", nameof(id));
        if (usuarioId == Guid.Empty) throw new ArgumentException("El identificador del usuario no puede ser vacío.", nameof(usuarioId));
        if (institucionId == Guid.Empty) throw new ArgumentException("El identificador de la institución no puede ser vacío.", nameof(institucionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(rol);

        var rolLimpio = rol.Trim();
        if (!RolesPersonal.Todos.Contains(rolLimpio))
            throw new ArgumentException($"El rol '{rol}' no es válido para el personal.", nameof(rol));

        Id = id;
        UsuarioId = usuarioId;
        InstitucionId = institucionId;
        Rol = rolLimpio;
    }

    public void CambiarRol(string nuevoRol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nuevoRol);
        var rolLimpio = nuevoRol.Trim();
        if (!RolesPersonal.Todos.Contains(rolLimpio))
            throw new ArgumentException($"El rol '{nuevoRol}' no es válido para el personal.", nameof(nuevoRol));

        Rol = rolLimpio;
    }
}
