using System;

namespace Jardin.Configuracion.Contracts
{
    // Datos de dirección para mensajes de entrada o salida; sus reglas pertenecen a Domain.
    public sealed record DatosDomicilio(
        string Calle,
        string Numero,
        string Localidad,
        string Provincia,
        string? CodigoPostal = null,
        string? Complemento = null,
        string? Piso = null,
        string? Departamento = null,
        string? Barrio = null);
}
