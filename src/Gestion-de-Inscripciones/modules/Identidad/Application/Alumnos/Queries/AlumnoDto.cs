using System;

namespace SistemaEducativo.Modulos.Identidad.Application.Alumnos.Queries
{
    public class AlumnoDto
    {
        public int IdAlumno { get; set; }
        public string NombreCompleto { get; set; }
        public string NroDocumento { get; set; }
        public int Edad { get; set; }
    }
}
