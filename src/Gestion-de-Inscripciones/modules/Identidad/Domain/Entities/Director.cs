namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{
    public class Director : Usuario
    {
        public Director(string nombre, string email) : base(nombre, email)
        {
            Rol = "Director";
        }

        public override bool TienePermiso(string operacion)
        {
            var permisosDirector = new List<string> { "CrearPeriodo", "CerrarPeriodo", "ConfigurarSalas" };
            return permisosDirector.Contains(operacion);
        }
    }
}
