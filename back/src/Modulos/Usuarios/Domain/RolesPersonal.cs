namespace Jardin.Usuarios.Domain;

public static class RolesPersonal
{
    public const string Administrativo = "Administrativo";
    public const string Directivo = "Directivo";
    public const string AdministradorDelSistema = "AdministradorDelSistema";
    public static IReadOnlyList<string> Todos { get; } =
        Array.AsReadOnly(new[] { Administrativo, Directivo, AdministradorDelSistema });
}
