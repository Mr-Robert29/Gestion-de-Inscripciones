namespace SistemaAsignacionEscolar.Api.modules.Identidad.IntegrationContracts.Events
{
    public record AlumnoRegistradoEvent(int IdAlumno, string NombreCompleto, int Edad);
}