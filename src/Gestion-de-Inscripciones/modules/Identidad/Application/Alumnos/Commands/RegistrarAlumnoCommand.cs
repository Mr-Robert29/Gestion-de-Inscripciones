using System;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Commands
{
    public record RegistrarAlumnoCommand(
        string Nombre, 
        string Apellido, 
        string TipoDocumento, 
        string NroDocumento, 
        DateTime FechaNacimiento, 
        string Sexo
    );
}
