namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{
    public class Maestro : Usuario
    {
        public Maestro(string nombre, string email) : base(nombre, email)
        {
            Rol = "Maestro";
        }

        public override bool TienePermiso(string operacion)
        {
            var permisosMaestro = new List<string> { "PreinscribirAlumno", "ConsultarListadoFinal" };
            return permisosMaestro.Contains(operacion);
        }
    }
}
