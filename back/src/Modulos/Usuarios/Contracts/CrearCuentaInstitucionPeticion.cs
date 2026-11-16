namespace Jardin.Usuarios.Contracts;

public sealed record CrearCuentaInstitucionPeticion(
    string Nombre,
    string CorreoElectronico,
    string ClaveInicial,
    string Dni);
