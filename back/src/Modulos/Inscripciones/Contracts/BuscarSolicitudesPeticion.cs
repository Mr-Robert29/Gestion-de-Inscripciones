namespace Jardin.Inscripciones.Contracts;

// Se envía en el cuerpo para no colocar nombre/DNI en la URL de búsqueda.
public sealed record BuscarSolicitudesPeticion(
    Guid? SolicitudId = null,
    Guid? PeriodoId = null,
    string? Nombre = null,
    string? Apellido = null,
    string? Dni = null,
    int Pagina = 1,
    int TamanoPagina = 20);
