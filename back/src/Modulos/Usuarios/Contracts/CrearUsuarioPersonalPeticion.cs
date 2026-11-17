namespace Jardin.Usuarios.Contracts;

public sealed record CrearUsuarioPersonalPeticion(
    string Nombre,
    string CorreoElectronico,
    string ClaveInicial,
    string RolGlobal,
    string Dni,
    Guid? InstitucionInicialId = null);
