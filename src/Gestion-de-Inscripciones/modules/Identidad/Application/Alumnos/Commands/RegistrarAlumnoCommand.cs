using System;

namespace SistemaEducativo.Modulos.Identidad.Application.Alumnos.Commands
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
