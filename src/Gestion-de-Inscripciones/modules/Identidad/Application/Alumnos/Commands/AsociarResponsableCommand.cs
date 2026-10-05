namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Commands
{
    public record AsociarResponsableCommand(
        int IdAlumno,
        string Nombre, 
        string Apellido, 
        string TipoDocumento, 
        string NroDocumento, 
        string Telefono, 
        string Email, 
        string Vinculo, 
        bool EsTutor
    );
}
