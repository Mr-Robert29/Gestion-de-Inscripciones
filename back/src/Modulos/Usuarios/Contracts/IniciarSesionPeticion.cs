namespace Jardin.Usuarios.Contracts;

public sealed record IniciarSesionPeticion(
    string CorreoElectronico,
    string Clave);
